using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace RetroCoreFit
{
    public class ApiException : HttpException
    {
        public JToken Details { get; }

        public ApiException(
            string path,
            HttpStatusCode statusCode,
            string message,
            JToken details)
            : base(path, statusCode, message)
        {
            this.Details = details;
        }

        public override string ToString()
        {
            var error = $"Status: {StatusCode}, Error = {Message}\r\nUrl: {this.Path}\r\n{Details.ToString(Formatting.Indented)}\r\n{this.StackTrace}";
            return error;
        }
    }
}
