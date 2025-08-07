using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace POIneer.Server.Tests
{
    /// <summary>
    /// Custom WebApplicationFactory that disables HTTPS redirection for integration tests.
    /// </summary>
    public class TestApiFactory : WebApplicationFactory<POIneer.Server.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Override the application's configuration for the test environment
            builder.ConfigureServices(services =>
            {
                // You can configure test-specific services here if needed
            });


            builder.UseEnvironment("Testing");

        }
    }
}
