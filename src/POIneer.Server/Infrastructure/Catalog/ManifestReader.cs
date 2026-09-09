using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Catalog;

public static class ManifestReader
{
    private const string VersionPattern = @"\A[1-9][0-9]*-[a-f0-9]{16}\z";
    private const string PathPattern = @"\A[a-z0-9]+(?:[._-][a-z0-9]+)*(?:/[a-z0-9]+(?:[._-][a-z0-9]+)*)*\z";

    public static bool IsRegionId(string value) => Matches(value, PathPattern);

    public static PublishedDataset Read(string key, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        RequireFields(root, "schemaVersion", "regionId", "releaseVersion", "publishedAt", "artifacts");
        if (root.GetProperty("schemaVersion").ValueKind != JsonValueKind.Number
            || !root.GetProperty("schemaVersion").TryGetInt32(out var schema) || schema != 1)
            throw new JsonException("Unsupported schemaVersion; expected 1.");
        var region = ReadString(root, "regionId", PathPattern);
        if (key != $"{region}/manifest.json")
            throw new JsonException("regionId does not match the manifest key.");
        var release = ReadString(root, "releaseVersion", VersionPattern);
        var published = ReadString(root, "publishedAt", @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]+)?Z\z");
        try { _ = XmlConvert.ToDateTimeOffset(published); }
        catch (FormatException exception) { throw new JsonException("Invalid publishedAt calendar date.", exception); }
        var artifacts = root.GetProperty("artifacts");
        if (artifacts.ValueKind != JsonValueKind.Array || artifacts.GetArrayLength() is < 1 or > 2)
            throw new JsonException("artifacts must contain SQLite and optionally PMTiles.");
        var types = new HashSet<string>(StringComparer.Ordinal);
        var publishedArtifacts = new List<PublishedArtifact>();
        foreach (var artifact in artifacts.EnumerateArray())
        {
            RequireFields(artifact, "type", "artifactVersion", "objectKey", "sizeBytes", "sha256");
            var type = ReadString(artifact, "type", @"\A(?:sqlite|pmtiles)\z");
            if (!types.Add(type)) throw new JsonException("Duplicate artifact type.");
            var version = ReadString(artifact, "artifactVersion", VersionPattern);
            var objectKey = ReadString(artifact, "objectKey", PathPattern);
            if (objectKey.Split('/')[^1] != $"{region.Split('/')[^1]}.{version}.{type}")
                throw new JsonException("objectKey filename does not match region, artifactVersion and type.");
            var sizeElement = artifact.GetProperty("sizeBytes");
            if (sizeElement.ValueKind != JsonValueKind.Number || !sizeElement.TryGetInt64(out var size) || size <= 0)
                throw new JsonException("sizeBytes must be a positive integer.");
            var checksum = ReadString(artifact, "sha256", @"\A[0-9a-f]{64}\z");
            publishedArtifacts.Add(new PublishedArtifact(type, version, size, checksum));
        }
        if (!types.Contains("sqlite")) throw new JsonException("Missing SQLite artifact.");
        return new PublishedDataset(region, release, publishedArtifacts);
    }

    private static string ReadString(JsonElement element, string field, string pattern)
    {
        var value = element.GetProperty(field);
        if (value.ValueKind != JsonValueKind.String || !Matches(value.GetString()!, pattern))
            throw new JsonException($"Invalid {field}.");
        return value.GetString()!;
    }

    private static bool Matches(string value, string pattern) =>
        Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static void RequireFields(JsonElement element, params string[] fields)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new JsonException("Expected an object.");
        var names = element.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Length != fields.Length || !names.ToHashSet(StringComparer.Ordinal).SetEquals(fields))
            throw new JsonException($"Expected exactly these fields: {string.Join(", ", fields)}.");
    }
}
