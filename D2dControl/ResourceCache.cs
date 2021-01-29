using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;

namespace D2dControl
{
    public class ResourceCache
    {
        private readonly Dictionary<string, Func<RenderTarget, object>> _generators = new();

        private readonly Dictionary<string, object?> _resources = new();
        private RenderTarget? _renderTarget;

        public RenderTarget? RenderTarget
        {
            get => _renderTarget;
            set
            {
                _renderTarget = value;
                UpdateResources();
            }
        }

        public int Count => _resources.Count;

        public object? this[string key] => _resources[key];

        public Dictionary<string, object?>.KeyCollection Keys => _resources.Keys;

        public Dictionary<string, object?>.ValueCollection Values => _resources.Values;

        public void Add(string key, Func<RenderTarget, object> gen)
        {
            if (_resources.TryGetValue(key, out var resOld))
            {
                if (resOld is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _generators.Remove(key);
                _resources.Remove(key);
            }

            if (_renderTarget == null)
            {
                _generators.Add(key, gen);
                _resources.Add(key, null);
            }
            else
            {
                var res = gen(_renderTarget);
                _generators.Add(key, gen);
                _resources.Add(key, res);
            }
        }

        public void Clear()
        {
            foreach (var key in _resources.Keys)
            {
                var res = _resources[key];
                if (res is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _generators.Clear();
            _resources.Clear();
        }

        public bool ContainsKey(string key)
        {
            return _resources.ContainsKey(key);
        }

        public bool ContainsValue(object val)
        {
            return _resources.ContainsValue(val);
        }

        public Dictionary<string, object?>.Enumerator GetEnumerator()
        {
            return _resources.GetEnumerator();
        }

        public bool Remove(string key)
        {
            if (_resources.TryGetValue(key, out var res))
            {
                if (res is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _generators.Remove(key);
                _resources.Remove(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out object? res)
        {
            return _resources.TryGetValue(key, out res);
        }

        private void UpdateResources()
        {
            foreach (var g in _generators)
            {
                var key = g.Key;
                var gen = g.Value;
                if (_renderTarget != null)
                {
                    var res = gen(_renderTarget);

                    if (_resources.TryGetValue(key, out var resOld))
                    {
                        if (resOld is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }

                        _resources.Remove(key);
                    }

                    _resources.Add(key, res);
                }
            }
        }
    }
}