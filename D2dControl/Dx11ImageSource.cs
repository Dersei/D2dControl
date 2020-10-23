using SharpDX.Direct3D9;
using System;
using System.Threading;
using System.Windows.Interop;

namespace D2dControl
{
    internal class Dx11ImageSource : D3DImage, IDisposable
    {
        private static int _activeClients;
        private static Direct3DEx _d3DContext = null!;
        private static DeviceEx _d3DDevice = null!;

        private Texture? _renderTarget;

        public int RenderWait { get; set; } = 2; // default: 2ms

        public Dx11ImageSource()
        {
            StartD3D();
            _activeClients++;
        }

        public void Dispose()
        {
            SetRenderTarget(null);
            _renderTarget?.Dispose();

            _activeClients--;
            EndD3D();
        }

        public void InvalidateD3DImage()
        {
            if (_renderTarget != null)
            {
                Lock();
                if (RenderWait != 0)
                {
                    Thread.Sleep(RenderWait);
                }

                AddDirtyRect(new System.Windows.Int32Rect(0, 0, PixelWidth, PixelHeight));
                Unlock();
            }
        }

        public void SetRenderTarget(SharpDX.Direct3D11.Texture2D? target)
        {
            if (_renderTarget != null)
            {
                _renderTarget = null;

                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                Unlock();
            }

            if (target == null)
            {
                return;
            }

            var format = TranslateFormat(target);
            var handle = GetSharedHandle(target);

            if (!IsShareable(target))
            {
                throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");
            }

            if (format == Format.Unknown)
            {
                throw new ArgumentException("Texture format is not compatible with OpenSharedResource");
            }

            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid handle");
            }

            _renderTarget = new Texture(_d3DDevice, target.Description.Width, target.Description.Height,
                1, Usage.RenderTarget, format, Pool.Default, ref handle);

            using var surface = _renderTarget.GetSurfaceLevel(0);
            Lock();
            SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
            Unlock();
        }

        private static void StartD3D()
        {
            if (_activeClients != 0)
            {
                return;
            }

            var presentParams = GetPresentParameters();
            const CreateFlags createFlags = CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded |
                                            CreateFlags.FpuPreserve;

            _d3DContext = new Direct3DEx();
            _d3DDevice =
                new DeviceEx(_d3DContext, 0, DeviceType.Hardware, IntPtr.Zero, createFlags, presentParams);
        }

        private void EndD3D()
        {
            if (_activeClients != 0)
            {
                return;
            }

            _renderTarget?.Dispose();
            _d3DDevice.Dispose();
            _d3DContext.Dispose();
        }

        private static void ResetD3D()
        {
            if (_activeClients == 0)
            {
                return;
            }

            var presentParams = GetPresentParameters();
            _d3DDevice.ResetEx(ref presentParams);
        }

        private static PresentParameters GetPresentParameters()
        {
            var presentParams = new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = NativeMethods.GetDesktopWindow(),
                PresentationInterval = PresentInterval.Default
            };


            return presentParams;
        }

        private static IntPtr GetSharedHandle(SharpDX.Direct3D11.Texture2D texture)
        {
            using var resource = texture.QueryInterface<SharpDX.DXGI.Resource>();
            return resource.SharedHandle;
        }

        private static Format TranslateFormat(SharpDX.Direct3D11.Texture2D texture)
        {
            return texture.Description.Format switch
            {
                SharpDX.DXGI.Format.R10G10B10A2_UNorm => Format.A2B10G10R10,
                SharpDX.DXGI.Format.R16G16B16A16_Float => Format.A16B16G16R16F,
                SharpDX.DXGI.Format.B8G8R8A8_UNorm => Format.A8R8G8B8,
                _ => Format.Unknown
            };
        }

        private static bool IsShareable(SharpDX.Direct3D11.Texture2D texture)
        {
            return (texture.Description.OptionFlags & SharpDX.Direct3D11.ResourceOptionFlags.Shared) != 0;
        }
    }
}