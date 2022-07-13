#nullable enable
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
}
