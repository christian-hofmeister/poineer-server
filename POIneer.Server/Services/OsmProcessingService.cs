using Microsoft.Data.Sqlite;
using POIneer.Server.Models;

namespace POIneer.Server.Services;

public class OsmProcessingService
{
    public void GenerateDummySQLite(string sqlitePath)
    {
        if (File.Exists(sqlitePath))
            File.Delete(sqlitePath);

        using var conn = new SqliteConnection($"Data Source={sqlitePath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS poi (
                id TEXT PRIMARY KEY,
                name TEXT,
                lat REAL,
                lon REAL,
                type TEXT,
                subtype TEXT,
                tags TEXT
            );";
        cmd.ExecuteNonQuery();

        var pois = new List<Poi>
        {
            new Poi { Id = "1", Name = "Public Pool", Lat = 52.51, Lon = 13.38, Type = "amenity", Subtype = "swimming_pool" },
            new Poi { Id = "2", Name = "Club Berlin", Lat = 52.52, Lon = 13.4, Type = "amenity", Subtype = "nightclub" }
        };

        foreach (var poi in pois)
        {
            var insert = conn.CreateCommand();
            insert.CommandText = @"
                INSERT INTO poi (id, name, lat, lon, type, subtype, tags)
                VALUES ($id, $name, $lat, $lon, $type, $subtype, $tags);";
            insert.Parameters.AddWithValue("$id", poi.Id);
            insert.Parameters.AddWithValue("$name", poi.Name);
            insert.Parameters.AddWithValue("$lat", poi.Lat);
            insert.Parameters.AddWithValue("$lon", poi.Lon);
            insert.Parameters.AddWithValue("$type", poi.Type);
            insert.Parameters.AddWithValue("$subtype", poi.Subtype);
            insert.Parameters.AddWithValue("$tags", poi.Tags);
            insert.ExecuteNonQuery();
        }
    }
}
