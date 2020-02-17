﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Hocon;

namespace Akka.Configuration
{
    public sealed class FlatConfig : Config
    {
        private ConcurrentDictionary<string, HoconValue> _cache = new ConcurrentDictionary<string, HoconValue>();

        public FlatConfig(HoconRoot root, Config fallback) : base(root, fallback)
        {
            if (root is FlatConfig caching)
                _cache = caching._cache;
            else
                _cache = new ConcurrentDictionary<string, HoconValue>();
        }

        public FlatConfig(HoconRoot root) : base(root)
        {
            if (root is FlatConfig caching)
                _cache = caching._cache;
            else
                _cache = new ConcurrentDictionary<string, HoconValue>();
        }

        protected override HoconValue GetNode(string path)
        {
            if (_cache.TryGetValue(path, out var result))
                return result;

            result = Root.GetObject().GetValue(HoconPath.Parse(path));
            _cache[path] = result;
            return result;
        }

        protected override HoconValue GetNode(HoconPath path)
        {
            var fullPath = path.ToString();
            if (_cache.TryGetValue(fullPath, out var result))
                return result;

            result = Root.GetObject().GetValue(path);
            _cache[fullPath] = result;
            return result;
        }

        protected override bool TryGetNode(string path, out HoconValue result)
        {
            if (_cache.TryGetValue(path, out result))
                return true;

            if(HoconPath.TryParse(path, out var hoconPath))
                if (Root.TryGetObject(out var obj))
                    if (obj.TryGetValue(hoconPath, out result))
                    {
                        _cache[path] = result;
                        return true;
                    }

            return false;
        }

        protected override bool TryGetNode(HoconPath path, out HoconValue result)
        {
            var fullPath = path.ToString();
            if (_cache.TryGetValue(fullPath, out result))
                return true;

            if(Root.TryGetObject(out var obj))
                if(obj.TryGetValue(path, out result))
                {
                    _cache[fullPath] = result;
                    return true;
                }
            return false;
        }
    }
}
