#nullable enable
using System;

namespace RetroCoreFit
{
    internal static class UrlExtensions
    {
        public static string EscapeUriComponent(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return Uri.UnescapeDataString(value).Replace("%20", "+");
        }

        public static string EscapeUriComponent(this FormattableString text)
        {
            var supplied = text.GetArguments();
            var args = new object?[text.ArgumentCount];
            for (int i = 0; i < args.Length; i++)
            {
                var v = supplied[i];
                if (v is Literal literal)
                {
                    args[i] = literal.Value;
                    continue;
                }
                args[i] = v != null ? v.ToString().EscapeUriComponent() : v ;
            }
            return string.Format(text.Format, args);
        }
    }
}
