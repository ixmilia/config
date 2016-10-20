// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace IxMilia.Config
{
    public static class ConfigExtensions
    {
        private static char[] Separator = new[] { '=' };

        public static void ParseConfig(this IDictionary<string, string> dictionary, params string[] lines)
        {
            var prefix = string.Empty;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                {
                    // ignore blank lines and comments
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // new section
                    prefix = line.Substring(1, line.Length - 2) + ".";
                }
                else
                {
                    // key/value pair
                    var parts = line.Split(Separator, 2);
                    var key = parts[0].Trim();
                    var value = parts.Length == 2 ? parts[1].Trim() : null;
                    if (value != null)
                    {
                        dictionary[prefix + key] = value;
                    }
                }
            }
        }
    }
}
