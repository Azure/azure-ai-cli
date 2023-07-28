//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public interface ICommandValues : INamedValues
    {
        INamedValues Defaults { get; }
        INamedValues Specified { get; }
    }

    public class CommandValues : ICommandValues, INamedValues
    {
        public CommandValues(INamedValues defaults = null)
        {
            _defaults = defaults == null ? _overrides : defaults;
            _values = new NamedValues();
        }

        public void Add(string name, string value)
        {
            _values.Add(name, value);
        }

        public bool Contains(string name, bool checkDefault = true)
        {
            var containsOverride = _overrides.Contains(name, false);
            if (containsOverride) return containsOverride;

            var containsValue = _values.Contains(name, false);
            var blockValue = containsValue && _values.Get(name, false) == null;
            return !blockValue && (containsValue || (checkDefault && _defaults.Contains(name, true)));
        }

        public string Get(string name, bool checkDefault = true)
        {
            var containsOverride = _overrides.Contains(name, false);
            if (containsOverride) return _overrides.Get(name, false);

            var containsValue = _values.Contains(name, false);
            var blockValue = containsValue && _values.Get(name, false) == null;
            if (blockValue) return null;

            var value = _values.Get(name, false);
            if (containsValue && !string.IsNullOrEmpty(value)) return value;

            return checkDefault ? _defaults.Get(name, true) : value;
        }

        public void Reset(string name, string value = null)
        {
            _values.Reset(name);
            if (value != null)
            {
                Add(name, value);
            }
        }

        public string this[string name]
        {
            get
            {
                return Get(name, true);
            }
        }

        public IEnumerable<string> Names
        {
            get
            {
                var list = new List<string>();
                foreach (var name in _defaults.Names.ToList())
                {
                    var dvalOk = !string.IsNullOrWhiteSpace(_defaults[name]);
                    if (dvalOk && !_values.Contains(name))
                    {
                        list.Add(name);
                    }
                }
                foreach (var name in _values.Names.ToList())
                {
                    if (!string.IsNullOrWhiteSpace(_values[name])) list.Add(name);
                }
                return list;
            }
        }

        public INamedValues Defaults
        {
            get
            {
                return _defaults;
            }
        }

        public INamedValues Specified
        {
            get
            {
                return _values;
            }
        }

        private INamedValues _defaults;
        private INamedValues _values;
        private static INamedValues _overrides = EnvironmentNamedValues.Current;
    }
}
