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
    }
}
