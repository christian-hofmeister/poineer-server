using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using POIneer.Server.Services;

namespace POIneer.Server.Tests
{
    public class OsmProcessingServiceInMemoryTests
    {
        [Fact]
        public async Task GenerateSQLiteSchema_InMemory_CreatesPoiTable()
        {
            // Arrange
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var service = new OsmProcessingService();

            // Act
            await service.CreateSchemaAsync(connection);

            // Assert
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='poi';";

            var result = await cmd.ExecuteScalarAsync();
            Assert.Equal("poi", result);
        }
    }
}
