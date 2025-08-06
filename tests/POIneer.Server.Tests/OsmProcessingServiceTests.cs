using System.IO;
using Xunit;
using POIneer.Server.Services;
using Microsoft.Data.Sqlite;

namespace POIneer.Server.Tests;

public class OsmProcessingServiceTests
{
    [Fact]
    public void GenerateDummySQLite_CreatesDatabaseWithPoiTable()
    {
        // Arrange
        var sqlitePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.sqlite");
        var service = new OsmProcessingService();

        // Act
        service.GenerateDummySQLite(sqlitePath);

        // Assert
        Assert.True(File.Exists(sqlitePath), "SQLite file was not created");

        using var conn = new SqliteConnection($"Data Source={sqlitePath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='poi';";
        var result = cmd.ExecuteScalar();

        Assert.Equal("poi", result);

        cmd.CommandText = "SELECT COUNT(*) FROM poi;";
        var countResult = cmd.ExecuteScalar();
        Assert.NotNull(countResult);
        Assert.IsType<long>(countResult);

        var count = (long)countResult;

        Assert.True(count >= 2, "Expected at least 2 dummy POIs in the table");

        // Clean up
        File.Delete(sqlitePath);
    }
}
