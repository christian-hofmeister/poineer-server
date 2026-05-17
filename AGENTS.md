# AGENTS.md

## Project

POIneer.Server is the backend API for the POIneer ecosystem.

It provides metadata, health/status information and future download-related endpoints for offline POI datasets.

The project uses modern .NET, Minimal APIs and a lightweight hexagonal architecture approach.

## Language and Style

- Code comments must be written in English.
- Documentation files should be written in English.
- Keep code simple, explicit and maintainable.
- Prefer clarity over clever abstractions.
- Avoid unnecessary enterprise-style boilerplate.

## Architecture

Use a lightweight hexagonal architecture:

    Api -> Application -> Domain
              ↑
       Infrastructure

### Layers

- Api: Minimal API endpoints, HTTP mapping, OpenAPI metadata.
- Application: Use cases, orchestration, ports/abstractions.
- Domain: Core domain concepts, entities, value objects.
- Infrastructure: Implementations for external dependencies.
- Contracts: Public request/response DTOs.

## Rules

- Do not put business logic into endpoints.
- Endpoints should only map HTTP requests/responses and call application logic.
- Domain must not depend on infrastructure or API.
- Prefer small records/value objects where they improve meaning.
- Avoid premature abstractions.
- Add tests for new behavior.
- Keep public API contracts stable and explicit.

## Testing

Use xUnit.

Test projects:

    tests/POIneer.Server.UnitTests
    tests/POIneer.Server.IntegrationTests
    tests/POIneer.Server.ContractTests

Guidelines:

- Unit tests should focus on domain/application behavior.
- Integration tests should verify HTTP endpoints and infrastructure integration.
- Contract tests should be used for public API response compatibility when needed.

## Commands

Restore:

    dotnet restore POIneer.Server.slnx

Build:

    dotnet build POIneer.Server.slnx

Test:

    dotnet test POIneer.Server.slnx

Run API:

    dotnet run --project src/POIneer.Server

## Dependencies

This repository uses Central Package Management.

Package versions belong in:

    Directory.Packages.props

Project files should reference packages without explicit versions.

## Branching

- main is the stable branch.
- develop is used for active development.
- Feature work should be done on feature branches and merged via pull requests.

## Current Priorities

- Keep the API small and clean.
- Provide useful OpenAPI documentation.
- Add health and region metadata endpoints.
- Prepare the server for future dataset metadata and download endpoints.