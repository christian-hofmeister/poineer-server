# POIneer.Server

Backend API for the POIneer ecosystem.

POIneer.Server provides metadata, downloads and future online services for the POIneer offline POI platform.

The project is built with modern .NET and follows a lightweight clean/hexagonal architecture approach focused on maintainability, simplicity and performance.

---

## Goals

- Provide region metadata for POI datasets
- Provide downloadable offline SQLite datasets
- Offer a lightweight and privacy-friendly API
- Keep the architecture clean and easy to evolve
- Support future mobile clients and synchronization features

---

## Tech Stack

- .NET 10
- ASP.NET Core Minimal APIs
- OpenAPI / Swagger
- xUnit
- Central Package Management
- Clean / Hexagonal Light Architecture

---

## Solution Structure

```text
src/
  POIneer.Server/

tests/
  POIneer.Server.UnitTests/
  POIneer.Server.IntegrationTests/
  POIneer.Server.ContractTests/
```

---

## Architecture

The project follows a lightweight layered architecture:

```text
Api -> Application -> Domain
          ↑
   Infrastructure
```

### Layers

| Layer | Responsibility |
|---|---|
| Api | HTTP endpoints and transport layer |
| Application | Use cases and orchestration |
| Domain | Core business concepts |
| Infrastructure | External systems and implementations |
| Contracts | Request/response DTOs |

---

## Features

Current:

- Health endpoint
- OpenAPI support
- Swagger UI
- Test project setup
- Centralized package management
- Versioned dataset catalog at `GET /api/v1/datasets` with validated manifests from local files or Azure Blob Storage; `/api/datasets` remains a compatibility alias

See [Dataset catalog](docs/dataset-catalog.md) for configuration, fixture limitations,
map availability, geographic enrichment and the demand-tracking proposal.
See [Azure development deployment](docs/azure-dev.md) for the Container Apps setup and Berlin smoke test.
See [Tag-based releases](docs/releases.md) for Jenkins configuration and versioning.

Planned:

- Dataset metadata
- Dataset downloads
- Rate limiting
- VPS deployment
- Scheduled dataset updates

---

## Development

### Restore

```bash
dotnet restore
```

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project src/POIneer.Server
```

### Test

```bash
dotnet test
```

---

## OpenAPI / Swagger

OpenAPI endpoint:

```text
/openapi/v1.json
```

ASP.NET Core generates this document in all environments. Swagger UI is available
only in Development and loads the same document:

```text
/swagger
```

The former development document route `/swagger/v1/swagger.json` remains an alias
served by the ASP.NET Core generator. The standalone
`Swashbuckle.AspNetCore.SwaggerUI` package provides only the viewer; SwaggerGen and
the umbrella `Swashbuckle.AspNetCore` package are not required. This avoids their
Microsoft.OpenApi dependency conflict with ASP.NET Core 10. Package versions
remain managed in `Directory.Packages.props`.

---

## Health Endpoint

```text
GET /api/health
```

The `version` field reports the running server's build version. API contract
versions such as `/api/v1` are independent of the server version; the health
endpoint does not use an API version segment.

Example response:

```json
{
  "status": "Healthy",
  "service": "POIneer.Server",
  "timestampUtc": "2026-05-17T09:08:56.2548963+00:00",
  "version": "0.3.0-dev"
}
```

---

## Project Status

Early MVP phase.

The current focus is building a stable and maintainable foundation for future offline map and POI services.

---

## Related Projects

- POIneer.Render — OSM → SQLite rendering pipeline
- Future mobile application (planned)

---

## License

MIT
