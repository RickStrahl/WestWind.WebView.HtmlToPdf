using System.Text;

namespace Westwind.WebView.HtmlToPdf.Utilities
{
    internal static class StringUtils
    {
        /// <summary>
        /// A helper to generate a JSON string from a string value
        /// 
        /// Use this to avoid bringing in a full JSON Serializer for
        /// scenarios of string serialization.
        ///         
        /// Note: Function includes surrounding quotes!
        /// </summary>
        /// <param name="text"></param>
        /// <returns>JSON encoded string ("text"), empty ("") or "null".</returns>
        public static string ToJsonString(string text, bool noQuotes = false)
        {
            if (text is null)
                return "null";

            var sb = new StringBuilder(text.Length);
            
            if (!noQuotes)
                sb.Append("\"");

            var ct = text.Length;

            for (int x = 0; x < ct; x++)
            {
                var c = text[x];

                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
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
                    default:
                        uint i = c;
                        if (i < 32)  // || i > 255
                            sb.Append($"\\u{i:x4}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            if (!noQuotes)
                sb.Append("\"");

            return sb.ToString();
        }
    }
}
