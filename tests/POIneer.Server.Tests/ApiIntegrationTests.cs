using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace POIneer.Server.Tests
{
    // This test spins up the API in memory and sends real HTTP requests to it
    public class ApiIntegrationTests : IClassFixture<TestApiFactory>
    {
        private readonly TestApiFactory _factory;

        public ApiIntegrationTests(TestApiFactory factory)
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
        }
    }
}
