#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RetroCoreFit
{

    public delegate HttpRequestMessage BuilderDelegate(HttpRequestMessage msg);


    public delegate void RequestBuilderHttpClientLogger(HttpRequestMessage request, HttpResponseMessage response);
    public delegate ValueTask RequestBuilderHttpClientLoggerAsync(HttpRequestMessage request, HttpResponseMessage response);

    public class RequestBuilder
    {
        protected BuilderDelegate Handler;

        protected RequestBuilderHttpClientLoggerAsync? loggerAsync;

        public HttpRequestMessage Build()
        {
            var m = Handler(null!);
            if(m.Content is FormContent fc)
            {
                m.Content = new FormUrlEncodedContent(fc.Values);
            }
            return m;
        }

        private (HttpRequestMessage,RequestBuilderHttpClientLoggerAsync? loggerAsync) BuildWithLogger()
        {
            return (Build(),loggerAsync);
        }

        protected static RequestBuilder Append(RequestBuilder @this, BuilderDelegate fx)
        {
            return new RequestBuilder() { 
                Handler = (prev) => fx(@this.Handler(prev)),
                loggerAsync = @this.loggerAsync
            };
        }

        public RequestBuilder Query(string name, string value, bool encode = true)
        {
            return Append(this, @this => {
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
                    url += $"&{name.EscapeUriComponent()}={value}";
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
            return Append(this, @this =>
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

        public RequestBuilder Post() => Append(this, (@this) => {
            @this.Method = HttpMethod.Post;
            return @this;
        });

        public RequestBuilder Put() => Append(this, (@this) => {
            @this.Method = HttpMethod.Put;
            return @this;
        });

        public RequestBuilder Patch() => Append(this, (@this) => {
            @this.Method = new HttpMethod("PATCH");
            return @this;
        });
        public RequestBuilder Delete() => Append(this, (@this) => {
            @this.Method = HttpMethod.Delete;
            return @this;
        });

        public static RequestBuilder Post(string url) => new RequestBuilder
        {
            Handler = (_) => new HttpRequestMessage(HttpMethod.Post, url)
        };

        public static RequestBuilder Put(string url) => new RequestBuilder
        {
            Handler = (_) => new HttpRequestMessage(HttpMethod.Put, url)
        };

        public static RequestBuilder Patch(string url) => new RequestBuilder
        {
            Handler = (_) => new HttpRequestMessage(new HttpMethod("PATCH"), url)
        };

        public static RequestBuilder Delete(string url) => new RequestBuilder
        {
            Handler = (_) => new HttpRequestMessage(HttpMethod.Delete, url)
        };

        public RequestBuilder Header(string name, string value, bool validate = false)
        {
            return Append(this, @this =>
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
            return Append(this, @this =>
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
            return Append(this, @this =>
            {
                var uri = new UriBuilder(@this.RequestUri)
                {
                    Scheme = scheme
                };
                @this.RequestUri = uri.Uri;
                return @this;
            });
        }

        public RequestBuilder Content(HttpContent content) {
            return Append(this, @this =>
            {
                @this.Content = content;
                return @this;
            });
        }

        public RequestBuilder Body<T>(T body, System.Text.Json.JsonSerializerOptions? options = null, string contentType = "application/json")
        {
            return Append(this, @this => {
                if (@this.Content != null)
                {
                    throw new ArgumentException($"Body is already set");
                }
                @this.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body, options),
                    System.Text.Encoding.UTF8, contentType);
                return @this;
            });
        }


        public RequestBuilder Form(string name, string value, bool encode = true)
        {
            return Append(this, @this =>
            {
                if (@this.Content is not FormContent fc)
                {
                    @this.Content = fc = new FormContent();
                }
                fc.Add(name, value, encode);
                return @this;
            });
        }

        public RequestBuilder Form(string name, long value)
        {
            return Form(name, value.ToString(), false);
        }

        public RequestBuilder Form(string name, int value)
        {
            return Form(name, value.ToString(), false);
        }
        public RequestBuilder Form(string name, bool value)
        {
            return Form(name, value ? "true" : "false", false);
        }

        public RequestBuilder Form(string name, double value)
        {
            return Form(name, value.ToString());
        }
        public RequestBuilder Form(string name, decimal value)
        {
            return Form(name, value.ToString());
        }

        public RequestBuilder Multipart(string name, string value)
        {
            return Append(this, @this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new StringContent(value), name);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(
            string name,
            string fileContent,
            string fileName = "file.dat",
            string? contentType = null)
        {
            return Append(this, @this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new StringContent(fileContent).WithContentType(contentType), name, fileName);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(
            string name,
            byte[] fileContent,
            string fileName = "file.dat",
            string? contentType = null)
        {
            return Append(this,@this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new ByteArrayContent(fileContent).WithContentType(contentType), name, fileName);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(
            string name,
            Stream fileContent,
            string fileName = "file.dat",
            string? contentType = null)
        {
            return Append(this, @this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(new StreamContent(fileContent).WithContentType(contentType), name, fileName);
                return @this;
            });
        }

        public RequestBuilder MultipartFile(string name, HttpContent fileContent,
            string fileName = "file.dat")
        {
            return Append(this, @this =>
            {
                if (@this.Content is not MultipartFormDataContent mfd)
                {
                    @this.Content = mfd = new MultipartFormDataContent();
                }
                mfd.Add(fileContent, name, fileName);
                return @this;
            });
        }

        public RequestBuilder WithLogger(RequestBuilderHttpClientLogger logger)
        {
            return new RequestBuilder() { 
                Handler = this.Handler,
                loggerAsync = (req, res) => {
                    logger(req, res);
                    return default;
                }
            };
        }
        public RequestBuilder WithAsyncLogger(RequestBuilderHttpClientLoggerAsync loggerAsync)
        {
            return new RequestBuilder() { 
                Handler = this.Handler,
                loggerAsync = loggerAsync
            };
        }
        
        public static RequestBuilder Get(string baseUrl) => 
            new RequestBuilder() { Handler = (_) => new HttpRequestMessage(HttpMethod.Get, baseUrl) };

        public static RequestBuilder New(string baseUrl) =>
            new RequestBuilder() { Handler = (_) => new HttpRequestMessage(HttpMethod.Get, baseUrl) };


        public Task<HttpResponseMessage> AsResponseMessageAsync(
            HttpClient client,
            CancellationToken cancellation = default)
        {
            var (req, loggerAsync) = this.BuildWithLogger();
            if(loggerAsync != null)
            {
                return SendAsync(client, req, loggerAsync, HttpCompletionOption.ResponseContentRead, cancellation);
            }
            return client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancellation);
        }

        public async Task<string> AsTextAsync(
            HttpClient client,
            CancellationToken cancellation = default)
        {
            var (req, loggerAsync) = this.BuildWithLogger();

            var r = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cancellation);
            if(loggerAsync != null) {
                await loggerAsync(req, r);
            }
            return await r.Content.ReadAsStringAsync();
        }


        private async Task<HttpResponseMessage> SendAsync(
            HttpClient client,
            HttpRequestMessage req,
            RequestBuilderHttpClientLoggerAsync logger,
            HttpCompletionOption option,
            CancellationToken cancellationToken)
        {
            var r = await client.SendAsync(req, option, cancellationToken);
            await logger(req, r);
            return r;
        }

        public async Task<T?> AsJsonAsync<T>(
            HttpClient client,
            CancellationToken cancellation = default,
            System.Text.Json.JsonSerializerOptions? options = null
            )
        {
            var (req, logger) = this.BuildWithLogger();
            logger ??= (a,b) => default;
            using(var r = await SendAsync(client, req, logger, HttpCompletionOption.ResponseHeadersRead, cancellation))
            {
                if (!r.IsSuccessStatusCode)
                {
                    var responseText = await r.Content.ReadAsStringAsync();
                    if (r.Content.Headers.ContentType?.MediaType?.Contains("json") ?? false)
                    {
                        var token= Newtonsoft.Json.Linq.JToken.Parse(responseText);
                        throw new ApiException(req.RequestUri.ToString(), r.StatusCode, responseText, token);
                    }
                    throw new ApiException(req.RequestUri.ToString(), r.StatusCode, responseText, null);
                }

                using var stream = await r.Content.ReadAsStreamAsync();
                if (typeof(IApiResponse).IsAssignableFrom(typeof(T)))
                {
                    var tx = (Activator.CreateInstance<T>() as IApiResponse)!;
                    var model = await System.Text.Json.JsonSerializer.DeserializeAsync(
                    stream,
                    tx.GetModelType(),
                    options,
                    cancellationToken: cancellation);
                    tx.Initialize(r, model);
                    return (T)tx;
                }

                return await System.Text.Json.JsonSerializer.DeserializeAsync<T>(
                    stream, 
                    options,
                    cancellationToken: cancellation);
            }
        }

    }
}
