using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace AutomaEx
{
    /// <summary>
    /// HtmlFormPost
    /// </summary>
    public class HtmlFormPost
    {
        /// <summary>
        /// Enum Mode
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// The form
            /// </summary>
            Form,
            /// <summary>
            /// The json
            /// </summary>
            Json,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlFormPost" /> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="s">The s.</param>
        /// <param name="marker">The marker.</param>
        /// <exception cref="InvalidOperationException">unable to find marker</exception>
        public HtmlFormPost(Mode mode, string s, string marker = null)
        {
            Values = new Dictionary<string, string>();
            Types = new Dictionary<string, string>();
            Checkboxs = new Dictionary<string, bool>();
            // advance
            var idxZ = 0;
            if (marker != null)
            {
                idxZ = s.IndexOf(marker) + marker.Length;
                if (idxZ < marker.Length)
                    throw new InvalidOperationException("unable to find marker");
            }
            //
            switch (mode)
            {
                case Mode.Json:
                    {
                        var end = s.IndexOfSkip("}", idxZ);
                        s = s.Substring(idxZ, end - idxZ);
                        var r = JObject.Parse(s);
                        foreach (var v in r)
                            Values.Add(v.Key, (string)v.Value);
                        return;
                    }
                case Mode.Form:
                    {
                        var end = s.IndexOfSkip("</form>", idxZ);
                        s = s.Substring(idxZ, (end != -1 ? end : 0) - idxZ);
                        s = s.Replace("\"", "'");
                        // parse
                        while (true)
                        {
                            var start_input = s.IndexOfSkip("<input"); var start_select = s.IndexOfSkip("<select"); var start = Math.Min(start_input != 5 ? start_input : int.MaxValue, start_select != 6 ? start_select : int.MaxValue);
                            if (start == int.MaxValue)
                                break;
                            end = s.IndexOfSkip(">", start);
                            // element
                            string type;
                            if (start == start_input)
                            {
                                var typeIdx = s.IndexOfSkip(" type='", start); var typeIdx2 = s.IndexOf("'", typeIdx); type = typeIdx < end ? s.Substring(typeIdx, typeIdx2 - typeIdx) : null;
                            }
                            else if (start == start_select)
                            {
                                var multipleIdx = s.IndexOfSkip("multiple", start); type = multipleIdx < end ? "multiple" : "select";
                            }
                            else type = "unknown";
                            var name = s.ExtractSpanInner(" name='", "'", start, end);
                            var value = s.ExtractSpanInner(" value='", "'", start, end);
                            if (name != null && !Values.ContainsKey(name))
                            {
                                Values.Add(name, value);
                                Types.Add(name, type);
                                if (type == "checkbox")
                                    Checkboxs.Add(name, false);
                            }
                            // advance
                            s = s.Substring(end + 1);
                        }
                        return;
                    }
            }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public Dictionary<string, string> Values { get; private set; }

        /// <summary>
        /// Gets the types.
        /// </summary>
        /// <value>The types.</value>
        public Dictionary<string, string> Types { get; private set; }

        /// <summary>
        /// Gets the checkboxs.
        /// </summary>
        /// <value>The checkboxs.</value>
        public Dictionary<string, bool> Checkboxs { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            var b = new StringBuilder();
            foreach (var v in Values)
            {
                string t;
                var hasValue = !Types.TryGetValue(v.Key, out t) || t != "checkbox" || Checkboxs[v.Key];
                if (hasValue) b.Append($"{v.Key}={HttpUtility.UrlEncode(v.Value)}&");
                else b.Append($"{v.Key}=&");
            }
            b.Length--;
            return b.ToString();
        }
    }
}