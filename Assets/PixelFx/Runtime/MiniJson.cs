using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PixelFx
{
    /// <summary>
    /// A small JSON reader (objects → Dictionary&lt;string, object&gt;, arrays → List&lt;object&gt;, numbers → double).
    /// Unity's JsonUtility can't read pixelfx's mixed arrays (e.g. act poses), so the sheet JSON is read with this.
    /// No UnityEngine dependency, so it is unit-testable outside the editor.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new Parser(json);
            var v = p.Value();
            p.SkipWs();
            if (!p.End) throw p.Error("trailing characters");
            return v;
        }

        sealed class Parser
        {
            readonly string s;
            int i;
            public Parser(string s) { this.s = s; }
            public bool End => i >= s.Length;
            public FormatException Error(string what) => new FormatException($"JSON: {what} at {i}");

            public void SkipWs()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            public object Value()
            {
                SkipWs();
                if (End) throw Error("unexpected end");
                char c = s[i];
                switch (c)
                {
                    case '{': return Obj();
                    case '[': return Arr();
                    case '"': return Str();
                    case 't': Lit("true"); return true;
                    case 'f': Lit("false"); return false;
                    case 'n': Lit("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return Num();
                        throw Error($"unexpected '{c}'");
                }
            }

            void Lit(string w)
            {
                if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) throw Error("bad literal");
                i += w.Length;
            }

            Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                i++;
                SkipWs();
                if (!End && s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    if (End || s[i] != '"') throw Error("expected key");
                    string k = Str();
                    SkipWs();
                    if (End || s[i] != ':') throw Error("expected ':'");
                    i++;
                    d[k] = Value();
                    SkipWs();
                    if (End) throw Error("unterminated object");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw Error("expected ',' or '}'");
                }
            }

            List<object> Arr()
            {
                var a = new List<object>();
                i++;
                SkipWs();
                if (!End && s[i] == ']') { i++; return a; }
                while (true)
                {
                    a.Add(Value());
                    SkipWs();
                    if (End) throw Error("unterminated array");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return a; }
                    throw Error("expected ',' or ']'");
                }
            }

            string Str()
            {
                var sb = new StringBuilder();
                i++;
                while (true)
                {
                    if (End) throw Error("unterminated string");
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (End) throw Error("bad escape");
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
                            if (i + 4 > s.Length) throw Error("bad \\u escape");
                            sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        default: throw Error("bad escape");
                    }
                }
            }

            double Num()
            {
                int start = i;
                if (s[i] == '-') i++;
                while (i < s.Length && "0123456789.eE+-".IndexOf(s[i]) >= 0) i++;
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
