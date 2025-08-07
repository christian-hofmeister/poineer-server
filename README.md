# POIneer.Server

**POIneer** is a backend service that extracts, processes, and provides Points of Interest (POIs) from OpenStreetMap data for mobile clients (e.g., Android apps).

## 🔥 Purpose

Offline-capable mobile application with region-based POI usage, supported by a modern .NET WebAPI.

---

## 🚀 Features

- .NET 9 WebAPI for serving POI data
- SQLite as persistent database (compact and offline-friendly)
- REST API with Swagger documentation
- Support for multiple regions (based on OSM data from providers like Geofabrik)
- Dummy data available for local development
- Planned Jenkins CI/CD process with automated deployment
- Planned: Admin UI for region management (React or Angular)

---

## 🗂 Project Structure

```plaintext
POIneer.Server/
│
├── Controllers/           # WebAPI endpoints
├── Services/              # Business logic and data access
├── Models/                # Data models (POI, etc.)
├── Data/                  # SQLite database and initialization
├── Tests/                 # Unit and integration tests
├── Jenkinsfile            # Jenkins pipeline definition
├── Program.cs             # Entry point
├── appsettings.json       # Configuration
└── README.md              # This document
```

---

## 🧪 Development & Testing

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/)
- Git
- SQLite
- Visual Studio Code (optional)
- Jenkins (for CI/CD)

### Run the application

```bash
dotnet run
```

The API will be available at `https://localhost:5001/swagger/index.html`.

### Run tests

```bash
dotnet test
```

---

## 🌍 Data Source

POIs are extracted from OpenStreetMap data. For the initial import, we use **.osm.pbf** files from [geofabrik.de](https://download.geofabrik.de/).

---

## 🛡️ Security

Planned:

- Authentication (only authorized clients may access certain endpoints)
- Restrict API access to mobile clients

---

## 📦 Deployment

Deployment is automated via Jenkins on a VPS:

- `develop`: Build + Test
- `release/*`: Build + Test + Deploy to Staging
- `main`: Build + Test + Deploy to Production

---

## 📄 License

*To be decided* – likely MIT or Apache 2.0

---

## 🧠 Roadmap

- [x] SQLite-based POI database with dummy data
- [x] REST API using .NET 9
- [x] Swagger UI
- [ ] Geofabrik importer for real OSM data
- [ ] Admin UI (Web interface)
- [ ] Integration tests
- [ ] Regional updates (Geofabrik diffs)
- [ ] Authentication & API keys
