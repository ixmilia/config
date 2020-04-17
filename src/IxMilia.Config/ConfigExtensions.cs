// Copyright (c) IxMilia.  All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IxMilia.Config
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ConfigPathAttribute : Attribute
    {
        public string Path { get; }

        public ConfigPathAttribute(string path)
        {
            Path = path;
        }
    }

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

        public static bool TryParseValue<T>(this string str, out T result)
        {
            return str.TryParseValue(GetParseFunction<T>(), out result);
        }

        public static bool TryParseValue<T>(this string str, Func<string, T> parser, out T result)
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
            if (str.TryParseValue(parser, out result))
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
                return value.TryParseValue<T>(parser, out result);
            }
            else
            {
                result = default(T);
                return false;
            }
        }

        public static void TryParseAssign<T>(this IDictionary<string, string> dictionary, string key, ref T target)
        {
            dictionary.TryParseAssign(key, GetParseFunction<T>(), ref target);
        }

        public static void TryParseAssign<T>(this IDictionary<string, string> dictionary, string key, Func<string, T> parser, ref T target)
        {
            T result;
            if (dictionary.TryParseValue(key, parser, out result))
            {
                target = result;
            }
        }

        public static string ToConfigString<T>(this T value)
        {
            var toString = GetToStringFunction(value.GetType());
            return toString?.Invoke(value);
        }

        public static void InsertConfigValue<T>(this IDictionary<string, string> dictionary, string key, T value)
        {
            dictionary[key] = value.ToConfigString();
        }

        public static void DeserializeConfig<T>(this T value, params string[] lines)
        {
            var dictionary = new Dictionary<string, string>();
            dictionary.ParseConfig(lines);
            foreach (var key in dictionary.Keys)
            {
                value.DeserializeProperty(key, dictionary[key]);
            }
        }

        public static string SerializeConfig<T>(this T value, params string[] existingLines)
        {
            var dict = new Dictionary<string, string>();
            foreach (var property in typeof(T).GetRuntimeProperties())
            {
                var configPath = property.GetCustomAttribute<ConfigPathAttribute>();
                var key = configPath?.Path ?? property.Name;
                dict[key] = property.GetValue(value).ToConfigString();
            }

            return dict.WriteConfig(existingLines);
        }

        public static void DeserializeProperty<T>(this T parentObject, string key, string value)
        {
            var property = (from prop in typeof(T).GetRuntimeProperties()
                            let configPath = prop.GetCustomAttribute<ConfigPathAttribute>()
                            let path = configPath?.Path ?? prop.Name
                            where path == key
                            select prop).FirstOrDefault();
            if (property != null)
            {
                // a terrible hack to get the appropriate generic method
                var getParseFunction = typeof(ConfigExtensions).GetRuntimeMethods().Single(m => m.Name == nameof(GetParseFunction));
                getParseFunction = getParseFunction.MakeGenericMethod(property.PropertyType);
                var parser = getParseFunction.Invoke(null, new object[0]);
                var parseInvoke = parser.GetType().GetRuntimeMethod("Invoke", new[] { typeof(string) });
                try
                {
                    var result = parseInvoke.Invoke(parser, new object[] { value });
                    property.SetValue(parentObject, result);
                }
                catch
                {
                }
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

        private static Func<object, string> GetToStringFunction(Type type)
        {
            Func<object, string> toString = null;
            if (type == typeof(string))
            {
                toString = value => EscapeString((string)value);
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                toString = value =>
                {
                    var enm = (Enum)value;

                    // first try a simple `.ToString()` call
                    var simple = enm.ToString();
                    if (Enum.GetNames(type).Contains(simple))
                    {
                        return simple;
                    }

                    // otherwise construct it from the flags
                    var flags = new List<string>();
                    foreach (Enum flag in Enum.GetValues(type))
                    {
                        if (enm.HasFlag(flag))
                        {
                            flags.Add(Enum.GetName(type, flag));
                        }
                    }

                    return String.Join("|", flags);
                };
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var elementToString = GetToStringFunction(elementType);
                toString = value =>
                {
                    var array = (Array)value;
                    var values = Enumerable.Range(0, array.Length).Select(i => elementToString(array.GetValue(i)));
                    return string.Join(";", values);
                };
            }
            else
            {
                // try to find a `.ToString(IFormatProvider)` method
                var toStringMethod = type.GetRuntimeMethod("ToString", new[] { typeof(IFormatProvider) });
                if (toStringMethod != null && !toStringMethod.IsStatic)
                {
                    toString = value => (string)toStringMethod.Invoke(value, new[] { CultureInfo.InvariantCulture });
                }

                if (toString == null)
                {
                    // fall back to `object.ToString()`
                    toString = value => value.ToString();
                }
            }

            return toString;
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

            var isSurroundedByQuotes = (value[0] == '\'' || value[0] == '"') && value[0] == value[value.Length - 1];
            if (!isSurroundedByQuotes)
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

        private static string EscapeString(string value)
        {
            if (value == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\v':
                        sb.Append("\\v");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    default:
                        sb.Append(c);
                        break;
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
