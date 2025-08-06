using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace POIneer.Server.Tests
{

    public class HealthEndpointTests : IClassFixture<TestApiFactory>
    {
        private readonly TestApiFactory _factory;

        public HealthEndpointTests(TestApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthEndpoint_Returns_OK()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("API is running", content);
        }
    }
}
