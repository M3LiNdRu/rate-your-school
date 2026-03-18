# RateYourSchool

A modern web application for rating and reviewing schools, enabling users to make informed educational decisions through community-driven feedback and comprehensive school information.

## Overview

RateYourSchool provides a platform where users can browse schools, view aggregate ratings, and read detailed reviews from other users. The application combines school information with user-generated content to create a comprehensive resource for evaluating educational institutions.

### Key Features

- **School Discovery**: Browse and search schools with pagination and filtering
- **School Ratings**: View aggregate scores calculated from user reviews
- **User Reviews**: Read and submit anonymous reviews with detailed feedback
- **Map View**: Explore schools geographically with lightweight map projections
- **Real-time Updates**: CQRS-based architecture with eventual consistency for optimal performance

## Technology Stack

### Backend
- **.NET 8.0+** - ASP.NET Core with Minimal APIs
- **MongoDB** - NoSQL database with Change Streams
- **OpenTelemetry** - Distributed tracing and observability
- **xUnit** - Unit testing framework
- **ReqnRoll/SpecFlow** - BDD-style integration testing with Gherkin

### Frontend
- **React 18+** - Modern UI framework
- **TypeScript** - Type-safe JavaScript
- **Vite** - Fast build tooling
- **Axios** - HTTP client

### Infrastructure
- **Terraform** - Infrastructure as Code
- **Docker** - Containerization
- **OpenAPI 3.x** - API documentation

## Architecture

The application follows **CQRS (Command Query Responsibility Segregation)** and **Vertical Slice Architecture** patterns:

- **Write Models**: `School` and `UserReview` collections store master data
- **Read Models**: `SchoolRate` and `SchoolMapView` provide optimized query projections
- **Change Streams**: MongoDB Change Streams synchronize write models to read models
- **Vertical Slices**: Each feature is self-contained with all layers (API, business logic, data access)

See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architectural documentation.

## Project Structure

```
RateYourSchool/
├── docs/                      # Documentation
│   ├── ARCHITECTURE.md        # Architecture decisions and patterns
│   ├── BUSINESS_RULES.md      # Business logic reference
│   └── use-cases/             # Feature use cases
├── src/
│   ├── backend/
│   │   ├── Api/               # ASP.NET Core API with Minimal APIs
│   │   │   └── Endpoints/     # Vertical slice features
│   │   ├── Domain/            # Core entities and repository interfaces
│   │   └── Data/              # Data access implementations
│   └── frontend/              # React TypeScript application
└── tests/                     # Unit and integration tests
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- MongoDB 4.4+
- Node.js 18+ (for frontend)
- Docker (optional, for containerized deployment)

### Running the Backend

```bash
cd src/backend/Api
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` (or as configured in `launchSettings.json`).

### Running Tests

```bash
# Unit tests
dotnet test tests/RateYourSchool.Tests.Unit/

# Integration tests
dotnet test tests/RateYourSchool.Tests.Integration/
```

## Documentation

- **[Architecture](docs/ARCHITECTURE.md)** - System design and patterns
- **[Business Rules](docs/BUSINESS_RULES.md)** - Application business logic reference
- **[Use Cases](docs/use-cases/)** - Feature specifications and workflows
- **[API Documentation](src/backend/Api/openapi/)** - OpenAPI specifications

## Business Rules

The application implements comprehensive business rules covering:
- School management (BR-001 to BR-008)
- SchoolRate read model synchronization (BR-009 to BR-015)
- Scoring system algorithms (BR-016 to BR-018)
- API pagination and filtering (BR-024 to BR-035)
- User review management (BR-036 to BR-040)

See [BUSINESS_RULES.md](docs/BUSINESS_RULES.md) for complete details.

## Contributing

This project follows strict coding standards:
- **C#**: PascalCase for types/methods, \_camelCase for private fields
- **TypeScript**: PascalCase for components/types, camelCase for variables
- **Testing**: Minimum 80% code coverage for business logic
- **Architecture**: Vertical slices with minimal cross-feature dependencies

Refer to [.github/copilot-instructions.md](.github/copilot-instructions.md) for detailed coding guidelines.

## License

See [COPYING](COPYING) for license information.

---

**Status**: Active Development  
**Last Updated**: March 2026
