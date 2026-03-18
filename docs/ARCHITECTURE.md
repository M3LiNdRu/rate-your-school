# RateYourSchool - Architecture Design Document

**Version:** 1.0  
**Last Updated:** March 15, 2026  
**Status:** Current Design

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Architecture Patterns](#2-architecture-patterns)
3. [Technology Stack](#3-technology-stack)
4. [API Design](#4-api-design)
5. [Backend Architecture](#5-backend-architecture)
6. [Database Architecture](#6-database-architecture)
7. [Frontend Architecture](#7-frontend-architecture)
8. [Event-Driven Communication](#8-event-driven-communication)
9. [Testing Strategy](#9-testing-strategy)
10. [Observability](#10-observability)
11. [Infrastructure as Code](#11-infrastructure-as-code)

---

## 1. Architecture Overview

### 1.1 System Architecture

RateYourSchool is designed as a modern, scalable web application following industry best practices and contemporary architectural patterns. The system enables users to discover, rate, and review schools while providing powerful search and filtering capabilities.

### 1.2 Key Architectural Principles

- **CQRS (Command Query Responsibility Segregation)**: Separate read and write models for optimized performance
- **Vertical Slice Architecture**: Self-contained feature implementations reducing coupling
- **Event-Driven Design**: Asynchronous communication using MongoDB Change Streams
- **Cloud-Ready**: Infrastructure defined as code for reproducible deployments
- **Observable**: Built-in telemetry and monitoring from the ground up
- **Testable**: Comprehensive test coverage with multiple testing strategies

### 1.3 High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend Layer                           │
│                  React SPA (Single Page App)                    │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTPS/REST
┌────────────────────────────▼────────────────────────────────────┐
│                      API Gateway Layer                          │
│              .NET 10 Minimal APIs (OpenAPI/Arazzo)              │
└─────────────┬──────────────────────────────────┬────────────────┘
              │                                  │
    ┌─────────▼─────────┐            ┌──────────▼──────────┐
    │  Command Side     │            │   Query Side        │
    │  (Write Model)    │            │   (Read Model)      │
    │                   │            │                     │
    │  - School         │            │  - SchoolRate       │
    │  - UserReview     │            │  - Map Projections  │
    └─────────┬─────────┘            └──────────┬──────────┘
              │                                  │
              └──────────────┬───────────────────┘
                             │
                    ┌────────▼─────────┐
                    │   MongoDB        │
                    │  - Write DB      │
                    │  - Read DB       │
                    │  - Change Stream │
                    └──────────────────┘
```

---

## 2. Architecture Patterns

### 2.1 CQRS (Command Query Responsibility Segregation)

**Pattern Description:**
- Separates read operations (Queries) from write operations (Commands)
- Allows independent optimization of read and write workloads
- Enables different data models for reading and writing

**Implementation in RateYourSchool:**

**Write Models (Commands):**
- `School` - Master school data
- `UserReview` - User-generated reviews

**Read Models (Queries):**
- `SchoolRate` - Denormalized view combining school info, scores, and last 25 reviews
- `SchoolMapView` - Lightweight projection for map display
- `ReviewList` - Paginated review history

**Benefits:**
- **Performance**: Read models are optimized for specific query patterns
- **Scalability**: Read and write databases can scale independently
- **Flexibility**: Multiple read models can serve different use cases
- **Eventual Consistency**: Acceptable trade-off for better performance

### 2.2 Vertical Slice Architecture

**Pattern Description:**
- Each feature is implemented as a self-contained vertical slice
- All layers of a feature (API, handler, repository, model) are grouped together
- Minimizes coupling between features

**Implementation in RateYourSchool:**

Each use case is organized as a vertical slice:

```
Endpoints/
├── GetSchools/
│   ├── Endpoint.cs              (API endpoint definition)
│   ├── Handler.cs               (Business logic)
│   ├── ReadonlyRepository.cs    (Data access)
│   ├── Request.cs               (Input model)
│   ├── Response.cs              (Output model)
│   ├── SchoolViewModel.cs       (View model)
│   └── ServiceCollectionExtensions.cs (DI registration)
├── CreateReview/
│   ├── Endpoint.cs
│   ├── Handler.cs
│   ├── Repository.cs
│   ├── Request.cs
│   └── ...
├── GetSchoolsForMap/
│   └── ...
└── GetTopRatedSchools/
    └── ...
```

**Benefits:**
- **Cohesion**: Related code lives together
- **Independence**: Features can evolve independently
- **Onboarding**: Easier for new developers to understand
- **Maintenance**: Changes are localized to feature folders

---

## 3. Technology Stack

### 3.1 Backend Technologies

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Runtime** | .NET | 10 | Primary backend framework |
| **API Framework** | Minimal APIs | .NET 10 | Lightweight HTTP APIs |
| **Language** | C# | Latest | Primary programming language |
| **Database** | MongoDB | Latest | Primary data store |
| **ORM/Driver** | MongoDB.Driver | Latest | Database connectivity |

### 3.2 Frontend Technologies

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | React | Latest | UI framework |
| **Architecture** | SPA | - | Single Page Application |
| **Language** | TypeScript | Latest | Type-safe JavaScript |
| **Build Tool** | Vite/Webpack | Latest | Module bundler |
| **State Management** | TBD | - | React Context/Redux/Zustand |

### 3.3 Infrastructure & DevOps

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **IaC** | Terraform | Infrastructure as Code |
| **Observability** | OpenTelemetry | Distributed tracing & metrics |
| **API Documentation** | OpenAPI 3.x | REST API specification |
| **Workflow Spec** | Arazzo | API workflow descriptions |

### 3.4 Testing Technologies

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Unit Testing** | xUnit | Unit test framework |
| **Integration Testing** | ReqnRoll/SpecFlow | BDD framework |
| **Test Language** | Gherkin | Human-readable test scenarios |
| **Test Host** | WebApplicationFactory | Integration test hosting |

---

## 4. API Design

### 4.1 REST API Principles

**Architecture Style:** RESTful HTTP APIs  
**Implementation:** .NET 10 Minimal APIs  
**Specification:** OpenAPI 3.x + Arazzo

**Design Principles:**
- Resource-based URLs
- Standard HTTP methods (GET, POST, PUT, DELETE)
- Proper HTTP status codes
- JSON request/response bodies
- Pagination for list endpoints
- Filtering and sorting capabilities

### 4.2 API Endpoint Structure

**Base URL:** `https://api.rateyourschool.com/api/v1`

**Endpoint Categories:**

```
/api/v1/schools                    # School management
  GET     /                        # List schools (paginated)
  GET     /{id}                    # Get school details
  POST    /                        # Create school (admin)
  PUT     /{id}                    # Update school (admin)
  
/api/v1/schools/map                # Map view
  GET     /                        # Get schools for map display
  
/api/v1/schools/top-rated          # Rankings
  GET     /                        # Get top-rated schools
  
/api/v1/schools/{id}/reviews       # Review management
  GET     /                        # Get all reviews for school
  POST    /                        # Create review (anonymous)
  
/api/v1/reviews                    # Global review operations
  GET     /{id}                    # Get specific review
```

### 4.3 OpenAPI Specification

All APIs are documented using OpenAPI 3.x specification:

- **Request/Response schemas** defined
- **Validation rules** documented
- **Authentication requirements** specified
- **Error responses** documented
- **Example payloads** provided

### 4.4 Arazzo Workflow Specification

Arazzo specifications define API workflows:

- **User journey flows** (e.g., browse → select → review)
- **Multi-step operations** (e.g., filter → rank → view details)
- **Error handling paths**
- **Dependency chains** between API calls

---

## 5. Backend Architecture

### 5.1 .NET 10 Implementation

**Framework:** ASP.NET Core 10  
**API Style:** Minimal APIs (lightweight, performance-focused)

**Key Characteristics:**
- Reduced ceremony compared to controller-based APIs
- Top-level statements for clean Program.cs
- Source generators for improved performance
- Native AOT compilation support

### 5.2 Minimal API Structure

**Example: GetSchools Endpoint**

```csharp
// Endpoint.cs
public static IEndpointRouteBuilder MapGetSchoolsEndpoint(this IEndpointRouteBuilder endpoints)
{
    endpoints.MapGet("api/v1/schools", async (
        int? page, 
        int? pageSize, 
        CancellationToken cancellationToken, 
        IHandler<Request, Response> handler) =>
    {
        var request = new Request
        {
            Page = page.GetValueOrDefault(1),
            PageSize = pageSize.GetValueOrDefault(20)
        };

        return await handler.HandleAsync(request, cancellationToken);
    })
    .WithName("GetSchools")
    .WithOpenApi();

    return endpoints;
}
```

### 5.3 Dependency Injection

Each vertical slice registers its own dependencies:

```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddGetSchoolsEndpoint(this IServiceCollection services)
{
    services.AddScoped<IHandler<Request, Response>, Handler>();
    services.AddScoped<IReadOnlyRepository<SchoolViewModel>, ReadonlyRepository>();
    return services;
}
```

### 5.4 Project Structure

```
RateYourSchool.Api/
├── Program.cs                    # Application entry point
├── Endpoints/                    # Vertical slices
│   ├── GetSchools/
│   ├── CreateReview/
│   └── ...
├── Domain/                       # Core business entities
│   ├── Entities/
│   └── Repositories/
├── Infrastructure/               # Cross-cutting concerns
│   ├── MongoDB/
│   ├── OpenTelemetry/
│   └── EventHandlers/
└── appsettings.json
```

---

## 6. Database Architecture

### 6.1 MongoDB as Primary Database

**Database:** MongoDB  
**Driver:** MongoDB.Driver for .NET

**Rationale:**
- **Flexible Schema**: Handles evolving domain models
- **Document Model**: Natural fit for read models
- **Change Streams**: Built-in event streaming
- **Scalability**: Horizontal scaling through sharding
- **Geospatial Queries**: Native support for location-based search

### 6.2 Database Organization

**Database:** `RateYourSchool`

**Collections:**

```
Write Model Collections:
├── schools              # School write models
└── userReviews          # UserReview write models

Read Model Collections:
├── schoolRates          # Denormalized school + reviews + scores
├── schoolMapViews       # Lightweight map projections
└── reviewLists          # Paginated review indexes
```

### 6.3 CQRS Data Flow

**Write Path:**
1. Command received (e.g., CreateReview)
2. Write to `userReviews` collection
3. MongoDB Change Stream detects insert
4. Event handler triggered
5. Read model updated (e.g., `schoolRates`)

**Read Path:**
1. Query received (e.g., GetSchools)
2. Read directly from read model collection (e.g., `schoolRates`)
3. No joins or complex aggregations needed
4. Fast, optimized queries

### 6.4 Indexes

**Write Collections:**
```javascript
// schools
{ schoolId: 1 }              // Primary key
{ city: 1, province: 1 }     // Location filtering
{ location: "2dsphere" }     // Geospatial queries
{ isPublished: 1 }           // Published schools

// userReviews
{ reviewId: 1 }              // Primary key
{ schoolId: 1, createdAt: -1 } // Reviews by school
{ createdAt: -1 }            // Recent reviews
```

**Read Collections:**
```javascript
// schoolRates
{ schoolId: 1 }              // Primary key
{ "score.total": -1 }        // Top-rated sorting
{ city: 1, "score.total": -1 } // Top in city
{ location: "2dsphere" }     // Geospatial queries

// schoolMapViews
{ location: "2dsphere" }     // Map display
{ province: 1, city: 1 }     // Location filtering
```

### 6.5 Data Consistency

- **Eventual Consistency**: Read models updated asynchronously via Change Streams
- **Acceptable Delay**: Brief lag between write and read model updates (typically milliseconds)
- **Idempotent Updates**: Event handlers designed to handle duplicate events

---

## 7. Frontend Architecture

### 7.1 React Single Page Application

**Framework:** React  
**Architecture:** Single Page Application (SPA)  
**Language:** TypeScript

**Key Characteristics:**
- Client-side routing (React Router)
- Component-based architecture
- State management for global state
- Lazy loading for performance
- Responsive design (mobile-first)

### 7.2 Application Structure

```
rateyourschool-web/
├── public/
│   └── index.html
├── src/
│   ├── components/           # Reusable UI components
│   │   ├── SchoolCard/
│   │   ├── ReviewForm/
│   │   ├── Map/
│   │   └── ...
│   ├── pages/                # Route-level pages
│   │   ├── HomePage/
│   │   ├── SchoolDetailsPage/
│   │   ├── MapPage/
│   │   └── ...
│   ├── services/             # API client services
│   │   ├── schoolService.ts
│   │   ├── reviewService.ts
│   │   └── apiClient.ts
│   ├── hooks/                # Custom React hooks
│   │   ├── useSchools.ts
│   │   ├── useReviews.ts
│   │   └── useGeolocation.ts
│   ├── state/                # State management
│   │   ├── store.ts
│   │   └── slices/
│   ├── types/                # TypeScript type definitions
│   │   ├── School.ts
│   │   ├── Review.ts
│   │   └── api.ts
│   ├── utils/                # Utility functions
│   ├── App.tsx               # Root component
│   └── main.tsx              # Entry point
├── package.json
└── tsconfig.json
```

### 7.3 Key Features

**SPA Benefits:**
- **Fast Navigation**: No full page reloads
- **Rich Interactions**: Dynamic UI updates
- **Offline Capability**: Service workers for PWA features
- **SEO Optimization**: Server-side rendering (SSR) considerations

**Component Examples:**
- `<SchoolMap />` - Interactive map with school markers
- `<SchoolList />` - Filterable, sortable school grid
- `<ReviewForm />` - Anonymous review submission
- `<FilterPanel />` - Advanced search and filtering UI

### 7.4 API Integration

**HTTP Client:** Axios or Fetch API  
**Type Safety:** Generated TypeScript types from OpenAPI spec  
**Error Handling:** Centralized error interceptors  
**Caching:** React Query or SWR for data fetching

---

## 8. Event-Driven Communication

### 8.1 MongoDB Change Streams

**Technology:** MongoDB Change Streams  
**Purpose:** Event-driven read model synchronization

**Architecture:**
- Change Streams monitor write collections in real-time
- Triggers event handlers when documents change
- Enables asynchronous CQRS implementation

### 8.2 Change Stream Implementation

**Monitored Collections:**
- `schools` - School write model changes
- `userReviews` - Review submissions

**Event Flow:**

```
1. Write Operation
   ↓
2. MongoDB Change Stream Detection
   ↓
3. Event Handler Invocation
   ↓
4. Read Model Update
   ↓
5. Updated Data Available for Queries
```

**Example Event Handler:**

```csharp
public class UserReviewCreatedHandler : IChangeStreamHandler
{
    public async Task HandleAsync(ChangeStreamDocument<UserReview> change)
    {
        if (change.OperationType == ChangeStreamOperationType.Insert)
        {
            var review = change.FullDocument;
            
            // Update SchoolRate read model
            await UpdateSchoolRateAsync(review.SchoolId);
        }
    }
    
    private async Task UpdateSchoolRateAsync(string schoolId)
    {
        // 1. Get school from write model
        // 2. Get all reviews for school
        // 3. Calculate aggregate score
        // 4. Get last 25 reviews
        // 5. Update/replace SchoolRate read model
    }
}
```

### 8.3 Change Stream Benefits

- **Real-Time Updates**: Near-instantaneous read model synchronization
- **Scalability**: Horizontal scaling via replica sets
- **Reliability**: Built-in resume capabilities after failures
- **Simplicity**: No external message broker needed
- **Consistency**: Guaranteed order within a shard

### 8.4 Event Processing Guarantees

- **At-Least-Once Delivery**: Events may be processed multiple times
- **Idempotent Handlers**: Designed to handle duplicate events safely
- **Resume Tokens**: Checkpoint for recovery after crashes
- **Ordered Processing**: Events processed in order within a shard

---

## 9. Testing Strategy

### 9.1 Testing Pyramid

```
        ╱╲
       ╱  ╲
      ╱ E2E ╲          Small number of end-to-end tests
     ╱──────╲
    ╱        ╲
   ╱Integration╲       Moderate number of integration tests
  ╱────────────╲
 ╱              ╲
╱  Unit Tests    ╲    Large number of unit tests (base)
──────────────────
```

### 9.2 Unit Testing

**Framework:** xUnit  
**Coverage:** Business logic, domain entities, handlers, validation

**Characteristics:**
- Fast execution (milliseconds)
- Isolated (no external dependencies)
- Mocked dependencies (repositories, databases)
- High code coverage target (>80%)

**Example Test Structure:**

```csharp
public class SchoolTests
{
    [Fact]
    public void Create_ShouldSetInitialStateCorrectly()
    {
        // Arrange
        var name = "Test School";
        var city = "Toronto";
        
        // Act
        var school = new School(name, address, city, ...);
        
        // Assert
        Assert.False(school.IsPublished);
        Assert.Equal(0, school.Score.Total);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_ShouldThrowWhenNameIsInvalid(string invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new School(invalidName, address, city, ...));
    }
}
```

**Test Organization:**

```
RateYourSchool.Tests.Unit/
├── Domain/
│   ├── Entities/
│   │   ├── SchoolTests.cs
│   │   ├── SchoolRateTests.cs
│   │   └── UserReviewTests.cs
│   └── Validators/
├── Endpoints/
│   ├── GetSchools/
│   │   └── HandlerTests.cs
│   └── CreateReview/
│       └── HandlerTests.cs
└── TestFixtures/
```

### 9.3 Integration Testing

**Framework:** ReqnRoll/SpecFlow + xUnit  
**Language:** Gherkin  
**Approach:** Behavior-Driven Development (BDD)

**Characteristics:**
- Test entire vertical slices
- Real HTTP requests via TestServer
- Real MongoDB (or testcontainers)
- Gherkin scenarios for stakeholder readability

**Example Gherkin Scenario:**

```gherkin
Feature: Create School Review
  As an anonymous user
  I want to submit a review for a school
  So that I can share my experience with others

  Scenario: Anonymous user creates a valid review
    Given a school exists with id "school-123"
    When I submit a review with:
      | Field      | Value                    |
      | SchoolId   | school-123               |
      | Username   | JohnDoe                  |
      | Comment    | Great school!            |
      | Rating     | 4.5                      |
    Then the review should be created successfully
    And the response status should be 201
    And the SchoolRate read model should be updated

  Scenario: Create review with missing required fields
    When I submit a review without a username
    Then the response status should be 400
    And the error message should contain "Username is required"
```

**ReqnRoll/SpecFlow Step Definitions:**

```csharp
[Binding]
public class CreateReviewSteps
{
    private readonly ScenarioContext _context;
    private readonly TestWebApplicationFactory _factory;
    
    [Given(@"a school exists with id ""(.*)""")]
    public async Task GivenSchoolExists(string schoolId)
    {
        // Setup: Insert test school into database
    }
    
    [When(@"I submit a review with:")]
    public async Task WhenISubmitReview(Table table)
    {
        // Act: POST to /api/v1/schools/{id}/reviews
    }
    
    [Then(@"the review should be created successfully")]
    public void ThenReviewCreatedSuccessfully()
    {
        // Assert: Check response and database
    }
}
```

**Test Organization:**

```
RateYourSchool.Tests.Integration/
├── Features/
│   ├── CreateReview.feature
│   ├── GetSchools.feature
│   ├── FilterSchools.feature
│   └── ...
├── StepDefinitions/
│   ├── CreateReviewSteps.cs
│   ├── GetSchoolsSteps.cs
│   └── CommonSteps.cs
├── Support/
│   ├── TestWebApplicationFactory.cs
│   ├── MongoDbFixture.cs
│   └── TestDataBuilder.cs
└── Config/
    └── reqnroll.json
```

### 9.4 Test Data Management

**Strategy:**
- **Builders**: Fluent test data builders for complex objects
- **Fixtures**: Reusable test data setup
- **Cleanup**: Automatic database cleanup after tests
- **Isolation**: Each test uses unique data to avoid conflicts

**Example:**

```csharp
public class SchoolBuilder
{
    public School Build()
    {
        return new School(
            name: "Test School",
            address: "123 Test St",
            city: "Toronto",
            province: "ON",
            type: SchoolType.Public,
            grades: new[] { EducationGradeType.Elementary },
            location: new GeoLocation(43.6532, -79.3832),
            eventLogs: Enumerable.Empty<EventLog>()
        );
    }
    
    public SchoolBuilder WithName(string name) { ... }
    public SchoolBuilder InCity(string city) { ... }
}
```

### 9.5 Test Coverage Goals

| Test Type | Coverage Target | Focus Area |
|-----------|----------------|------------|
| **Unit Tests** | >80% code coverage | Business logic, domain rules |
| **Integration Tests** | All critical paths | API endpoints, database operations |
| **E2E Tests** | Key user journeys | Complete workflows |

---

## 10. Observability

### 10.1 OpenTelemetry Implementation

**Framework:** OpenTelemetry  
**Purpose:** Distributed tracing, metrics, and logging

**Three Pillars of Observability:**
1. **Traces** - Distributed request tracing
2. **Metrics** - Performance and business metrics
3. **Logs** - Structured application logging

### 10.2 Distributed Tracing

**Implementation:**
- Automatic instrumentation for ASP.NET Core
- Custom spans for business operations
- Trace context propagation across services
- Integration with MongoDB driver

**Example Trace:**

```
GET /api/v1/schools
├─ Handler.HandleAsync
│  ├─ Repository.GetAsync
│  │  └─ MongoDB.Find (200ms)
│  └─ MapToResponse (5ms)
└─ Total: 210ms
```

**Code Example:**

```csharp
public async Task<Response> HandleAsync(Request request, CancellationToken ct)
{
    using var activity = ActivitySource.StartActivity("GetSchools.Handle");
    activity?.SetTag("page", request.Page);
    activity?.SetTag("pageSize", request.PageSize);
    
    var schools = await _repository.GetAsync(request.Page, request.PageSize, ct);
    
    activity?.SetTag("results.count", schools.Count());
    
    return new Response { Schools = schools };
}
```

### 10.3 Metrics

**Types of Metrics:**
- **Request Metrics**: Request count, duration, status codes
- **Business Metrics**: Reviews created, schools viewed, searches performed
- **Database Metrics**: Query duration, connection pool usage
- **System Metrics**: CPU, memory, garbage collection

**Custom Metrics Example:**

```csharp
// Define metrics
private static readonly Counter<long> ReviewsCreated = 
    Meter.CreateCounter<long>("reviews.created");

private static readonly Histogram<double> ReviewProcessingTime = 
    Meter.CreateHistogram<double>("reviews.processing_time_ms");

// Record metrics
ReviewsCreated.Add(1, new KeyValuePair<string, object>("school.id", schoolId));
ReviewProcessingTime.Record(elapsed.TotalMilliseconds);
```

### 10.4 Structured Logging

**Framework:** ILogger with OpenTelemetry integration  
**Format:** Structured JSON logs

**Log Levels:**
- **Trace**: Detailed flow information
- **Debug**: Internal state for debugging
- **Information**: General application flow
- **Warning**: Abnormal but handled conditions
- **Error**: Error conditions
- **Critical**: Critical failures

**Example:**

```csharp
_logger.LogInformation(
    "Review created for school {SchoolId} by user {Username}",
    review.SchoolId,
    review.Username);

_logger.LogWarning(
    "Read model update delayed: {Delay}ms for school {SchoolId}",
    delay.TotalMilliseconds,
    schoolId);
```

### 10.5 Observability Exporters

**Configuration:**

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddMongoDBInstrumentation()
        .AddSource("RateYourSchool")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
```

**Export Targets:**
- **Development**: Console exporter
- **Production**: OTLP (OpenTelemetry Protocol) to observability backend
  - Jaeger (tracing)
  - Prometheus (metrics)
  - Grafana (visualization)

### 10.6 Monitoring Dashboards

**Key Dashboards:**
1. **API Performance**: Request rates, latency percentiles, error rates
2. **Database Performance**: Query performance, connection pools
3. **Business Metrics**: Reviews/day, active schools, popular searches
4. **System Health**: CPU, memory, disk usage

---

## 11. Infrastructure as Code

### 11.1 Terraform Implementation

**Tool:** Terraform  
**Purpose:** Define and provision all infrastructure

**Benefits:**
- **Reproducibility**: Identical environments across dev/staging/prod
- **Version Control**: Infrastructure changes tracked in Git
- **Automation**: Infrastructure provisioning is automated
- **Documentation**: Infrastructure as living documentation

### 11.2 Infrastructure Components

**Resources Managed by Terraform:**

```
Infrastructure/
├── network/
│   ├── vpc.tf              # Virtual network
│   ├── subnets.tf          # Network segments
│   └── security_groups.tf  # Firewall rules
├── compute/
│   ├── app_service.tf      # .NET 10 API hosting
│   ├── scaling.tf          # Auto-scaling rules
│   └── load_balancer.tf    # Traffic distribution
├── database/
│   ├── mongodb_cluster.tf  # MongoDB Atlas/EC2
│   ├── backup.tf           # Backup configuration
│   └── monitoring.tf       # Database monitoring
├── storage/
│   └── blob_storage.tf     # Static file storage
├── observability/
│   ├── logging.tf          # Log aggregation
│   ├── metrics.tf          # Metrics collection
│   └── alerting.tf         # Alert rules
├── frontend/
│   ├── cdn.tf              # Content delivery
│   └── static_hosting.tf   # SPA hosting
└── dns/
    └── domains.tf          # Domain configuration
```

### 11.3 Environment Structure

**Environments:**

```
terraform/
├── modules/                # Reusable modules
│   ├── api/
│   ├── database/
│   └── frontend/
├── environments/
│   ├── dev/
│   │   ├── main.tf
│   │   ├── variables.tf
│   │   └── terraform.tfvars
│   ├── staging/
│   │   └── ...
│   └── production/
│       └── ...
└── shared/
    └── backend.tf          # Remote state configuration
```

### 11.4 Example Terraform Configuration

**API Service:**

```hcl
resource "azurerm_app_service" "api" {
  name                = "rateyourschool-api-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  app_service_plan_id = azurerm_app_service_plan.main.id

  site_config {
    dotnet_framework_version = "v10.0"
    always_on                = true
    health_check_path        = "/health"
  }

  app_settings = {
    "MongoDB__ConnectionString" = var.mongodb_connection_string
    "OTEL_EXPORTER_OTLP_ENDPOINT" = var.otel_endpoint
    "ASPNETCORE_ENVIRONMENT" = var.environment
  }

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}
```

**MongoDB Cluster:**

```hcl
resource "mongodbatlas_cluster" "main" {
  project_id = var.atlas_project_id
  name       = "rateyourschool-${var.environment}"

  cluster_type = "REPLICASET"
  
  provider_name               = "AWS"
  provider_region_name        = "US_EAST_1"
  provider_instance_size_name = var.mongodb_instance_size

  mongo_db_major_version = "7.0"
  auto_scaling_disk_gb_enabled = true

  # Enable change streams
  advanced_configuration {
    oplog_size_mb = 1024
  }
}
```

### 11.5 State Management

**Backend Configuration:**

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "terraform-state"
    storage_account_name = "tfstaterateyourschool"
    container_name       = "tfstate"
    key                  = "production.terraform.tfstate"
  }
}
```

**State Locking:**
- Prevents concurrent modifications
- Ensures consistency across team members
- Remote state for collaboration

### 11.6 Deployment Pipeline

**CI/CD Integration:**

```yaml
# .github/workflows/deploy.yml
name: Deploy Infrastructure

on:
  push:
    branches: [main]
    paths: ['terraform/**']

jobs:
  terraform:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Terraform Init
        run: terraform init
        working-directory: ./terraform/environments/${{ env.ENVIRONMENT }}
      
      - name: Terraform Plan
        run: terraform plan
        working-directory: ./terraform/environments/${{ env.ENVIRONMENT }}
      
      - name: Terraform Apply
        if: github.ref == 'refs/heads/main'
        run: terraform apply -auto-approve
        working-directory: ./terraform/environments/${{ env.ENVIRONMENT }}
```

---

## 12. Architecture Decision Records (ADRs)

### 12.1 Key Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Architecture Pattern** | CQRS | Separate read/write optimization |
| **Code Organization** | Vertical Slice | Feature independence, low coupling |
| **API Style** | REST with Minimal APIs | Simplicity, performance, standards |
| **Backend Framework** | .NET 10 | Modern, performant, cross-platform |
| **Database** | MongoDB | Flexible schema, change streams, geospatial |
| **Frontend Framework** | React SPA | Rich UI, large ecosystem, TypeScript support |
| **Event Mechanism** | MongoDB Change Streams | No external broker, simplicity |
| **Testing Framework** | xUnit + ReqnRoll/SpecFlow | Industry standard, BDD support |
| **API Specification** | OpenAPI + Arazzo | Standards-based, tooling support |
| **Observability** | OpenTelemetry | Vendor-neutral, comprehensive |
| **Infrastructure** | Terraform | Multi-cloud, mature, declarative |

---

## Appendix

### A. Technology Versions

- **.NET**: 10.0 (LTS)
- **C#**: 12.0
- **MongoDB**: 7.0+
- **React**: 18+
- **TypeScript**: 5.0+
- **Terraform**: 1.5+
- **OpenTelemetry**: 1.5+

### B. External Dependencies

**NuGet Packages:**
- MongoDB.Driver
- OpenTelemetry.Exporter.OpenTelemetryProtocol
- OpenTelemetry.Instrumentation.AspNetCore
- Swashbuckle.AspNetCore (OpenAPI)

**NPM Packages:**
- react, react-dom
- react-router-dom
- axios
- @opentelemetry/api

### C. Non-Functional Requirements

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| **API Response Time** | <200ms (p95) | OpenTelemetry traces |
| **Availability** | 99.9% | Uptime monitoring |
| **Read Model Lag** | <100ms (p95) | Custom metrics |
| **Test Coverage** | >80% | Code coverage tools |
| **API Documentation** | 100% | OpenAPI completeness |

### D. Security Considerations

- **HTTPS**: All traffic encrypted in transit
- **Input Validation**: Request validation at API boundary
- **Rate Limiting**: Prevent abuse
- **CORS**: Configured for frontend domains
- **Secrets Management**: Environment variables, key vaults
- **Data Sanitization**: Prevent XSS, SQL injection equivalent

### E. Scalability Strategy

- **Horizontal Scaling**: Multiple API instances behind load balancer
- **Database Scaling**: MongoDB sharding for large datasets
- **CDN**: Static assets served via CDN
- **Caching**: Redis for frequently accessed read models (future)
- **Connection Pooling**: Efficient database connection reuse

---

**Document Control:**
- This document defines the architecture design for RateYourSchool
- Should be updated when architectural decisions change
- Referenced during development and code reviews
- Used for onboarding new team members

**Related Documents:**
- [Business Rules](BUSINESS_RULES.md)
- OpenAPI Specification (TBD)
- Deployment Guide (TBD)
