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

Planned:

- Region endpoints
- Dataset metadata
- Dataset downloads
- API versioning
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

Swagger UI:

```text
/swagger
```

---

## Health Endpoint

```text
GET /health
```

Example response:

```json
{
  "status": "Healthy",
  "service": "POIneer.Server",
  "timestampUtc": "2026-05-17T09:08:56.2548963+00:00",
  "version": "0.1.0"
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