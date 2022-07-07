using System;
using System.Net;

namespace RetroCoreFit
{
    public class HttpException : Exception
    {
        public string Path { get; }

        public HttpStatusCode StatusCode { get; }
        public HttpException(
            string path,
            HttpStatusCode statusCode,
            string content)
            : base(content)
        {
            this.Path = path;
            this.StatusCode = statusCode;
        }

        public override string ToString()
        {
            var error = $"Status: {StatusCode}, Error = {Message}\r\nUrl: {this.Path}\r\n{this.StackTrace}";
            return error;
        }
    }
}
