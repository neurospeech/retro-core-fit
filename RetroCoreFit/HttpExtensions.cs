﻿#nullable enable
using System;
using System.Net.Http;
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
            System.Text.Json.JsonSerializerOptions? options = null,
            Func<HttpRequestMessage, Task>? requestLogger = null,
            Func<HttpResponseMessage, Task>? responseLogger = null
            )
        {
            var req = request.Build();
            if (requestLogger != null)
            {
                try
                {
                    await requestLogger(req);
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            using(var r = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellation))
            {
                if (responseLogger != null) {
                    try
                    {
                        await responseLogger(r);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }
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
}
