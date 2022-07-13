#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RetroCoreFit
{
    public class FormContent : HttpContent
    {
        private List<KeyValuePair<string, string>> values
            = new List<KeyValuePair<string, string>>();

        public FormContent()
        {
            Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        }

        private byte[]? data;
        private byte[] content => data ??= EncodeValues();

        private byte[] EncodeValues()
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms, System.Text.Encoding.UTF8);
            bool notFirst = false;
            foreach(var pair in values)
            {
                if (notFirst)
                {
                    sw.Write('&');
                }
                else
                    notFirst = true;
                sw.Write(pair.Key);
                sw.Write('=');
                sw.Write(pair.Value);
            }
            sw.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var c = content;
            return stream.WriteAsync(c, 0, c.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.LongLength;
            return true;
        }

        public void Add(string name, string value, bool encode = true)
        {
            name = name.EscapeUriComponent();
            if (encode)
            {
                value = value.EscapeUriComponent();
            }
            this.values.Add(new KeyValuePair<string, string>(name, value));
        }
    }
}
