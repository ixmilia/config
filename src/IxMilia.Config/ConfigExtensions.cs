// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

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
                if (IsLineIgnorable(line))
                {
                    // ignore blank lines and comments
                }
                else if (IsSectionName(line))
                {
                    // new section
                    prefix = GetSectionName(line) + ".";
                }
                else
                {
                    // key/value pair
                    var kvp = GetKeyValuePair(line);
                    if (kvp.Value != null)
                    {
                        dictionary[prefix + kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        public static string WriteConfig(this IDictionary<string, string> dictionary, params string[] existingLines)
        {
            var sb = new StringBuilder();
            var writtenKeys = new HashSet<string>();
            var sortedKeys = dictionary.Keys
                .Select(k => GetPrefixAndKey(k))
                .OrderBy(k => k, KeyPrefixComparer.Instance)
                .ToList();

            // write values out while preserving as much of the existing structure as possible
            var prefix = string.Empty;
            var lastLineWasBlank = false;
            foreach (var existing in existingLines)
            {
                if (IsLineIgnorable(existing))
                {
                    if (!lastLineWasBlank)
                    {
                        // just re-copy this line out
                        sb.AppendLine(existing);
                    }

                    lastLineWasBlank = string.IsNullOrWhiteSpace(existing);
                }
                else
                {
                    lastLineWasBlank = false;
                    if (IsSectionName(existing))
                    {
                        // write out any remaining key/value pairs that also belong under this section
                        var extraKeys = sortedKeys.Where(k => k.Item1 == prefix && !writtenKeys.Contains(MakeFullKey(k))).ToList();
                        foreach (var prefixAndKey in extraKeys)
                        {
                            var shortKey = prefixAndKey.Item2;
                            var fullKey = MakeFullKey(prefixAndKey);
                            writtenKeys.Add(fullKey);
                            sb.AppendLine(MakeLine(shortKey, dictionary[fullKey]));
                        }

                        prefix = GetSectionName(existing);
                        if (sortedKeys.Any(k => k.Item1 == prefix))
                        {
                            // only do this if there's actually something to be written
                            if (extraKeys.Count > 0)
                            {
                                sb.AppendLine();
                            }

                            // re-copy this line and note what section we're in
                            sb.AppendLine(existing);
                        }
                    }
                    else
                    {
                        // key/value pair; overwrite or remove value
                        var kvp = GetKeyValuePair(existing);
                        var fullKey = MakeFullKey(prefix, kvp.Key);
                        if (dictionary.ContainsKey(fullKey))
                        {
                            // update value
                            writtenKeys.Add(fullKey);
                            sb.AppendLine(MakeLine(kvp.Key, dictionary[fullKey]));
                        }
                        else
                        {
                            // do nothing (swallow key/value)
                        }
                    }
                }
            }

            // write out any remaining values
            foreach (var key in sortedKeys.Where(k => !writtenKeys.Contains(MakeFullKey(k))))
            {
                var nextPrefix = key.Item1;
                var shortKey = key.Item2;
                if (prefix != nextPrefix)
                {
                    sb.AppendLine();
                    sb.AppendLine(string.Concat("[", nextPrefix, "]"));
                    prefix = nextPrefix;
                }

                sb.AppendLine(MakeLine(shortKey, dictionary[MakeFullKey(key)]));
            }

            return sb.ToString();
        }

        public static bool TryParseInto<T>(this string str, out T result)
        {
            return str.TryParseInto(GetParseFunction<T>(), out result);
        }

        public static bool TryParseInto<T>(this string str, Func<string, T> parser, out T result)
        {
            result = default(T);
            if (parser == null)
            {
                return false;
            }

            try
            {
                result = parser(str);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static void TryParseAssign<T>(this string str, ref T target)
        {
            str.TryParseAssign(GetParseFunction<T>(), ref target);
        }

        public static void TryParseAssign<T>(this string str, Func<string, T> parser, ref T target)
        {
            T result;
            if (str.TryParseInto(parser, out result))
            {
                target = result;
            }
        }

        public static bool TryParseValue<T>(this IDictionary<string, string> dictionary, string key, out T result)
        {
            return dictionary.TryParseValue(key, GetParseFunction<T>(), out result);
        }

        public static bool TryParseValue<T>(this IDictionary<string, string> dictionary, string key, Func<string, T> parser, out T result)
        {
            string value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value.TryParseInto<T>(parser, out result);
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public static void TryAssignValue<T>(this IDictionary<string, string> dictionary, string key, ref T target)
        {
            dictionary.TryAssignValue(key, GetParseFunction<T>(), ref target);
        }

        public static void TryAssignValue<T>(this IDictionary<string, string> dictionary, string key, Func<string, T> parser, ref T target)
        {
            T result;
            if (dictionary.TryParseValue(key, parser, out result))
            {
                target = result;
            }
        }

        private static Func<string, T> GetParseFunction<T>()
        {
            var parser = GetParseFunctionSimple(typeof(T));
            if (typeof(T).IsArray)
            {
                // when creating an array, each element must be manually copied over
                return str =>
                {
                    var items = (object[])parser(str);
                    var elementType = typeof(T).GetElementType();
                    var array = Array.CreateInstance(elementType, items.Length);
                    Array.Copy(items, array, items.Length);
                    return (T)((object)array);
                };
            }

            return x => (T)parser(x);
        }

        private static Func<string, object> GetParseFunctionSimple(Type type)
        {
            // try to find a Parse() method to use
            Func<string, object> parser = null;
            if (type == typeof(string))
            {
                parser = value => ParseString(value);
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                parser = value => value.Split('|').Select(v => (int)Enum.Parse(type, v.Trim())).Aggregate((a, b) => a | b);
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var elementParser = GetParseFunctionSimple(elementType);
                parser = str => str.Split(';').Select(s => elementParser(s)).ToArray();
            }
            else
            {
                // use reflection to find a Parse() method, first trying for one that also takes an IFormatProvider
                var parseMethod = type.GetRuntimeMethod("Parse", new[] { typeof(string), typeof(IFormatProvider) });
                if (parseMethod != null && parseMethod.IsStatic)
                {
                    parser = value => parseMethod.Invoke(null, new object[] { value, CultureInfo.InvariantCulture });
                }

                if (parser == null)
                {
                    // otherwise look for the string-only version
                    parseMethod = type.GetRuntimeMethod("Parse", new[] { typeof(string) });
                    if (parseMethod != null && parseMethod.IsStatic)
                    {
                        parser = value => parseMethod.Invoke(null, new object[] { value });
                    }
                }
            }

            return parser;
        }

        private static bool IsLineIgnorable(string line)
        {
            return string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#");
        }

        private static bool IsSectionName(string line)
        {
            return line.StartsWith("[") && line.EndsWith("]");
        }

        private static string GetSectionName(string line)
        {
            return line.Substring(1, line.Length - 2);
        }

        private static Tuple<string, string> GetPrefixAndKey(string fullKey)
        {
            var dot = fullKey.LastIndexOf('.');
            var prefix = dot < 0
                ? String.Empty
                : fullKey.Substring(0, dot);
            var key = dot < 0
                ? fullKey
                : fullKey.Substring(dot + 1);
            return Tuple.Create(prefix, key);
        }

        private static KeyValuePair<string, string> GetKeyValuePair(string line)
        {
            var parts = line.Split(Separator, 2);
            var key = parts[0].Trim();
            var value = parts.Length == 2 ? parts[1].Trim() : null;
            return new KeyValuePair<string, string>(key, value);
        }

        private static string MakeFullKey(Tuple<string, string> key)
        {
            return MakeFullKey(key.Item1, key.Item2);
        }

        private static string MakeFullKey(string prefix, string shortKey)
        {
            return string.IsNullOrEmpty(prefix)
                ? shortKey
                : string.Concat(prefix, ".", shortKey);
        }

        private static string MakeLine(string key, string value)
        {
            return string.Concat(key, " = ", value);
        }

        private static string ParseString(string value)
        {
            if (value == null || value.Length == 1)
            {
                // null or too short
                return value;
            }

            if (value[0] != value[value.Length - 1] && (value[0] != '\'' || value[0] != '"'))
            {
                // not surrounded by single or double quotes
                return value;
            }

            var sb = new StringBuilder();
            var isEscaping = false;
            foreach (var c in value.ToCharArray().Skip(1).Take(value.Length - 2))
            {
                if (isEscaping)
                {
                    isEscaping = false;
                    switch (c)
                    {
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'v':
                            sb.Append('\v');
                            break;
                        case '\\':
                            sb.Append('\\');
                            break;
                        case '"':
                            sb.Append('"');
                            break;
                        default:
                            // unsupported escape sequence, just ignore it
                            break;
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        isEscaping = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }

        private class KeyPrefixComparer : IComparer<Tuple<string, string>>
        {
            public static KeyPrefixComparer Instance = new KeyPrefixComparer();

            private KeyPrefixComparer()
            {
            }

            public int Compare(Tuple<string, string> left, Tuple<string, string> right)
            {
                // split into parts before comparing, and only compare the number of prefix parts that exist
                var partsLeft = left.Item1.Split('.');
                var partsRight = right.Item1.Split('.');
                var maxPartsToCompare = Math.Min(partsLeft.Length, partsRight.Length);
                for (int i = 0; i < maxPartsToCompare; i++)
                {
                    var result = partsLeft[i].CompareTo(partsRight[i]);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                // the shortest name (with the fewest parts) wins, or fall back to comparing the short key
                var result2 = partsLeft.Length.CompareTo(partsRight.Length);
                return result2 == 0
                    ? left.Item2.CompareTo(right.Item2)
                    : result2;
            }
        }
    }
}
