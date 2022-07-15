#nullable enable

using System;

namespace RetroCoreFit
{
    public readonly struct Escaped
    {
        public readonly string Value;

        public Escaped(string value)
        {
            this.Value = value;
        }

        public override string ToString()
        {
            return Value.EscapeUriComponent();
        }

        public static explicit operator Escaped(string s) => new Escaped(s);

        public static explicit operator Escaped(float s) => new Escaped(s.ToString());
        
        public static explicit operator Escaped(double s) => new Escaped(s.ToString());

        public static string String(string s) => s.EscapeUriComponent();
    }
}
