using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NihongoLife.Core
{
    /// <summary>
    /// Small, strict JSON reader/writer for realtime messages, where JsonUtility falls short (the "event" key is a C#
    /// keyword, presence state is a dictionary keyed by user, chat text may hold any character). Objects are
    /// Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double, plus string/bool/null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int i = 0;
            object value = ReadValue(json, ref i);
            SkipWhite(json, ref i);
            if (i != json.Length) throw new FormatException($"Unexpected '{json[i]}' at {i}");
            return value;
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            try { return Parse(json) as Dictionary<string, object>; }
            catch (FormatException) { return null; }
        }

        // ─────────── Typed access ───────────

        public static string Str(this Dictionary<string, object> o, string key, string fallback = null) =>
            o != null && o.TryGetValue(key, out var v) && v != null ? v as string ?? Convert.ToString(v, CultureInfo.InvariantCulture) : fallback;

        public static double Num(this Dictionary<string, object> o, string key, double fallback = 0) =>
            o != null && o.TryGetValue(key, out var v) && v is double d ? d : fallback;

        public static bool Bool(this Dictionary<string, object> o, string key, bool fallback = false) =>
            o != null && o.TryGetValue(key, out var v) && v is bool b ? b : fallback;

        public static Dictionary<string, object> Obj(this Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as Dictionary<string, object> : null;

        public static List<object> Arr(this Dictionary<string, object> o, string key) =>
            o != null && o.TryGetValue(key, out var v) ? v as List<object> : null;

        // ─────────── Writing ───────────

        public static string Serialize(object value)
        {
            var sb = new StringBuilder(128);
            Write(sb, value);
            return sb.ToString();
        }

        public static string Quote(string s)
        {
            var sb = new StringBuilder((s?.Length ?? 0) + 2);
            WriteString(sb, s);
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case RawJson raw: sb.Append(string.IsNullOrEmpty(raw.Json) ? "null" : raw.Json); break;
                case IDictionary<string, object> dict:
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        Write(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    bool firstItem = true;
                    foreach (var item in list)
                    {
                        if (!firstItem) sb.Append(',');
                        firstItem = false;
                        Write(sb, item);
                    }
                    sb.Append(']');
                    break;
                case float f: sb.Append(float.IsFinite(f) ? f.ToString("R", CultureInfo.InvariantCulture) : "0"); break;
                case double d: sb.Append(double.IsFinite(d) ? d.ToString("R", CultureInfo.InvariantCulture) : "0"); break;
                case IFormattable n: sb.Append(n.ToString(null, CultureInfo.InvariantCulture)); break;
                default: WriteString(sb, value.ToString()); break;
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null)
            {
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                            else sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        /// <summary>Already-serialised JSON embedded as-is by <see cref="Serialize"/>.</summary>
        public readonly struct RawJson
        {
            public readonly string Json;
            public RawJson(string json) { Json = json; }
        }

        // ─────────── Reading ───────────

        private static void SkipWhite(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        private static object ReadValue(string s, ref int i)
        {
            SkipWhite(s, ref i);
            if (i >= s.Length) throw new FormatException("Unexpected end");
            char c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return ReadString(s, ref i);
            if (c == 't' && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (c == 'f' && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (c == 'n' && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            return ReadNumber(s, ref i);
        }

        private static Dictionary<string, object> ReadObject(string s, ref int i)
        {
            var o = new Dictionary<string, object>();
            i++; // {
            SkipWhite(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return o; }
            while (true)
            {
                SkipWhite(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new FormatException($"Expected key at {i}");
                string key = ReadString(s, ref i);
                SkipWhite(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException($"Expected ':' at {i}");
                i++;
                o[key] = ReadValue(s, ref i);
                SkipWhite(s, ref i);
                if (i >= s.Length) throw new FormatException("Unclosed object");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return o; }
                throw new FormatException($"Expected ',' or '}}' at {i}");
            }
        }

        private static List<object> ReadArray(string s, ref int i)
        {
            var a = new List<object>();
            i++; // [
            SkipWhite(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return a; }
            while (true)
            {
                a.Add(ReadValue(s, ref i));
                SkipWhite(s, ref i);
                if (i >= s.Length) throw new FormatException("Unclosed array");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return a; }
                throw new FormatException($"Expected ',' or ']' at {i}");
            }
        }

        private static string ReadString(string s, ref int i)
        {
            i++; // opening quote
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("Bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException($"Bad escape \\{e}");
                }
            }
            throw new FormatException("Unclosed string");
        }

        private static double ReadNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == start) throw new FormatException($"Unexpected '{s[start]}' at {start}");
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}
