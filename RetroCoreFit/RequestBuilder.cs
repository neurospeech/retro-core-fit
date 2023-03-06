#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace RetroCoreFit
{

    public delegate HttpRequestMessage BuilderDelegate(HttpRequestMessage msg);

    public class RequestBuilder
    {
        protected BuilderDelegate Handler;

        public HttpRequestMessage Build()
        {
            var m = Handler(null!);
            if(m.Content is FormContent fc)
            {
                m.Content = new FormUrlEncodedContent(fc.Values);
            }
            return m;
        }

        protected static RequestBuilder Append(RequestBuilder @this, BuilderDelegate fx)
        {
            return new RequestBuilder() { 
                Handler = (prev) => fx(@this.Handler(prev))
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

        public RequestBuilder Body<T>(T body, System.Text.Json.JsonSerializerOptions? options = null)
        {
            return Append(this, @this => {
                if (@this.Content != null)
                {
                    throw new ArgumentException($"Body is already set");
                }
                @this.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body, options),
                    System.Text.Encoding.UTF8, "application/json");
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
        
        public static RequestBuilder Get(string baseUrl) => 
            new RequestBuilder() { Handler = (_) => new HttpRequestMessage(HttpMethod.Get, baseUrl) };

        public static RequestBuilder New(string baseUrl) =>
            new RequestBuilder() { Handler = (_) => new HttpRequestMessage(HttpMethod.Get, baseUrl) };

    }
}
