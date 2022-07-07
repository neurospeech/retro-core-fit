#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RetroCoreFit
{
    internal static class HttpExtensions
    {
        public static T WithContentType<T>(this T content, string? contentType)
            where T: HttpContent
        {
            if (contentType != null)
            {
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }
            return content;
        }

        public static Task<HttpResponseMessage> GetResponseAsync(
            this RequestBuilder request,
            HttpClient client,
            CancellationToken cancellation = default)
        {
            var req = request.Build();
            return client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancellation);
        }

        public static async Task<T?> GetResponseAsync<T>(
            this RequestBuilder request,
            HttpClient client,
            CancellationToken cancellation = default,
            System.Text.Json.JsonSerializerOptions? options = null)
        {
            var req = request.Build();
            using(var r = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellation))
            {
                if (!r.IsSuccessStatusCode)
                {
                    var responseText = await r.Content.ReadAsStringAsync();
                    throw new ApiException(req.RequestUri.ToString(), r.StatusCode, responseText, null);
                }
                return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
                    await r.Content.ReadAsStreamAsync(), 
                    options,
                    cancellationToken: cancellation);
            }
        }

        public static async Task<ApiResponse<T>?> GetResponseWithHeadersAsync<T>(
            this RequestBuilder request, 
            HttpClient client,
            CancellationToken cancellation = default,
            System.Text.Json.JsonSerializerOptions? options = null)
        {
            var req = request.Build();
            using (var r = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!r.IsSuccessStatusCode)
                {
                    var responseText = await r.Content.ReadAsStringAsync();
                    throw new ApiException(req.RequestUri.ToString(), r.StatusCode, responseText, null);
                }
                var model = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
                    await r.Content.ReadAsStreamAsync(),
                    options,
                    cancellationToken: cancellation);
                var result = new ApiResponse<T>();
                result.Initialize(r, model);
                return result;
            }
        }

    }

    public class RequestBuilder
    {
        private List<Func<HttpRequestMessage, HttpRequestMessage>> funcs = new();

        protected RequestBuilder(Func<HttpRequestMessage, HttpRequestMessage> fx)
        {
            funcs.Add(fx);
        }

        private RequestBuilder(
            List<Func<HttpRequestMessage, HttpRequestMessage>> funcs,
            Func<HttpRequestMessage, HttpRequestMessage> func)
        {
            this.funcs = new(funcs)
            {
                func
            };
        }

        protected RequestBuilder Append(Func<HttpRequestMessage, HttpRequestMessage> func)
        {
            return new RequestBuilder(funcs, func);
        }

        public static implicit operator HttpRequestMessage(RequestBuilder builder)
        {
            return builder.Build();
        }

        public HttpRequestMessage Build()
        {
            HttpRequestMessage msg = null!;
            foreach(var f in funcs)
            {
                msg = f(msg);
            }
            return msg;
        }

        public static RequestBuilder Post(string url) => new RequestBuilder((_) => new HttpRequestMessage(HttpMethod.Post, url));

        public static RequestBuilder Put(string url) => new RequestBuilder((_) => new HttpRequestMessage(HttpMethod.Put, url));

        public static RequestBuilder Get(string url) => new RequestBuilder((_) => new HttpRequestMessage(HttpMethod.Get, url));

        public static RequestBuilder Patch(string url) => new RequestBuilder((_) => new HttpRequestMessage(new HttpMethod("PATCH"), url));

        public static RequestBuilder Delete(string url) => new RequestBuilder((_) => new HttpRequestMessage(HttpMethod.Delete, url));


        public RequestBuilder Body<T>(T body, System.Text.Json.JsonSerializerOptions? options = null)
        {
            return this.Append(@this => { 
                if (@this.Content != null)
                {
                    throw new ArgumentException($"Body is already set");
                }
                @this.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body, options),
                    System.Text.Encoding.UTF8);
                return @this;
            });
        }

        public RequestBuilder Query(string name, string value)
        {
            return this.Append(@this => {
                var url = @this.RequestUri.ToString();
                if (url.IndexOf('?') == -1)
                {
                    url += $"?{name}={Uri.EscapeUriString(value)}";
                } else
                {
                    url += $"{name}={Uri.EscapeUriString(value)}";
                }
                @this.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                return @this;
            });
        }

        public RequestBuilder Path(string name, string? value, bool encode = false)
        {
            return this.Append(@this =>
            {
                value ??= "";
                if (encode && value.Length > 0)
                {
                    value = Uri.EscapeDataString(value).Replace("%20", "+");
                }
                var url = @this.RequestUri.OriginalString
                    .Replace($"{{{name}}}", value);
                @this.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                return @this;
            });
        }

        public RequestBuilder Form(string name, string value)
        {
            return this.Append(@this =>
            {
                if(@this.Content is not FormContent fc)
                {
                    @this.Content = fc = new FormContent();
                }
                fc.Add(name, value);
                return @this;
            });
        }

        public RequestBuilder Multipart(string name, string value)
        {
            return this.Append(@this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new StringContent(value), name);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(string name, string fileContent, string? contentType = null)
        {
            return this.Append(@this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new StringContent(fileContent).WithContentType(contentType), name);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(string name, byte[] fileContent, string? contentType = null)
        {
            return this.Append(@this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new ByteArrayContent(fileContent).WithContentType(contentType), name);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(string name, Stream fileContent, string? contentType = null)
        {
            return this.Append(@this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new StreamContent(fileContent).WithContentType(contentType), name);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(string name, HttpContent fileContent)
        {
            return this.Append(@this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(fileContent, name);
                return @this;
            });
        }

        public RequestBuilder Header(string name, string value, bool validate = false)
        {
            return this.Append(@this =>
            {
                if (validate)
                {
                    @this.Headers.Add(name, value);
                }
                else
                {
                    @this.Headers.TryAddWithoutValidation(name, value);
                }
                return @this;
            });            
        }

        public RequestBuilder Host(string host, int? port = null)
        {
            return this.Append(@this =>
            {
                var uri = new UriBuilder(@this.RequestUri)
                {
                    Host = host
                };
                if (port != null)
                {
                    uri.Port = port.Value;
                }
                @this.RequestUri = uri.Uri;
                return @this;
            });
        }

        public RequestBuilder Scheme(string scheme)
        {
            return this.Append(@this =>
            {
                var uri = new UriBuilder(@this.RequestUri)
                {
                    Scheme = scheme
                };
                @this.RequestUri = uri.Uri;
                return @this;
            });
        }
    }

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
                sw.Write(Encode(pair.Key));
                sw.Write('=');
                sw.Write(Encode(pair.Value));
            }
            sw.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }

        private static string Encode(string data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return String.Empty;
            }
            // Escape spaces as '+'.
            return Uri.EscapeDataString(data).Replace("%20", "+");
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

        public void Add(string name, string value)
        {
            this.values.Add(new KeyValuePair<string, string>(name, value));
        }
    }
}
