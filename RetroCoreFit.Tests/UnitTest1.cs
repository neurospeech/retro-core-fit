using System;
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

            var api = ib.Build<IApi>();

            Assert.Null(api.Authorize);

            api.Authorize = "a";

            Assert.Equal("a", api.Authorize);

            await api.UpdateAsync(new Product { });
            

        }

        public interface IApi
        {

            [Header("Authorize")]
            string Authorize { get; set; }

            [Put("products/{id}/edit")]
            Task<Product> UpdateAsync([Body] Product product, [Query] bool email = false);

        }

        public class Product {
            public string Name { get; set; }
        }
    }
}
