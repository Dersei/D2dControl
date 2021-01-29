using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace D2dControl
{
    public abstract class D2dControl : System.Windows.Controls.Image
    {
        private SharpDX.Direct3D11.Device _device = null!;
        private Texture2D? _renderTarget;
        private Dx11ImageSource _d3DSurface = null!;
        private RenderTarget? _d2DRenderTarget;
        private SharpDX.Direct2D1.Factory? _d2DFactory;

        private readonly Stopwatch _renderTimer = new();

        protected readonly ResourceCache ResourceCache = new();

        private long _lastFrameTime;
        private long _lastRenderTime;
        private int _frameCount;
        private int _frameCountHistTotal;
        private readonly Queue<int> _frameCountHist = new();

        public static bool IsInDesignMode
        {
            get
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                var isDesignMode = (bool) DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement))
                    .Metadata.DefaultValue;
                return isDesignMode;
            }
        }

        private static readonly DependencyPropertyKey FpsPropertyKey = DependencyProperty.RegisterReadOnly(
            "Fps",
            typeof(int),
            typeof(D2dControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.None)
        );

        public static readonly DependencyProperty FpsProperty = FpsPropertyKey.DependencyProperty;

        public int Fps
        {
            get => (int) GetValue(FpsProperty);
            protected set => SetValue(FpsPropertyKey, value);
        }

        public static readonly DependencyProperty RenderWaitProperty = DependencyProperty.Register(
            "RenderWait",
            typeof(int),
            typeof(D2dControl),
            new FrameworkPropertyMetadata(2, OnRenderWaitChanged)
        );

        public int RenderWait
        {
            get => (int) GetValue(RenderWaitProperty);
            set => SetValue(RenderWaitProperty, value);
        }
        
        public D2dControl()
        {
            Loaded += Window_Loaded;
            Unloaded += Window_Closing;

            Stretch = System.Windows.Media.Stretch.Fill;
        }

        public abstract void Render(RenderTarget target);
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsInDesignMode)
            {
                return;
            }

            StartD3D();
            StartRendering();
        }

        private void Window_Closing(object sender, RoutedEventArgs e)
        {
            if (IsInDesignMode)
            {
                return;
            }

            StopRendering();
            EndD3D();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            PrepareAndCallRender();
            _d3DSurface.InvalidateD3DImage();

            _lastRenderTime = _renderTimer.ElapsedMilliseconds;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CreateAndBindTargets();
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3DSurface.IsFrontBufferAvailable)
            {
                StartRendering();
            }
            else
            {
                StopRendering();
            }
        }

        private static void OnRenderWaitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (D2dControl) d;
            control._d3DSurface.RenderWait = (int) e.NewValue;
        }
        
        private void StartD3D()
        {
            _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            _d3DSurface = new Dx11ImageSource();
            _d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            CreateAndBindTargets();

            Source = _d3DSurface;
        }

        private void EndD3D()
        {
            _d3DSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            Source = null;

            _d2DRenderTarget?.Dispose();
            _d2DFactory?.Dispose();
            _d3DSurface.Dispose();
            _renderTarget?.Dispose();
            _device.Dispose();
        }

        private void CreateAndBindTargets()
        {
            _d3DSurface.SetRenderTarget(null);

            _d2DRenderTarget?.Dispose();
            _d2DFactory?.Dispose();
            _renderTarget?.Dispose();

            var width = Math.Max((int) ActualWidth, 100);
            var height = Math.Max((int) ActualHeight, 100);

            var renderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            _renderTarget = new Texture2D(_device, renderDesc);

            var surface = _renderTarget.QueryInterface<Surface>();

            _d2DFactory = new SharpDX.Direct2D1.Factory();
            var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown,
                SharpDX.Direct2D1.AlphaMode.Premultiplied));
            _d2DRenderTarget = new RenderTarget(_d2DFactory, surface, rtp);
            ResourceCache.RenderTarget = _d2DRenderTarget;

            _d3DSurface.SetRenderTarget(_renderTarget);

            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);
        }

        private void StartRendering()
        {
            if (_renderTimer.IsRunning)
            {
                return;
            }

            System.Windows.Media.CompositionTarget.Rendering += OnRendering;
            _renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            System.Windows.Media.CompositionTarget.Rendering -= OnRendering;
            _renderTimer.Stop();
        }

        private void PrepareAndCallRender()
        {
            if(_d2DRenderTarget is null) throw new NullReferenceException();
            _d2DRenderTarget.BeginDraw();
            Render(_d2DRenderTarget);
            _d2DRenderTarget.EndDraw();

            CalcFps();

            _device.ImmediateContext.Flush();
        }

        private void CalcFps()
        {
            _frameCount++;
            if (_renderTimer.ElapsedMilliseconds - _lastFrameTime > 1000)
            {
                _frameCountHist.Enqueue(_frameCount);
                _frameCountHistTotal += _frameCount;
                if (_frameCountHist.Count > 5)
                {
                    _frameCountHistTotal -= _frameCountHist.Dequeue();
                }

                Fps = _frameCountHistTotal / _frameCountHist.Count;

                _frameCount = 0;
                _lastFrameTime = _renderTimer.ElapsedMilliseconds;
            }
        }
    }
}