using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace POIneer.Server.Tests
{

    public class HealthEndpointTests : IClassFixture<TestApiFactory>
    {
        private readonly HttpClient _client;

        public HealthEndpointTests(TestApiFactory factory)
        {
            // Create a client from the custom factory
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HealthEndpoint_Returns_OK()
        {
            var response = await _client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("API is running", content);
        }
    }
}
