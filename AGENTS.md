# AGENTS.md

## Project

POIneer.Server is the backend API for the POIneer ecosystem. It exposes health
information and region metadata so clients can discover available offline POI
regions. Future endpoints will provide dataset metadata and download information.

The API uses .NET, ASP.NET Core Minimal APIs, and a lightweight hexagonal
architecture.

## Language and Style

- Write code comments and repository documentation in English.
- Keep code simple, explicit, and maintainable.
- Follow the conventions in the surrounding code and reuse existing components
  where they fit.
- Use records and value objects where they make a concept clearer.
- Introduce abstractions when they solve a concrete problem.

## Architecture

- **API** defines Minimal API endpoints and OpenAPI metadata. Endpoints handle
  HTTP binding and validation, call application use cases, and map results to
  HTTP responses.
- **Application** contains use cases and orchestration. It depends on Domain and
  defines the ports its use cases need.
- **Domain** contains core domain concepts, entities, and value objects. It
  depends on no other project layer.
- **Infrastructure** implements application ports for external dependencies.
- **Contracts** contains public request and response DTOs. The API maps between
  these DTOs and application inputs and results.

Keep business rules in Application or Domain. Wire application ports to
infrastructure implementations in the API composition root.

## API Design

- Use lowercase, plural resource names for collection routes, such as
  `GET /api/regions`. Use path parameters to identify individual resources.
- Use HTTP methods to express operations. Use query parameters for filtering,
  search, sorting, and pagination when a collection endpoint supports them.
- Document supported query parameters, allowed values, defaults, and limits in
  OpenAPI. Return `400 Bad Request` for invalid values of supported parameters.
- Return `200 OK` with an empty collection when a valid query has no matches.
  Return `404 Not Found` when a requested individual resource does not exist.
- Use Problem Details for API errors.
- Define explicit request and response DTOs that expose only the information
  clients need. Follow the existing convention for explicit JSON property names
  in public contracts.
- Preserve existing public routes and response contracts within a published
  major API version. Introduce a new major version for breaking changes and
  document the migration path.

## Testing

Use xUnit. Add or update tests for new behavior and bug fixes at the narrowest
appropriate level.

| Test project | Focus |
| --- | --- |
| `tests/POIneer.Server.UnitTests` | Domain rules and application behavior |
| `tests/POIneer.Server.IntegrationTests` | HTTP endpoint behavior and infrastructure integration |
| `tests/POIneer.Server.ContractTests` | Public API response compatibility, including JSON shape and property names |

Cover changes to public response shapes with contract tests. Run the relevant
tests after making a change and run the full suite when a change affects
multiple layers or shared behavior.

## Commands

Run these commands from the repository root.

**Restore**

```sh
dotnet restore POIneer.Server.slnx
```

**Build**

```sh
dotnet build POIneer.Server.slnx
```

**Test**

```sh
dotnet test POIneer.Server.slnx
```

**Run the API**

```sh
dotnet run --project src/POIneer.Server
```

## Dependencies

Use Central Package Management. Define package versions in
`Directory.Packages.props` and omit versions from `PackageReference` entries in
project files.

## Branching

- `main` is the stable branch.
- `develop` is the integration branch for active development.
- Base feature branches on `develop` and target `develop` when opening feature
  pull requests.

## Current Priorities

- Keep the API small and focused on offline dataset discovery and delivery.
- Maintain the health and region metadata endpoints and their OpenAPI documentation.
- Extend the API with dataset metadata and download information as client needs
  become concrete.