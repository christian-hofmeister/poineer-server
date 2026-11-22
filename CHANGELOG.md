# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Historical entries for 0.1.0 and 0.2.0 were reconstructed from their Git tags
and source history. Release dates follow the annotated tag dates.

## [0.3.0] - 2026-11-22

### Added

- Dataset discovery at `GET /api/v1/datasets`, with `/api/datasets` retained as
  a compatibility alias for the same catalog response.
- Catalog sources for local files and private Azure Blob Storage, with managed
  identity support and validation of region snapshots and producer manifests.
- Catalog metadata for SQLite and optional PMTiles artifacts, including versions,
  sizes, SHA-256 checksums and reserved download URLs.
- Health endpoint at `GET /api/health` reporting the running server's build version.
- Docker image and Bicep infrastructure for the Azure development environment,
  with deployment and smoke-test documentation.
- Unit, integration and contract test projects, producer manifest fixtures and
  schema validation, plus CI coverage reporting.
- Tag-based Jenkins release preparation: validate `vMAJOR.MINOR.PATCH` tags,
  require the tagged commit to be reachable from `main`, run builds and tests,
  and build a versioned release container.

### Changed

- Rebuilt the backend on .NET 10 and ASP.NET Core Minimal APIs using a lightweight
  hexagonal architecture and Central Package Management.
- Consolidated OpenAPI generation at `/openapi/v1.json`; development Swagger UI
  and the `/swagger/v1/swagger.json` compatibility route use the same generator.
- Derive release build and health versions from the Git tag, with `0.3.0-dev`
  as the default for local and branch builds.
- Replaced release-branch deployment selection with tag-based selection in
  Jenkins while preserving branch and pull request builds.
- Replaced the legacy `GET /health` string response with the structured
  `GET /api/health` response. Monitoring clients must update the path and parse
  the JSON object instead of a string.
- Updated runtime, Azure SDK, OpenAPI and test dependencies.

### Removed

- Legacy POI processing, database migration tooling and controller-based region
  endpoints, including `GET /api/regions` and `GET /api/regions/{region}/sqlite`.
  Clients should use `GET /api/v1/datasets` for discovery. Artifact download
  transport is not implemented in this release; advertised download URLs are
  reserved for follow-up work.

### Release limitations

- Jenkins builds the release container locally but does not yet push it to a
  registry or deploy it. Development and release deployment stages remain
  placeholders; the documented manual Azure development workflow is available.
- Invalid individual manifests are omitted from the catalog. Unavailable or
  invalid catalog sources return `503` Problem Details; a valid source with no
  published releases returns `200` with an empty array.

## [0.2.0] - 2025-08-07

### Added in 0.2.0

- `GET /health` endpoint returning the JSON string `"API is running"`.
- In-memory HTTP integration tests using `WebApplicationFactory` and a dedicated
  test factory, plus additional SQLite service tests.
- Flyway configuration and an initial SQL migration for the SQLite POI schema.
- Dependabot configuration for NuGet updates and a local .NET tool manifest
  including `dotnet-outdated-tool`.
- Project documentation covering development setup and commit conventions.

### Changed in 0.2.0

- Moved the server into `src/` and tests into `tests/`, renamed the test project,
  and updated solution and pipeline paths.
- Disabled HTTPS redirection in the `Testing` environment to support in-memory
  API tests.
- Extended Jenkins with workspace cleanup, local tool restoration and outdated
  package checks, retaining placeholder deployment steps.

## [0.1.0] - 2025-08-02

### Added in 0.1.0

- Initial ASP.NET Core controller-based API on .NET 9.
- `GET /api/regions` for region metadata from a local JSON file and
  `GET /api/regions/{region}/sqlite` for serving existing SQLite files.
- SQLite POI model and a service for generating a database with sample POIs;
  generation was not enabled automatically at application startup.
- Unit tests for SQLite database generation.
- Swagger/OpenAPI documentation and an interactive UI in Development and Staging.
- Jenkins pipeline for restore, build and tests, with informational branch-based
  placeholders for future deployments.

[0.3.0]: https://github.com/christian-hofmeister/poineer-server/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/christian-hofmeister/poineer-server/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/christian-hofmeister/poineer-server/tree/v0.1.0
