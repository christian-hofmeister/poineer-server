# Dataset catalog

`GET /api/datasets` returns an array of currently published releases containing
SQLite and, when available, PMTiles artifacts.
The existing `/api/regions` and dataset metadata DTO remain unchanged. The
legacy hardcoded region endpoint is not an availability catalog.

The catalog joins current manifests to `regions.json` by exact, case-sensitive
region ID. Display records alone never make a dataset available. Invalid or
unsupported manifests are logged and omitted as whole releases, including when
an optional PMTiles artifact is invalid. Missing display metadata is logged and
the release omitted. Missing, malformed or duplicate region snapshots and read
failures return HTTP 503 Problem Details, not an empty catalog. A successfully
read source with no valid current releases returns `[]`.

## Local development and configuration

Development uses `DevelopmentFixtures/catalog`, containing the unchanged
synthetic Berlin SQLite fixture from the renderer contract. Its size, checksum
and version are illustrative; it is not a real published download. Tests use
the pinned producer fixtures under `tests/Fixtures`; see their provenance file.
Production has no default metadata root and returns 503 until configured.

Set `Catalog:MetadataRoot` (environment variable `Catalog__MetadataRoot`) to a
local metadata directory, absolute or relative to the API content root:

```text
regions.json
geofabrik/europe/germany/berlin/manifest.json
```

`regions.json` is the producer's PascalCase array containing `Id`, `Name`,
optional `Country` and `Category`; source fields such as `PbfUrl` are ignored.
Discovery recursively selects current `manifest.json` files under `geofabrik/`.
Artifact filenames, release version ordering and modification times never
determine availability. The producer guarantees complete, verified publication;
this reader validates metadata, not remote artifact bytes.

`ICatalogSource` isolates listing, reads and producer validation from the
application catalog mapping. Replace its local adapter for remote storage.
Producer manifest JSON is separate from public response DTOs. Full draft-v1
validation rejects unknown fields and types, unsupported versions, unsafe keys,
bad filenames, invalid dates, nonpositive sizes and malformed SHA-256 values.
`dataset.version` identifies the release. `dataset.artifacts` lists every
available artifact with an explicit `type` (`sqlite` or `pmtiles`), its own
`artifactVersion`, `sizeBytes`, `sha256Checksum` and `downloadUrl`. A SQLite-only
release has one entry; a release with tiles has both. Clients select by type,
not array position. Release and artifact versions can differ, as can the versions
of SQLite and PMTiles within one release.

## Geography and map interactions

The producer v1 contract does **not** supply bounds or parent relationships.
The server must not invent them or derive them from hierarchical IDs. The
public `bounds` field is nullable; `IRegionBoundsSource` is the enrichment port.
An operator can supply verified bounds separately in server configuration:

```json
{
  "Catalog": {
    "RegionBounds": [
      {
        "RegionId": "example/region",
        "MinLat": 10,
        "MinLon": 20,
        "MaxLat": 11,
        "MaxLon": 21
      }
    ]
  }
}
```

These coordinates are syntax examples, not Berlin data. Entries require all
coordinates, valid ranges, positive area and unique IDs. Antimeridian-wrapping
boxes are not supported by this initial source. Verified Berlin bounds and an
agreed producer geography contract are still needed for real viewport matching.

For ordinary non-wrapping boxes, a viewport intersects a region when their
latitude and longitude intervals overlap (including boundaries). Split a
viewport crossing the antimeridian into two boxes before matching. A bounding
box is a coarse candidate filter, not an administrative polygon containment test.

On pan/zoom, match entries with bounds locally. Entries with null bounds remain
discoverable by ID or list selection, but cannot participate in viewport
matching. On selecting a known region ID, absence from a successfully fetched
catalog means no dataset is currently offered for that ID. No matching box means
no known coverage, not proof of unavailable coverage when bounds are missing.
On HTTP 503, show availability as temporarily unknown and allow retry.
For each installed artifact, select the matching `type` in `dataset.artifacts`
and compare its version/checksum to detect updates independently. Release
version alone can change when a different artifact changes.

## Download boundary

Each `dataset.artifacts[].downloadUrl` reserves
`/api/datasets/{hierarchical-region-id}/latest/{type}`, where `type` is `sqlite`
or `pmtiles`.
The download handler, redirects, caching and URL renewal belong to renderer
issue #203 and are not implemented here; these URLs currently return 404.
Clients must follow API-provided URLs once transport is enabled and must not
construct object keys or bucket paths. No storage credentials or object keys
are exposed by this catalog.

Metadata discovery uses the metadata root only (planned Azure Blob Storage).
Artifact references resolve from a separately configured artifact root (planned
Hetzner), never the manifest directory or metadata root. Both roots may later
point to one local store. Remote root resolution and byte transport remain in
#203; no artifact-root setting is consumed by this local catalog adapter.

## Lightweight demand tracking proposal

No tracking occurs automatically on map movement. A future explicit "Request
offline availability" action can submit a stable region ID to a dedicated
endpoint. Aggregate counts by region and calendar month; never persist precise
coordinates, device/account identifiers, IP addresses or movement history in
the demand store. Do not put request bodies or coordinates in access logs.
Retain monthly aggregates for a defined short period (for example 90 days).
Rate-limit requests and document that anonymous counters measure submissions,
not unique people. If region IDs are unavailable, agree on coarse fixed cells
before accepting map-area requests. Storage, abuse protection and a write
endpoint are deferred; this issue prepares the approach through documentation.

## References

- [Server catalog issue #54](https://github.com/christian-hofmeister/poineer-server/issues/54)
- [Producer manifest contract #204](https://github.com/christian-hofmeister/poineer-render/issues/204)
- [Separate storage roots #207](https://github.com/christian-hofmeister/poineer-render/issues/207)
- [Transport integration #203](https://github.com/christian-hofmeister/poineer-render/issues/203)
