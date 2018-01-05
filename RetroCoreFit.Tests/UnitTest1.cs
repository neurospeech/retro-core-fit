using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace RetroCoreFit.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1Async()
        {

            InterfaceBuilder ib = new InterfaceBuilder();

            var api = ib.Build<IApi,TestBaseService>();

            Assert.Null(api.Authorize);

            api.Authorize = "a";

            Assert.Equal("a", api.Authorize);

            // await api.UpdateAsync(1,new Product { });
            

        }

        public class TestBaseService : BaseService {

            protected override Task<T> InvokeAsync<T>(HttpMethod method, string path, IEnumerable<RestParameter> plist)
            {
                return base.InvokeAsync<T>(method, path, plist);
            }

        }

        public interface IApi
        {

            [Header("Authorize")]
            string Authorize { get; set; }

            [Put("products/{id}/edit")]
            Task<Product> UpdateAsync(
                [Path("id")] long productId,
                [Body] Product product, 
                [Query] bool email = false);

        }

        public class Product {
            public string Name { get; set; }
        }
    }
}
