# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - Unreleased

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

## [0.1.0] - 2025-08-02

### Added in 0.1.0

- Initial version of POIneer.Server based on .NET 9
- SQLite integration for offline POI storage
- Dummy data generator for POIs (GenerateDummySQLite)
- REST API for querying POIs
- Swagger (OpenAPI) integration for interactive API documentation
- Project structure with modular service layer
- First unit tests for SQLite access
- Jenkins Multibranch Pipeline integration
- GitHub SSH deployment setup for Jenkins
- Webhook-based triggering from GitHub to Jenkins
- Development and release branching strategy established

### Planned at 0.1.0

- Integration tests for REST API endpoints
- Admin dashboard (React) for managing OSM regions
- Automatic download and processing of Geofabrik updates
- Jenkins deployment to staging and production environments
- Offline map tile support for the Android app
- Mobile client integration with region-based POI sync
