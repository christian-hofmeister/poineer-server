# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-08-02
### Added
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

### Planned
- Integration tests for REST API endpoints
- Admin dashboard (React) for managing OSM regions
- Automatic download and processing of Geofabrik updates
- Jenkins deployment to staging and production environments
- Offline map tile support for the Android app
- Mobile client integration with region-based POI sync

