//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;

namespace Azure.AI.Details.Common.CLI
{
    public class EnvironmentNamedValues : INamedValues
    {
        public static INamedValues Current = new EnvironmentNamedValues();
        
        public EnvironmentNamedValues()
        {
            _values = new NamedValues();

            var vars = Environment.GetEnvironmentVariables();
            foreach (var key in vars.Keys)
            {
                var ks = key.ToString() ?? string.Empty;
                var nameAndPrefix = $"{Program.Name}_";
                if (ks.ToLower().StartsWith(nameAndPrefix))
                {
                    var name = ks.Substring(nameAndPrefix.Length).Replace("_", ".").ToLower();
                    var value = vars[key]!.ToString();
                    _values.Add(name, value);
                }
            }
        }

        public string? this[string name] => Get(name, true);
        public IEnumerable<string> Names => _values.Names;
        public void Add(string name, string? value) => _values.Add(name, value);
        public bool Contains(string name, bool checkDefault = true) => _values.Contains(name, checkDefault);
        public string? Get(string name, bool checkDefault = true) => _values.Get(name, checkDefault);
        public void Reset(string name, string? value = null) => _values.Reset(name, value);

        private INamedValues _values;
    }
}
