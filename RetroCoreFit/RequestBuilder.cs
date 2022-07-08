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
    public static class HttpExtensions
    {
        internal static T WithContentType<T>(this T content, string? contentType)
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

                if (typeof(IApiResponse).IsAssignableFrom(typeof(T)))
                {
                    var tx = (Activator.CreateInstance<T>() as IApiResponse)!;
                    var model = await System.Text.Json.JsonSerializer.DeserializeAsync(
                    await r.Content.ReadAsStreamAsync(),
                    tx.GetModelType(),
                    options,
                    cancellationToken: cancellation);
                    tx.Initialize(r, model);
                    return (T)tx;
                }

                return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
                    await r.Content.ReadAsStreamAsync(), 
                    options,
                    cancellationToken: cancellation);
            }
        }

        //public static async Task<ApiResponse<T>?> GetResponseWithHeadersAsync<T>(
        //    this RequestBuilder request, 
        //    HttpClient client,
        //    CancellationToken cancellation = default,
        //    System.Text.Json.JsonSerializerOptions? options = null)
        //{
        //    var req = request.Build();
        //    using (var r = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
        //    {
        //        if (!r.IsSuccessStatusCode)
        //        {
        //            var responseText = await r.Content.ReadAsStringAsync();
        //            throw new ApiException(req.RequestUri.ToString(), r.StatusCode, responseText, null);
        //        }
        //        var model = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
        //            await r.Content.ReadAsStreamAsync(),
        //            options,
        //            cancellationToken: cancellation);
        //        var result = new ApiResponse<T>();
        //        result.Initialize(r, model);
        //        return result;
        //    }
        //}

    }

    public delegate HttpRequestMessage BuilderDelegate(HttpRequestMessage msg);

    public class RequestBuilder
    {
        protected BuilderDelegate Handler;

        public HttpRequestMessage Build() => Handler(null!);

        protected static T Append<T>(RequestBuilder @this, BuilderDelegate fx)
            where T: RequestBuilder, new()
        {
            return new T() { 
                Handler = (prev) => fx(@this.Handler(prev))
            };
        }

        public RequestBuilder Query(string name, string value, bool encode = true)
        {
            return Append<RequestBuilder>(this, @this => {
                var url = @this.RequestUri.ToString();
                if (encode)
                {
                    value = value.EscapeUriComponent();
                }
                if (url.IndexOf('?') == -1)
                {
                    url += $"?{name.EscapeUriComponent()}={value}";
                }
                else
                {
                    url += $"{name.EscapeUriComponent()}={value}";
                }
                @this.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);
                return @this;
            });
        }

        public RequestBuilder Query(string name, long value)
        {
            return Query(name, value.ToString(), false);
        }

        public RequestBuilder Query(string name, int value)
        {
            return Query(name, value.ToString(), false);
        }

        public RequestBuilder Query(string name, bool value)
        {
            return Query(name, value ? "true" : "false", false);
        }


        public RequestBuilder Query(string name, double value)
        {
            return Query(name, value.ToString());
        }

        public RequestBuilder Query(string name, decimal value)
        {
            return Query(name, value.ToString());
        }

        public RequestBuilder Path(string name, string? value, bool encode = false)
        {
            return Append<RequestBuilder>(this, @this =>
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

        public BodyBuilder Post() => Append<BodyBuilder>(this, (@this) => {
            @this.Method = HttpMethod.Post;
            return @this;
        });

        public BodyBuilder Put() => Append<BodyBuilder>(this, (@this) => {
            @this.Method = HttpMethod.Put;
            return @this;
        });

        public BodyBuilder Patch() => Append<BodyBuilder>(this, (@this) => {
            @this.Method = new HttpMethod("PATCH");
            return @this;
        });
        public BodyBuilder Delete() => Append<BodyBuilder>(this, (@this) => {
            @this.Method = HttpMethod.Delete;
            return @this;
        });

        public static BodyBuilder Post(string url) => new BodyBuilder {
            Handler = (_) => new HttpRequestMessage(HttpMethod.Post, url)
        };

        public static BodyBuilder Put(string url) => new BodyBuilder
        {
            Handler = (_) => new HttpRequestMessage(HttpMethod.Put, url)
        };

        public static BodyBuilder Patch(string url) => new BodyBuilder
        {
            Handler = (_) => new HttpRequestMessage(new HttpMethod("PATCH"), url)
        };
        public static BodyBuilder Delete(string url) => new BodyBuilder
        {
            Handler = (_) => new HttpRequestMessage(HttpMethod.Delete, url)
        };


        public RequestBuilder Header(string name, string value, bool validate = false)
        {
            return Append<RequestBuilder>(this, @this =>
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
            return Append<RequestBuilder>(this, @this =>
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
            return Append<RequestBuilder>(this, @this =>
            {
                var uri = new UriBuilder(@this.RequestUri)
                {
                    Scheme = scheme
                };
                @this.RequestUri = uri.Uri;
                return @this;
            });
        }

        public class BodyBuilder: RequestBuilder
        {
            public BodyBuilder Body<T>(T body, System.Text.Json.JsonSerializerOptions? options = null)
            {
                return Append<BodyBuilder>(this, @this => {
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


            public BodyBuilder Form(string name, string value, bool encode = true)
            {
                return Append<BodyBuilder>(this, @this =>
                {
                    if (@this.Content is not FormContent fc)
                    {
                        @this.Content = fc = new FormContent();
                    }
                    fc.Add(name, value, encode);
                    return @this;
                });
            }

            public BodyBuilder Form(string name, long value)
            {
                return Form(name, value.ToString(), false);
            }

            public BodyBuilder Form(string name, int value)
            {
                return Form(name, value.ToString(), false);
            }
            public BodyBuilder Form(string name, bool value)
            {
                return Form(name, value ? "true" : "false", false);
            }

            public BodyBuilder Form(string name, double value)
            {
                return Form(name, value.ToString());
            }
            public BodyBuilder Form(string name, decimal value)
            {
                return Form(name, value.ToString());
            }

            public BodyBuilder Multipart(string name, string value)
            {
                return Append<BodyBuilder>(this, @this =>
                {
                    if (@this.Content is not MultipartFormDataContent mfd)
                    {
                        @this.Content = mfd = new MultipartFormDataContent();
                    }
                    mfd.Add(new StringContent(value), name);
                    return @this;
                });
            }

            public BodyBuilder MultipartFile(string name, string fileContent, string? contentType = null)
            {
                return Append<BodyBuilder>(this, @this =>
                {
                    if (@this.Content is not MultipartFormDataContent mfd)
                    {
                        @this.Content = mfd = new MultipartFormDataContent();
                    }
                    mfd.Add(new StringContent(fileContent).WithContentType(contentType), name);
                    return @this;
                });
            }

            public BodyBuilder MultipartFile(string name, byte[] fileContent, string? contentType = null)
            {
                return Append<BodyBuilder>(this,@this =>
                {
                    if (@this.Content is not MultipartFormDataContent mfd)
                    {
                        @this.Content = mfd = new MultipartFormDataContent();
                    }
                    mfd.Add(new ByteArrayContent(fileContent).WithContentType(contentType), name);
                    return @this;
                });
            }

            public BodyBuilder MultipartFile(string name, Stream fileContent, string? contentType = null)
            {
                return Append<BodyBuilder>(this, @this =>
                {
                    if (@this.Content is not MultipartFormDataContent mfd)
                    {
                        @this.Content = mfd = new MultipartFormDataContent();
                    }
                    mfd.Add(new StreamContent(fileContent).WithContentType(contentType), name);
                    return @this;
                });
            }

            public BodyBuilder MultipartFile(string name, HttpContent fileContent)
            {
                return Append<BodyBuilder>(this, @this =>
                {
                    if (@this.Content is not MultipartFormDataContent mfd)
                    {
                        @this.Content = mfd = new MultipartFormDataContent();
                    }
                    mfd.Add(fileContent, name);
                    return @this;
                });
            }
        }
        public static RequestBuilder Get(string baseUrl) => 
            new RequestBuilder() { Handler = (_) => new HttpRequestMessage(HttpMethod.Get, baseUrl) };

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
