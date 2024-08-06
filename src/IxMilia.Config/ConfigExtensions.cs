using System;
using System.Collections.Generic;
using System.Linq;
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
            var lines = new List<string>();
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
                        lines.Add(existing);
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
                            var value = dictionary[fullKey];
                            if (value != null)
                            {
                                lines.Add(MakeLine(shortKey, value));
                            }
                        }

                        prefix = GetSectionName(existing);
                        if (sortedKeys.Any(k => k.Item1 == prefix))
                        {
                            // only do this if there's actually something to be written
                            if (extraKeys.Count > 0)
                            {
                                lines.Add(string.Empty);
                            }

                            // re-copy this line and note what section we're in
                            lines.Add(existing);
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
                            var value = dictionary[fullKey];
                            if (value != null)
                            {
                                lines.Add(MakeLine(kvp.Key, value));
                            }
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
                    lines.Add(string.Empty);
                    lines.Add(string.Concat("[", nextPrefix, "]"));
                    prefix = nextPrefix;
                }

                var value = dictionary[MakeFullKey(key)];
                if (value != null)
                {
                    lines.Add(MakeLine(shortKey, value));
                }
            }

            // remove blank lines within the same section
            string lastSectionName = null;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (IsSectionName(line))
                {
                    lastSectionName = GetSectionName(line);
                }
                else
                {
                    var isLastLine = i == lines.Count - 1;
                    if (!isLastLine)
                    {
                        var nextLine = lines[i + 1];
                        if (string.IsNullOrEmpty(line) && !IsSectionName(nextLine))
                        {
                            // found a blank line in the middle of a section; remove it
                            lines.RemoveAt(i);
                            i--; // back off index
                        }
                    }
                }
            }

            var result = string.Join(Environment.NewLine, lines);
            return result;
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
            var parsedValue = ParseString(value);
            return new KeyValuePair<string, string>(key, parsedValue);
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
            var escapedValue = EscapeString(value);
            return string.Concat(key, " = ", escapedValue);
        }

        internal static string ParseString(string value)
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

        internal static string EscapeString(string value)
        {
            if (value == null)
            {
                return null;
            }

            var neededEscaping = false;
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\f':
                        neededEscaping = true;
                        sb.Append("\\f");
                        break;
                    case '\n':
                        neededEscaping = true;
                        sb.Append("\\n");
                        break;
                    case '\r':
                        neededEscaping = true;
                        sb.Append("\\r");
                        break;
                    case '\t':
                        neededEscaping = true;
                        sb.Append("\\t");
                        break;
                    case '\v':
                        neededEscaping = true;
                        sb.Append("\\v");
                        break;
                    case '\\':
                        neededEscaping = true;
                        sb.Append("\\\\");
                        break;
                    case '"':
                        neededEscaping = true;
                        sb.Append("\\\"");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
            return neededEscaping ? sb.ToString() : value;
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
