using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RetroCoreFit.Tests
{
    [TestClass]
    public class RequestBuilderTests
    {
        [TestMethod]
        public void Build()
        {
            var post = RequestBuilder.Post("/");

            Assert.IsTrue(post.Header("a", "b").Build().Headers.Contains("a"));
            Assert.IsFalse(post.Build().Headers.Contains("a"));
        }

        [TestMethod]
        public void Query()
        {
            var post = RequestBuilder.Post("/");
            var r = post.Query("a", "b").Build();
            Assert.IsTrue(r.RequestUri.OriginalString.Contains("/?a=b"));
            Assert.IsFalse(post.Build().RequestUri.OriginalString.Contains("/?a=b"));
        }


        [TestMethod]
        public void Path()
        {
            var post = RequestBuilder.Post("/{a}");

            Assert.IsTrue(post.Path("a", "b").Build().RequestUri.OriginalString.Contains("/b"));
            Assert.IsFalse(post.Build().RequestUri.OriginalString.Contains("/b"));
        }

        [TestMethod]
        public async Task Body()
        {
            var post = RequestBuilder.Post("/a");
            var content = post.Body("body").Build().Content;
            if (!(content is StringContent @string))
            {
                throw new Exception($"content is {content?.GetType()?.Name}");
            }
            var stringValue = await @string.ReadAsStringAsync();
            Assert.AreEqual("\"body\"", stringValue);
            Assert.IsNull(post.Build().Content);
        }

        [TestMethod]
        public async Task Form()
        {
            var post = RequestBuilder.Post("/a");
            var content = post.Form("a", "b").Form("a", "b").Build().Content;
            if (!(content is FormContent @form))
            {
                throw new Exception($"content is {content?.GetType()?.Name}");
            }
            var stringValue = await @form.ReadAsStringAsync();
            Assert.AreEqual("a=b&a=b", stringValue);
            Assert.IsNull(post.Build().Content);
        }

        [TestMethod]
        public void Header()
        {
            var post = RequestBuilder.Post("/a");
            var request = post.Header("a", "b").Build();
            if (!request.Headers.TryGetValues("a", out var items))
            {
                throw new Exception("a not found");
            }
            Assert.AreEqual("b", items.FirstOrDefault());
            Assert.IsFalse(post.Build().Headers.Contains("a"));
        }
    }
}
