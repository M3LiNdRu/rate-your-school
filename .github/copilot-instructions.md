# GitHub Copilot Instructions - RateYourSchool

This file provides context and guidelines for GitHub Copilot when generating code for the RateYourSchool project.

---

## Project Overview

RateYourSchool is a web application for rating and reviewing schools, built with:
- **Backend**: .NET 10, ASP.NET Core Minimal APIs
- **Database**: MongoDB
- **Frontend**: React 18+ with TypeScript
- **Testing**: xUnit (unit), ReqnRoll/SpecFlow + Gherkin (integration)
- **Observability**: OpenTelemetry
- **Infrastructure**: Terraform

**Important Git Convention:**
- **Trunk Branch**: `master` (NOT `main`)
- Always use `master` as the base branch for PRs and branch comparisons
- GitHub Actions workflows and git commands should reference `master`

**Key Documentation:**
- Business Rules: `docs/BUSINESS_RULES.md`
- Architecture: `docs/ARCHITECTURE.md`
- Use Cases: `docs/use-cases/`

---

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)

**Write Models** (Commands):
- `School` - Master school data
- `UserReview` - User-generated reviews

**Read Models** (Queries):
- `SchoolRate` - Denormalized view (school info + scores + last 25 reviews)
- `SchoolMapView` - Lightweight projection for map display

**Guidelines:**
- Write operations modify write model collections (`schools`, `userReviews`)
- Read operations query read model collections (`schoolRates`, `schoolMapViews`)
- MongoDB Change Streams synchronize write → read models
- Accept eventual consistency (brief lag is acceptable)

### Vertical Slice Architecture

Each feature is a self-contained vertical slice with all layers together:

```
src/Api/Endpoints/[FeatureName]/
├── Endpoint.cs                    # API endpoint mapping
├── Handler.cs                     # Business logic
├── Repository.cs / ReadonlyRepository.cs
├── Request.cs                     # Input DTO
├── Response.cs                    # Output DTO
├── [ViewModels].cs                # View models
└── ServiceCollectionExtensions.cs # DI registration
```

**When creating new endpoints:**
- Create a new folder under `src/Api/Endpoints/[FeatureName]/`
- Include all files listed above
- Keep features independent (minimal cross-feature dependencies)
- Register services in `ServiceCollectionExtensions.cs`
- Map endpoint in `Program.cs`

---

## Technology Stack & Versions

### Backend
- **.NET**: 10.0 (LTS)
- **C#**: 12.0
- **API Style**: Minimal APIs (not Controllers)
- **MongoDB Driver**: Latest for .NET

### Frontend
- **React**: 18+
- **TypeScript**: 5.0+
- **Build Tool**: Vite
- **HTTP Client**: Axios

### Testing
- **Unit Tests**: xUnit
- **Integration Tests**: ReqnRoll/SpecFlow + xUnit
- **BDD Language**: Gherkin
- **Test Containers**: Testcontainers.MongoDb

### Infrastructure
- **IaC**: Terraform
- **Observability**: OpenTelemetry
- **API Docs**: OpenAPI 3.x + Arazzo

---

## Project Structure

```
RateYourSchool/
├── .github/                       # GitHub configuration
├── docs/                          # Documentation
│   ├── ARCHITECTURE.md
│   ├── BUSINESS_RULES.md
│   └── use-cases/
├── src/                           # Source code
│   ├── Api/                       # ASP.NET Core API
│   │   ├── Endpoints/             # Vertical slices
│   │   ├── Infrastructure/        # Cross-cutting concerns
│   │   │   ├── MongoDB/
│   │   │   ├── OpenTelemetry/
│   │   │   └── EventHandlers/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── openapi/               # OpenAPI specs
│   ├── Domain/                    # Core domain entities
│   │   ├── Entities/
│   │   └── Repositories/
│   ├── Data/                      # Data access
│   └── Tests/
│       ├── RateYourSchool.Tests.Unit/
│       └── RateYourSchool.Tests.Integration/
└── RateYourSchool.sln
```

---

## Coding Standards

### C# / .NET

**Naming Conventions:**
- Classes: `PascalCase`
- Methods: `PascalCase`
- Private fields: `_camelCase` with underscore prefix
- Parameters: `camelCase`
- Constants: `PascalCase`
- Async methods: Suffix with `Async`

**File Organization:**
- One class per file
- File name matches class name
- Use `sealed` for classes not designed for inheritance
- Use `internal` for implementation details not exposed outside assembly

**Patterns to Follow:**

```csharp
// Minimal API Endpoint
public static IEndpointRouteBuilder MapGetSchoolRatesEndpoint(this IEndpointRouteBuilder endpoints)
{
    endpoints.MapGet("api/v1/schools", async (
        int? page, 
        int? pageSize, 
        CancellationToken cancellationToken, 
        IHandler<Request, Response> handler) =>
    {
        var request = new Request { Page = page ?? 1, PageSize = pageSize ?? 20 };
        return await handler.HandleAsync(request, cancellationToken);
    })
    .WithName("GetSchoolRates")
    .WithOpenApi()
    .Produces<Response>(200)
    .Produces<ProblemDetails>(500);

    return endpoints;
}

// Handler Pattern
internal sealed class Handler : IHandler<Request, Response>
{
    private readonly IRepository _repository;
    private readonly ILogger<Handler> _logger;

    public Handler(IRepository repository, ILogger<Handler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Response> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        // Business logic here
        
        return new Response { /* ... */ };
    }
}

// Service Registration
public static IServiceCollection AddFeatureNameEndpoint(this IServiceCollection services)
{
    services.AddScoped<IHandler<Request, Response>, Handler>();
    services.AddScoped<IRepository, MongoDbRepository>();
    return services;
}
```

**MongoDB Patterns:**

```csharp
// Document Model (Infrastructure layer)
public class SchoolRateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    
    // Properties use PascalCase
}

// Repository Pattern
internal sealed class MongoDbRepository : IRepository
{
    private readonly IMongoCollection<Document> _collection;
    private readonly ILogger<MongoDbRepository> _logger;

    public async Task<IEnumerable<T>> GetAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        var skip = (page - 1) * pageSize;
        
        return await _collection
            .Find(FilterDefinition<Document>.Empty)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }
}
```

### TypeScript / React

**Naming Conventions:**
- Components: `PascalCase`
- Functions/variables: `camelCase`
- Types/Interfaces: `PascalCase`
- Files: `kebab-case.ts` or `PascalCase.tsx` (components)
- CSS files: Match component name

**Patterns to Follow:**

```typescript
// Type Definitions
export interface SchoolViewModel {
  info: SchoolInfo;
  score: Score;
  reviews: UserReview[];
}

// API Service
export const schoolService = {
  async getSchoolRates(params: GetSchoolRatesParams = {}): Promise<GetSchoolRatesResponse> {
    const { page = 1, pageSize = 20 } = params;
    const response = await apiClient.get<GetSchoolRatesResponse>('/api/v1/schools', {
      params: { page, pageSize },
    });
    return response.data;
  },
};

// Custom Hook
export const useSchools = (initialPage: number = 1): UseSchoolsResult => {
  const [schools, setSchools] = useState<SchoolViewModel[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Fetch logic
  }, [page]);

  return { schools, loading, error };
};

// React Component
export const SchoolCard: React.FC<SchoolCardProps> = ({ school, onClick }) => {
  return (
    <div className="school-card" onClick={onClick}>
      {/* Component JSX */}
    </div>
  );
};
```

---

## Testing Standards

### Unit Tests (xUnit)

**Location:** `src/Tests/RateYourSchool.Tests.Unit/`

**Naming:**
- Test class: `[ClassUnderTest]Tests`
- Test method: `[MethodName]_[Scenario]_[ExpectedBehavior]`

**Patterns:**

```csharp
public class HandlerTests
{
    private readonly Mock<IRepository> _mockRepository;
    private readonly Handler _handler;

    public HandlerTests()
    {
        _mockRepository = new Mock<IRepository>();
        _handler = new Handler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldReturnSchools()
    {
        // Arrange
        var request = new Request { Page = 1, PageSize = 20 };
        var expectedData = CreateTestData();
        _mockRepository.Setup(r => r.GetAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Schools.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    public async Task HandleAsync_WithInvalidPage_ShouldCorrectToOne(int invalid, int expected)
    {
        // Test implementation
    }
}
```

**Code Coverage Requirements:**
- **Minimum:** 80% line coverage for all business logic
- **Target:** Measure coverage using `dotnet test --collect:"XPlat Code Coverage"`
- **Focus Areas:** Handlers, repositories, domain logic, validation logic
- **Exclusions:** Infrastructure code, Program.cs, DTOs may have lower coverage
- **Enforcement:** All PRs must maintain or improve coverage percentage
- **Reporting:** Use coverlet or similar tools to generate coverage reports

### Integration Tests (ReqnRoll/SpecFlow + Gherkin)

**Location:** `src/Tests/RateYourSchool.Tests.Integration/`

**Gherkin Format:**

```gherkin
Feature: Get SchoolRates List
  As an anonymous user
  I want to retrieve a paginated list of school rates
  So that I can browse and compare schools

  Scenario: Retrieve first page with default page size
    Given the following schools exist in the database:
      | SchoolId | Name          | City    |
      | school-1 | Test School 1 | Toronto |
    When I send a GET request to "/api/v1/schools"
    Then the response status code should be 200
    And the response should contain 1 schools
```

**Step Definitions:**

```csharp
[Binding]
public class GetSchoolRatesSteps : IClassFixture<TestWebApplicationFactory>
{
    [Given(@"the following schools exist in the database:")]
    public async Task GivenSchoolsExist(Table table)
    {
        // Setup test data
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendGetRequest(string url)
    {
        _response = await _client.GetAsync(url);
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenStatusCodeShouldBe(int statusCode)
    {
        ((int)_response.StatusCode).Should().Be(statusCode);
    }
}
```

---

## MongoDB Guidelines

### Connection & Configuration

```csharp
// appsettings.json structure
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "RateYourSchool",
    "Collections": {
      "SchoolRates": "schoolRates",
      "Schools": "schools",
      "UserReviews": "userReviews"
    }
  }
}

// Service registration
services.AddSingleton<IMongoClient>(sp => 
    new MongoClient(settings.ConnectionString));
services.AddSingleton<IMongoDatabase>(sp => 
    sp.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName));
```

### Collections

**Write Collections:**
- `schools` - School write models
- `userReviews` - UserReview write models

**Read Collections:**
- `schoolRates` - Denormalized school + reviews + scores
- `schoolMapViews` - Lightweight map projections

### Indexes

```javascript
// Always create indexes for:
// - Primary keys
// - Foreign keys (schoolId in reviews)
// - Frequently queried fields (city, province)
// - Geospatial fields (location: "2dsphere")
// - Sorting fields (createdAt, score.total)
```

### Change Streams

```csharp
// Monitor write collections for changes
var changeStream = collection.Watch();
await changeStream.ForEachAsync(async change =>
{
    if (change.OperationType == ChangeStreamOperationType.Insert)
    {
        // Update read model
    }
});
```

---

## API Design Guidelines

### REST Principles

- **Resource-based URLs**: `/api/v1/schools`, `/api/v1/schools/{id}/reviews`
- **HTTP Methods**: GET (read), POST (create), PUT (update), DELETE (delete)
- **Status Codes**: 200 (OK), 201 (Created), 400 (Bad Request), 404 (Not Found), 500 (Error)
- **JSON**: All requests/responses use JSON

### Minimal API Pattern

```csharp
// In Program.cs
app.MapGetSchoolRatesEndpoint();
app.MapCreateReviewEndpoint();

// Endpoint files
public static IEndpointRouteBuilder MapXxxEndpoint(this IEndpointRouteBuilder endpoints)
{
    endpoints.Map[Method]("api/v1/[resource]", async ([params], IHandler handler) =>
    {
        // Handle request
    })
    .WithName("[OperationId]")
    .WithOpenApi()
    .Produces<Response>(200);

    return endpoints;
}
```

### Validation

```csharp
// In Handler
public async Task<Response> HandleAsync(Request request, CancellationToken ct)
{
    ArgumentNullException.ThrowIfNull(request);
    
    // Business validation
    var page = request.Page < 1 ? 1 : request.Page;
    var pageSize = Math.Clamp(request.PageSize, 1, 1000);
    
    // Process
}
```

### Error Handling

```csharp
// Use try-catch in repositories
try
{
    return await _collection.Find(...).ToListAsync(ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error retrieving data");
    throw; // Re-throw to let middleware handle
}
```

---

## Logging & Observability

### Structured Logging

```csharp
_logger.LogInformation(
    "Retrieving schools: page={Page}, pageSize={PageSize}", 
    page, 
    pageSize);

_logger.LogWarning(
    "Invalid page number {Page} corrected to 1", 
    request.Page);

_logger.LogError(
    ex, 
    "Error processing request for school {SchoolId}", 
    schoolId);
```

### OpenTelemetry

```csharp
// Create activity sources
private static readonly ActivitySource ActivitySource = new("RateYourSchool");

// Add spans
using var activity = ActivitySource.StartActivity("GetSchools.Handle");
activity?.SetTag("page", request.Page);
activity?.SetTag("results.count", schools.Count());

// Record metrics
private static readonly Counter<long> RequestCounter = 
    Meter.CreateCounter<long>("requests.count");
    
RequestCounter.Add(1, new KeyValuePair<string, object>("endpoint", "GetSchools"));
```

---

## Business Rules References

Always reference business rules in code comments:

```csharp
// BR-024: Pagination Support - page and pageSize parameters
// BR-025: Default Pagination Values - default page=1, pageSize=20
// BR-026: Page Number Validation - correct values < 1 to 1
// BR-027: Page Size Constraints - clamp between 1 and 1000

var page = request.Page < 1 ? 1 : request.Page; // BR-026
var pageSize = Math.Clamp(request.PageSize, 1, 1000); // BR-027
```

**Key Business Rules:**
- BR-001 to BR-008: School Management
- BR-009 to BR-015: SchoolRate Read Model
- BR-016 to BR-018: Scoring System
- BR-024 to BR-028: API & Pagination
- BR-030 to BR-035: Filtering & Ranking
- BR-036 to BR-040: User Reviews

See `docs/BUSINESS_RULES.md` for complete reference.

---

## Security Considerations

- **Input Validation**: Always validate and sanitize user input
- **SQL Injection**: Not applicable (NoSQL), but sanitize queries
- **XSS**: Frontend must sanitize displayed user content
- **CORS**: Configure allowed origins
- **Rate Limiting**: Implement for public endpoints
- **Anonymous Reviews**: Allowed per BR-038 (no auth required)

---

## Performance Guidelines

- **Pagination**: Always paginate list endpoints
- **Indexes**: Ensure MongoDB collections are properly indexed
- **Projections**: Use MongoDB projections to fetch only needed fields
- **Caching**: Consider for frequently accessed read models (future)
- **Response Time**: Target <200ms for p95 (BR-029)
- **Async/Await**: Always use async for I/O operations
- **CancellationToken**: Pass through all async methods

---

## Common Pitfalls to Avoid

❌ **Don't:**
- Use Controllers (use Minimal APIs)
- Put business logic in Program.cs
- Create cross-feature dependencies
- Skip error handling
- Forget CancellationToken parameters
- Use blocking calls (.Result, .Wait())
- Hard-code configuration values
- Skip logging for important operations
- Ignore null safety

✅ **Do:**
- Follow Vertical Slice architecture
- Keep features independent
- Use dependency injection
- Log appropriately (Info, Warning, Error)
- Handle errors gracefully
- Use async/await properly
- Read configuration from appsettings.json
- Write tests alongside code
- Reference business rules in comments

---

## Example: Creating a New Endpoint

1. **Create folder**: `src/Api/Endpoints/FeatureName/`

2. **Create Request.cs**:
```csharp
namespace RateYourSchool.Endpoints.FeatureName;

internal sealed record Request
{
    public required string Parameter { get; init; }
}
```

3. **Create Response.cs**:
```csharp
namespace RateYourSchool.Endpoints.FeatureName;

internal sealed record Response
{
    public required IEnumerable<ResultType> Results { get; init; }
}
```

4. **Create Handler.cs**:
```csharp
namespace RateYourSchool.Endpoints.FeatureName;

internal sealed class Handler : IHandler<Request, Response>
{
    private readonly IRepository _repository;
    private readonly ILogger<Handler> _logger;

    public Handler(IRepository repository, ILogger<Handler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Response> HandleAsync(Request request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        _logger.LogInformation("Processing request for {Parameter}", request.Parameter);
        
        var results = await _repository.GetDataAsync(request.Parameter, ct);
        
        return new Response { Results = results };
    }
}
```

5. **Create Repository (if needed)**

6. **Create ServiceCollectionExtensions.cs**:
```csharp
namespace RateYourSchool.Endpoints.FeatureName;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureNameEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IHandler<Request, Response>, Handler>();
        // Add other services
        return services;
    }
}
```

7. **Create Endpoint.cs**:
```csharp
namespace RateYourSchool.Endpoints.FeatureName;

internal static class FeatureNameEndpoint
{
    public static IEndpointRouteBuilder MapFeatureNameEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/v1/resource", async (
            string? param,
            CancellationToken ct,
            IHandler<Request, Response> handler) =>
        {
            var request = new Request { Parameter = param ?? "default" };
            return await handler.HandleAsync(request, ct);
        })
        .WithName("FeatureName")
        .WithOpenApi()
        .Produces<Response>(200);

        return endpoints;
    }
}
```

8. **Register in Program.cs**:
```csharp
builder.Services.AddFeatureNameEndpoint();
app.MapFeatureNameEndpoint();
```

9. **Write tests** (unit + integration)

---

## Additional Resources

- **Architecture**: `docs/ARCHITECTURE.md`
- **Business Rules**: `docs/BUSINESS_RULES.md`
- **Use Case Template**: `docs/use-cases/USE-CASE-TEMPLATE.md`
- **Example Use Case**: `docs/use-cases/get-schoolrates.md`

---

**Last Updated:** 2026-03-15  
**Version:** 1.0
