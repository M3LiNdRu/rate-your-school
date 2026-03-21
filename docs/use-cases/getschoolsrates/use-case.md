# Use Case: Get SchoolRates (List & Detail)

**Status:** Partially Implemented  
**Priority:** High  
**Related Use Cases:** UC-003 (View School Details)  
**Business Rules:** BR-024 through BR-028

---

## Implementation Progress

| Task | Status | Completed |
|------|--------|-----------|
| **Functional Requirements Document** | DONE | 2026-03-16 |
| **API Specifications (OpenAPI + Arazzo)** | DONE | 2026-03-21 |
| **Backend Implementation (Vertical Slice)** | TODO | - |
| **Frontend Implementation** | TODO | - |
| **E2E Tests** | TODO | - |
| **Penetration Tests** | TODO | - |
| **Performance Tests** | TODO | - |

**Status Legend:**
- `TODO` - Not started
- `IN-PROGRESS` - Currently being worked on
- `IN-REVIEW` - Completed and under review
- `DONE` - Completed and verified

---

## 1. Summary

### Overview
This use case enables anonymous users to retrieve a paginated list of schoolRates with comprehensive information including school details, aggregated scores, and recent user reviews. The endpoint supports pagination to handle large datasets efficiently and returns denormalized read models optimized for display.

### Functional Requirements
- Retrieve a list of schoolRates with pagination support
- Return school information (name, address, city, province, type, location)
- Include aggregated multi-dimensional scores (Innovation, Building, Professorate, ManagementTeam, Total)
- Include the last 25 user reviews per school
- Support configurable page size with validation (min: 1, max: 1000)
- Default to page 1 and page size 20 if not specified
- Automatically correct invalid page numbers (< 1) to 1

### Technical Context
- **CQRS Pattern**: Reads from SchoolRate read model (not School write model)
- **Data Source**: MongoDB `schoolRates` collection
- **Architecture**: Vertical Slice in `src/Api/Endpoints/GetSchoolRates/`
- **Current State**: Partially implemented with basic handler and repository

---

## 2. Acceptance Criteria (Gherkin)

```gherkin
Feature: Get SchoolRates List
  As an anonymous user
  I want to retrieve a paginated list of school rates
  So that I can browse and compare schools

  Background:
    Given the following schools exist in the database:
      | SchoolId | Name                  | City      | Province | Type    | Score |
      | school-1 | Maple Leaf Elementary | Toronto   | ON       | Public  | 4.5   |
      | school-2 | Oak Grove High        | Toronto   | ON       | Public  | 3.8   |
      | school-3 | Pine Academy          | Ottawa    | ON       | Private | 4.2   |
      | school-4 | Cedar Charter         | Montreal  | QC       | Charter | 3.9   |
      | school-5 | Birch International   | Vancouver | BC       | Private | 4.7   |

  Scenario: Retrieve first page with default page size
    When I request GET "/api/v1/schools"
    Then the response status should be 200
    And the response should contain 5 schools
    And each school should have the following properties:
      | Property | Type   | Required |
      | Info     | Object | Yes      |
      | Score    | Object | Yes      |
      | Reviews  | Array  | Yes      |
    And each school Info should contain:
      | Property | Type   | Required |
      | Name     | String | Yes      |
      | Address  | String | Yes      |
      | ImageUrl | String | Yes      |
    And each school Score should contain:
      | Property       | Type    | Required |
      | Innovation     | Decimal | Yes      |
      | Building       | Decimal | Yes      |
      | Professorate   | Decimal | Yes      |
      | ManagementTeam | Decimal | Yes      |
      | Total          | Decimal | Yes      |

  Scenario: Retrieve specific page with custom page size
    When I request GET "/api/v1/schools?page=1&pageSize=2"
    Then the response status should be 200
    And the response should contain 2 schools
    And the schools should be ordered by creation date descending

  Scenario: Retrieve second page
    When I request GET "/api/v1/schools?page=2&pageSize=2"
    Then the response status should be 200
    And the response should contain 2 schools
    And the schools should be different from page 1

  Scenario: Handle invalid page number (less than 1)
    When I request GET "/api/v1/schools?page=0"
    Then the response status should be 200
    And the page should be automatically corrected to 1
    And the response should contain schools from page 1

  Scenario: Handle invalid page number (negative)
    When I request GET "/api/v1/schools?page=-5&pageSize=10"
    Then the response status should be 200
    And the page should be automatically corrected to 1

  Scenario: Handle page size exceeding maximum
    When I request GET "/api/v1/schools?pageSize=2000"
    Then the response status should be 200
    And the page size should be clamped to 1000
    And the response should contain at most 1000 schools

  Scenario: Handle page size below minimum
    When I request GET "/api/v1/schools?pageSize=0"
    Then the response status should be 200
    And the page size should be clamped to 1
    And the response should contain at least 1 school

  Scenario: Handle empty result set
    Given there are no schools in the database
    When I request GET "/api/v1/schools"
    Then the response status should be 200
    And the response should contain 0 schools
    And the Schools array should be empty

  Scenario: Each school includes last 25 reviews
    Given school "school-1" has 30 user reviews
    When I request GET "/api/v1/schools"
    And I find the school with id "school-1" in the response
    Then that school should have exactly 25 reviews
    And the reviews should be the most recent 25 reviews

  Scenario: Reviews are ordered by date descending
    Given school "school-1" has reviews with dates:
      | Username | Comment        | Date       |
      | User1    | Great!         | 2026-03-01 |
      | User2    | Excellent      | 2026-03-10 |
      | User3    | Amazing        | 2026-03-05 |
    When I request GET "/api/v1/schools"
    And I find the school with id "school-1" in the response
    Then the reviews should be ordered:
      | Username | Date       |
      | User2    | 2026-03-10 |
      | User3    | 2026-03-05 |
      | User1    | 2026-03-01 |

  Scenario: Performance - Response time within acceptable range
    Given the database contains 1000 schools
    When I request GET "/api/v1/schools?page=1&pageSize=20"
    Then the response status should be 200
    And the response time should be less than 200ms
```

---

## 3. OpenAPI Schema & Arazzo Flow

### 3.1 Define OpenAPI Schema

**File:** `src/Api/openapi/schools.yaml` (or within main OpenAPI spec)

```yaml
openapi: 3.0.3
info:
  title: RateYourSchool API
  version: 1.0.0

paths:
  /api/v1/schools:
    get:
      summary: Get list of school rates
      description: |
        Retrieves a paginated list of school rates with comprehensive information
        including school details, aggregated scores, and recent reviews.
        
        **CQRS Note:** This endpoint reads from the SchoolRate read model.
      operationId: getSchoolRates
      tags:
        - Schools
      parameters:
        - name: page
          in: query
          description: Page number (1-based). Invalid values (<1) are corrected to 1.
          required: false
          schema:
            type: integer
            minimum: 1
            default: 1
            example: 1
        - name: pageSize
          in: query
          description: Number of items per page. Clamped to [1, 1000].
          required: false
          schema:
            type: integer
            minimum: 1
            maximum: 1000
            default: 20
            example: 20
      responses:
        '200':
          description: Successfully retrieved school rates list
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GetSchoolRatesResponse'
              examples:
                success:
                  summary: Successful response with schools
                  value:
                    schools:
                      - info:
                          name: "Maple Leaf Elementary"
                          address: "123 Main St, Toronto, ON"
                          imageUrl: "https://example.com/school1.jpg"
                        score:
                          innovation: 4.5
                          building: 4.2
                          professorate: 4.6
                          managementTeam: 4.3
                          total: 4.4
                        reviews:
                          - userName: "JohnDoe"
                            comment: "Excellent school with great teachers!"
                            rating: 5.0
                          - userName: "JaneSmith"
                            comment: "Good facilities and programs"
                            rating: 4.0
        '500':
          description: Internal server error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

components:
  schemas:
    GetSchoolRatesResponse:
      type: object
      required:
        - schools
      properties:
        schools:
          type: array
          items:
            $ref: '#/components/schemas/SchoolViewModel'
    
    SchoolViewModel:
      type: object
      required:
        - info
        - score
        - reviews
      properties:
        info:
          $ref: '#/components/schemas/SchoolInfo'
        score:
          $ref: '#/components/schemas/Score'
        reviews:
          type: array
          maxItems: 25
          items:
            $ref: '#/components/schemas/UserReview'
    
    SchoolInfo:
      type: object
      required:
        - name
        - address
        - imageUrl
      properties:
        name:
          type: string
          example: "Maple Leaf Elementary"
        address:
          type: string
          example: "123 Main St, Toronto, ON"
        imageUrl:
          type: string
          format: uri
          example: "https://example.com/school1.jpg"
    
    Score:
      type: object
      required:
        - innovation
        - building
        - professorate
        - managementTeam
        - total
      properties:
        innovation:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          example: 4.5
        building:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          example: 4.2
        professorate:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          example: 4.6
        managementTeam:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          example: 4.3
        total:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          example: 4.4
          description: Average of all four dimensions
    
    UserReview:
      type: object
      required:
        - userName
        - comment
        - rating
      properties:
        userName:
          type: string
          example: "JohnDoe"
        comment:
          type: string
          example: "Great school with excellent facilities"
        rating:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          example: 4.5
    
    Error:
      type: object
      properties:
        message:
          type: string
        code:
          type: string
```

### 3.2 Arazzo Workflow Definition

**File:** `src/Api/arazzo/browse-schools-workflow.yaml`

```yaml
arazzo: 1.0.0
info:
  title: Browse Schools Workflow
  version: 1.0.0
  description: |
    Workflow for browsing and exploring schools, starting with 
    a list view and drilling down to details.

sourceDescriptions:
  - name: rateyourschool-api
    type: openapi
    url: ./openapi/schools.yaml

workflows:
  - workflowId: browse-schoolrates
    summary: Browse school rates and view details
    description: |
      User journey: List school rates → Select school → View details
    
    inputs:
      type: object
      properties:
        initialPage:
          type: integer
          default: 1
        initialPageSize:
          type: integer
          default: 20
    
    steps:
      - stepId: listSchoolRates
        description: Retrieve initial list of school rates
        operationId: getSchoolRates
        parameters:
          - name: page
            in: query
            value: $inputs.initialPage
          - name: pageSize
            in: query
            value: $inputs.initialPageSize
        successCriteria:
          - condition: $statusCode == 200
        outputs:
          schoolsList: $response.body.schools
          firstSchoolName: $response.body.schools[0].info.name
      
      - stepId: selectSchool
        description: User selects a school from the list
        # This would link to getSchoolById when implemented
        dependsOn: listSchools
        outputs:
          selectedSchool: $steps.listSchools.outputs.schoolsList[0]
      
      - stepId: loadNextPage
        description: Load next page of results (pagination)
        operationId: getSchoolRates
        dependsOn: listSchoolRates
        parameters:
          - name: page
            in: query
            value: $inputs.initialPage + 1
          - name: pageSize
            in: query
            value: $inputs.initialPageSize
        successCriteria:
          - condition: $statusCode == 200
        outputs:
          nextPageSchools: $response.body.schools

    outputs:
      initialSchools: $steps.listSchoolRates.outputs.schoolsList
      selectedSchoolName: $steps.selectSchool.outputs.selectedSchool.info.name
      hasNextPage: $steps.loadNextPage.outputs.nextPageSchools.length > 0
```

### 3.3 Implementation Steps

1. **Create OpenAPI specification file** in `src/Api/openapi/schools.yaml`
2. **Create Arazzo workflow file** in `src/Api/arazzo/browse-schools-workflow.yaml`
3. **Configure Swashbuckle** in `Program.cs` to include OpenAPI documentation:
   ```csharp
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen(options =>
   {
       options.SwaggerDoc("v1", new OpenApiInfo
       {
           Title = "RateYourSchool API",
           Version = "v1"
       });
   });
   ```
4. **Add OpenAPI annotations** to endpoint in `Endpoint.cs`:
   ```csharp
   .WithName("GetSchoolRates")
   .WithOpenApi(operation => new(operation)
   {
       Summary = "Get list of school rates",
       Description = "Retrieves a paginated list of school rates..."
   })
   .Produces<Response>(200)
   .Produces<ProblemDetails>(500);
   ```
5. **Validate schema** using OpenAPI validation tools
6. **Test Arazzo workflow** using Arazzo runtime/validator

---

## 4. Backend Implementation

### 4.1 Current State Analysis

**Existing Files:**
- ✅ `Endpoint.cs` - Basic endpoint mapping
- ✅ `Handler.cs` - Business logic with pagination validation
- ✅ `ReadonlyRepository.cs` - Repository interface definition
- ✅ `Request.cs` - Request model
- ✅ `Response.cs` - Response model
- ✅ `SchoolViewModel.cs` - View model
- ✅ `ServiceCollectionExtensions.cs` - DI registration

**Missing/Incomplete:**
- ❌ MongoDB repository implementation (ReadonlyRepository is interface only)
- ❌ MongoDB configuration and connection
- ❌ SchoolRate read model mapping from MongoDB documents
- ❌ Error handling and logging
- ❌ OpenTelemetry instrumentation

### 4.2 Implementation Steps

#### Step 1: Configure MongoDB Connection

**File:** `src/Api/appsettings.json`

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "RateYourSchool",
    "Collections": {
      "SchoolRates": "schoolRates"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**File:** `src/Api/Infrastructure/MongoDB/MongoDbSettings.cs` (NEW)

```csharp
namespace RateYourSchool.Infrastructure.MongoDB;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public CollectionSettings Collections { get; set; } = new();
}

public class CollectionSettings
{
    public string SchoolRates { get; set; } = "schoolRates";
    public string Schools { get; set; } = "schools";
    public string UserReviews { get; set; } = "userReviews";
}
```

#### Step 2: Create MongoDB Models

**File:** `src/Api/Infrastructure/MongoDB/Models/SchoolRateDocument.cs` (NEW)

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RateYourSchool.Infrastructure.MongoDB.Models;

public class SchoolRateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    
    public string SchoolId { get; set; } = string.Empty;
    
    public SchoolInfoDocument Info { get; set; } = new();
    
    public ScoreDocument Score { get; set; } = new();
    
    public List<UserReviewDocument> Reviews { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}

public class SchoolInfoDocument
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class ScoreDocument
{
    public decimal Innovation { get; set; }
    public decimal Building { get; set; }
    public decimal Professorate { get; set; }
    public decimal ManagementTeam { get; set; }
    public decimal Total { get; set; }
}

public class UserReviewDocument
{
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Step 3: Implement MongoDB Repository

**File:** `src/Api/Endpoints/GetSchoolRates/MongoDbReadonlyRepository.cs` (NEW)

```csharp
using MongoDB.Driver;
using RateYourSchool.Infrastructure.MongoDB;
using RateYourSchool.Infrastructure.MongoDB.Models;

namespace RateYourSchool.Endpoints.GetSchoolRates;

internal sealed class MongoDbReadonlyRepository : IReadOnlyRepository<SchoolViewModel>
{
    private readonly IMongoCollection<SchoolRateDocument> _collection;
    private readonly ILogger<MongoDbReadonlyRepository> _logger;

    public MongoDbReadonlyRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<MongoDbReadonlyRepository> logger)
    {
        _collection = database.GetCollection<SchoolRateDocument>(
            settings.Collections.SchoolRates);
        _logger = logger;
    }

    public async Task<IEnumerable<SchoolViewModel>> GetAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving schools: page={Page}, pageSize={PageSize}", 
                page, 
                pageSize);

            var skip = (page - 1) * pageSize;

            var documents = await _collection
                .Find(FilterDefinition<SchoolRateDocument>.Empty)
                .SortByDescending(s => s.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            var viewModels = documents.Select(MapToViewModel).ToList();

            _logger.LogInformation(
                "Retrieved {Count} schools for page {Page}", 
                viewModels.Count, 
                page);

            return viewModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error retrieving schools: page={Page}, pageSize={PageSize}", 
                page, 
                pageSize);
            throw;
        }
    }

    private static SchoolViewModel MapToViewModel(SchoolRateDocument document)
    {
        return new SchoolViewModel
        {
            Info = new SchoolInfo(
                document.Info.Name,
                document.Info.Address,
                document.Info.ImageUrl),
            
            Score = new Domain.Entities.Score(
                document.Score.Innovation,
                document.Score.Building,
                document.Score.Professorate,
                document.Score.ManagementTeam),
            
            Reviews = document.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Take(25)
                .Select(r => new UserReview(
                    r.UserName,
                    r.Comment,
                    r.Rating))
                .ToList()
        };
    }
}
```

#### Step 4: Configure MongoDB Services

**File:** `src/Api/Infrastructure/MongoDB/ServiceCollectionExtensions.cs` (NEW)

```csharp
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace RateYourSchool.Infrastructure.MongoDB;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind settings
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDB"));

        // Register MongoDB client
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        // Register database
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return client.GetDatabase(settings.DatabaseName);
        });

        return services;
    }
}
```

#### Step 5: Update Service Registration

**File:** `src/Api/Endpoints/GetSchoolRates/ServiceCollectionExtensions.cs`

```csharp
namespace RateYourSchool.Endpoints.GetSchoolRates;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetSchoolRatesEndpoint(
        this IServiceCollection services)
    {
        services.AddScoped<IHandler<Request, Response>, Handler>();
        services.AddScoped<IReadOnlyRepository<SchoolViewModel>, MongoDbReadonlyRepository>();
        
        return services;
    }
}
```

#### Step 6: Update Program.cs

**File:** `src/Api/Program.cs`

```csharp
using RateYourSchool.Endpoints.GetSchoolRates;
using RateYourSchool.Infrastructure.MongoDB;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// Add endpoints
builder.Services.AddGetSchoolRatesEndpoint();

// Add OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "RateYourSchool API v1.0");
app.MapGetSchoolRatesEndpoint();

app.Run();
```

#### Step 7: Add OpenTelemetry Instrumentation

**File:** `src/Api/Infrastructure/OpenTelemetry/ServiceCollectionExtensions.cs` (NEW)

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RateYourSchool.Infrastructure.OpenTelemetry;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("RateYourSchool.Api"))
                    .AddAspNetCoreInstrumentation()
                    .AddMongoDBInstrumentation()
                    .AddSource("RateYourSchool")
                    .AddConsoleExporter(); // For development
            });

        return services;
    }
}
```

Update `Program.cs`:
```csharp
builder.Services.AddObservability(builder.Configuration);
```

---

## 5. Unit Tests Implementation

### 5.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Unit/RateYourSchool.Tests.Unit.csproj` (NEW)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Api\Api.csproj" />
  </ItemGroup>
</Project>
```

### 5.2 Handler Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolRates/HandlerTests.cs` (NEW)

```csharp
using FluentAssertions;
using Moq;
using RateYourSchool.Endpoints.GetSchoolRates;
using Xunit;

namespace RateYourSchool.Tests.Unit.Endpoints.GetSchoolRates;

public class HandlerTests
{
    private readonly Mock<IReadOnlyRepository<SchoolViewModel>> _mockRepository;
    private readonly Handler _handler;

    public HandlerTests()
    {
        _mockRepository = new Mock<IReadOnlyRepository<SchoolViewModel>>();
        _handler = new Handler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldReturnSchools()
    {
        // Arrange
        var request = new Request { Page = 1, PageSize = 20 };
        var expectedSchools = new List<SchoolViewModel>
        {
            CreateSchoolViewModel("School 1"),
            CreateSchoolViewModel("School 2")
        };

        _mockRepository
            .Setup(r => r.GetAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSchools);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Schools.Should().HaveCount(2);
        result.Schools.Should().BeEquivalentTo(expectedSchools);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-100, 1)]
    public async Task HandleAsync_WithInvalidPageNumber_ShouldCorrectToOne(
        int invalidPage, 
        int expectedPage)
    {
        // Arrange
        var request = new Request { Page = invalidPage, PageSize = 20 };

        _mockRepository
            .Setup(r => r.GetAsync(expectedPage, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchoolViewModel>());

        // Act
        await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.GetAsync(expectedPage, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2000, 1000)]
    [InlineData(1500, 1000)]
    public async Task HandleAsync_WithInvalidPageSize_ShouldClamp(
        int invalidPageSize, 
        int expectedPageSize)
    {
        // Arrange
        var request = new Request { Page = 1, PageSize = invalidPageSize };

        _mockRepository
            .Setup(r => r.GetAsync(1, expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchoolViewModel>());

        // Act
        await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            r => r.GetAsync(1, expectedPageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyRepository_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new Request { Page = 1, PageSize = 20 };

        _mockRepository
            .Setup(r => r.GetAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchoolViewModel>());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Schools.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var request = new Request { Page = 1, PageSize = 20 };
        var cts = new CancellationTokenSource();

        _mockRepository
            .Setup(r => r.GetAsync(1, 20, cts.Token))
            .ReturnsAsync(new List<SchoolViewModel>());

        // Act
        await _handler.HandleAsync(request, cts.Token);

        // Assert
        _mockRepository.Verify(
            r => r.GetAsync(1, 20, cts.Token),
            Times.Once);
    }

    private static SchoolViewModel CreateSchoolViewModel(string name)
    {
        return new SchoolViewModel
        {
            Info = new SchoolInfo(name, "123 Main St", "http://example.com/image.jpg"),
            Score = new Domain.Entities.Score(4.0m, 4.0m, 4.0m, 4.0m),
            Reviews = new List<UserReview>()
        };
    }
}
```

### 5.3 Request Validation Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolRates/RequestTests.cs` (NEW)

```csharp
using FluentAssertions;
using RateYourSchool.Endpoints.GetSchoolRates;
using Xunit;

namespace RateYourSchool.Tests.Unit.Endpoints.GetSchoolRates;

public class RequestTests
{
    [Fact]
    public void Request_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new Request();

        // Assert
        request.Page.Should().Be(0);
        request.PageSize.Should().Be(0);
    }

    [Fact]
    public void Request_ShouldAllowSettingValues()
    {
        // Act
        var request = new Request
        {
            Page = 5,
            PageSize = 50
        };

        // Assert
        request.Page.Should().Be(5);
        request.PageSize.Should().Be(50);
    }
}
```

### 5.4 Run Unit Tests

```bash
# Navigate to test project
cd src/Tests/RateYourSchool.Tests.Unit

# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Run specific test class
dotnet test --filter "FullyQualifiedName~HandlerTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

---

## 6. Integration Tests Implementation

### 6.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Integration/RateYourSchool.Tests.Integration.csproj` (NEW)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="Reqnroll" Version="2.0.0" />
    <PackageReference Include="Reqnroll.xUnit" Version="2.0.0" />
    <PackageReference Include="Reqnroll.Tools.MsBuild" Version="2.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Testcontainers.MongoDb" Version="3.7.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Api\Api.csproj" />
  </ItemGroup>
</Project>
```

### 6.2 Gherkin Feature File

**File:** `src/Tests/RateYourSchool.Tests.Integration/Features/GetSchoolRates.feature` (NEW)

```gherkin
Feature: Get SchoolRates List
  As an anonymous user
  I want to retrieve a paginated list of school rates
  So that I can browse and compare schools

  Background:
    Given the following schools exist in the database:
      | SchoolId | Name                  | City      | Province | Type    | TotalScore |
      | school-1 | Maple Leaf Elementary | Toronto   | ON       | Public  | 4.5        |
      | school-2 | Oak Grove High        | Toronto   | ON       | Public  | 3.8        |
      | school-3 | Pine Academy          | Ottawa    | ON       | Private | 4.2        |

  Scenario: Retrieve first page with default page size
    When I send a GET request to "/api/v1/schools"
    Then the response status code should be 200
    And the response should contain 3 schools
    And each school should have properties "Info", "Score", "Reviews"

  Scenario: Retrieve specific page with custom page size
    When I send a GET request to "/api/v1/schools?page=1&pageSize=2"
    Then the response status code should be 200
    And the response should contain 2 schools

  Scenario: Handle invalid page number
    When I send a GET request to "/api/v1/schools?page=0"
    Then the response status code should be 200
    And the results should be from page 1

  Scenario: Handle page size exceeding maximum
    When I send a GET request to "/api/v1/schools?pageSize=2000"
    Then the response status code should be 200
    And the response should contain at most 1000 schools

  Scenario: Handle empty database
    Given the database is empty
    When I send a GET request to "/api/v1/schools"
    Then the response status code should be 200
    And the response should contain 0 schools
```

### 6.3 Test Web Application Factory

**File:** `src/Tests/RateYourSchool.Tests.Integration/Support/TestWebApplicationFactory.cs` (NEW)

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;

namespace RateYourSchool.Tests.Integration.Support;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithPortBinding(27017, true)
        .Build();

    public string MongoConnectionString => _mongoContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] = MongoConnectionString,
                ["MongoDB:DatabaseName"] = "RateYourSchool_Test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Additional test-specific service configuration
        });
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

### 6.4 Step Definitions

**File:** `src/Tests/RateYourSchool.Tests.Integration/StepDefinitions/GetSchoolRatesSteps.cs` (NEW)

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RateYourSchool.Endpoints.GetSchoolRates;
using RateYourSchool.Infrastructure.MongoDB;
using RateYourSchool.Infrastructure.MongoDB.Models;
using RateYourSchool.Tests.Integration.Support;
using Reqnroll;
using Reqnroll.Assist;

namespace RateYourSchool.Tests.Integration.StepDefinitions;

[Binding]
public class GetSchoolRatesSteps : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IMongoDatabase _database;
    private HttpResponseMessage? _response;
    private Response? _responseBody;

    public GetSchoolRatesSteps(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        var scope = factory.Services.CreateScope();
        _database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    }

    [Given(@"the following schools exist in the database:")]
    public async Task GivenTheFollowingSchoolsExist(Table table)
    {
        var schools = table.CreateSet<SchoolTestData>();
        var collection = _database.GetCollection<SchoolRateDocument>("schoolRates");

        var documents = schools.Select(s => new SchoolRateDocument
        {
            Id = Guid.NewGuid().ToString(),
            SchoolId = s.SchoolId,
            Info = new SchoolInfoDocument
            {
                Name = s.Name,
                Address = $"{s.City}, {s.Province}",
                ImageUrl = "http://example.com/default.jpg"
            },
            Score = new ScoreDocument
            {
                Innovation = s.TotalScore,
                Building = s.TotalScore,
                Professorate = s.TotalScore,
                ManagementTeam = s.TotalScore,
                Total = s.TotalScore
            },
            Reviews = new List<UserReviewDocument>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await collection.InsertManyAsync(documents);
    }

    [Given(@"the database is empty")]
    public async Task GivenTheDatabaseIsEmpty()
    {
        var collection = _database.GetCollection<SchoolRateDocument>("schoolRates");
        await collection.DeleteManyAsync(FilterDefinition<SchoolRateDocument>.Empty);
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGETRequestTo(string url)
    {
        _response = await _client.GetAsync(url);
        _responseBody = await _response.Content.ReadFromJsonAsync<Response>();
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int statusCode)
    {
        _response.Should().NotBeNull();
        ((int)_response!.StatusCode).Should().Be(statusCode);
    }

    [Then(@"the response should contain (.*) schools")]
    public void ThenTheResponseShouldContainSchools(int count)
    {
        _responseBody.Should().NotBeNull();
        _responseBody!.Schools.Should().HaveCount(count);
    }

    [Then(@"each school should have properties ""(.*)"", ""(.*)"", ""(.*)""")]
    public void ThenEachSchoolShouldHaveProperties(string prop1, string prop2, string prop3)
    {
        _responseBody.Should().NotBeNull();
        foreach (var school in _responseBody!.Schools)
        {
            school.Info.Should().NotBeNull();
            school.Score.Should().NotBeNull();
            school.Reviews.Should().NotBeNull();
        }
    }

    [Then(@"the response should contain at most (.*) schools")]
    public void ThenTheResponseShouldContainAtMostSchools(int maxCount)
    {
        _responseBody.Should().NotBeNull();
        _responseBody!.Schools.Should().HaveCountLessOrEqualTo(maxCount);
    }

    [Then(@"the results should be from page (.*)")]
    public void ThenTheResultsShouldBeFromPage(int page)
    {
        // Verification that results match expected page
        _responseBody.Should().NotBeNull();
        _responseBody!.Schools.Should().NotBeEmpty();
    }

    [AfterScenario]
    public async Task CleanupDatabase()
    {
        var collection = _database.GetCollection<SchoolRateDocument>("schoolRates");
        await collection.DeleteManyAsync(FilterDefinition<SchoolRateDocument>.Empty);
    }
}

public class SchoolTestData
{
    public string SchoolId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
}
```

### 6.5 ReqnRoll/SpecFlow Configuration

**File:** `src/Tests/RateYourSchool.Tests.Integration/reqnroll.json` (NEW)

```json
{
  "bindingCulture": {
    "name": "en-US"
  },
  "stepAssemblies": [
    {
      "assembly": "RateYourSchool.Tests.Integration"
    }
  ]
}
```

### 6.6 Run Integration Tests

```bash
# Navigate to integration test project
cd src/Tests/RateYourSchool.Tests.Integration

# Run all integration tests
dotnet test

# Run specific feature
dotnet test --filter "FullyQualifiedName~GetSchoolRates"

# Run with living documentation generation
dotnet test --logger "reqnroll;LogFilePath=TestResults/report.html"
```

---

## 7. Frontend Implementation

### 7.1 Project Setup

**Prerequisites:**
- Node.js 20+ installed
- React 18+
- TypeScript 5+

**Initialize React Project:**

```bash
# Create React app with TypeScript
npm create vite@latest rateyourschool-web -- --template react-ts
cd rateyourschool-web

# Install dependencies
npm install axios react-router-dom

# Install dev dependencies
npm install -D @types/react @types/react-dom
```

### 7.2 API Client Service

**File:** `src/services/api/apiClient.ts`

```typescript
import axios, { AxiosInstance } from 'axios';

const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor
apiClient.interceptors.response.use(
  (response) => {
    console.log(`[API] Response: ${response.status}`);
    return response;
  },
  (error) => {
    console.error('[API] Error:', error.message);
    return Promise.reject(error);
  }
);

export default apiClient;
```

### 7.3 TypeScript Types

**File:** `src/types/School.ts`

```typescript
export interface SchoolInfo {
  name: string;
  address: string;
  imageUrl: string;
}

export interface Score {
  innovation: number;
  building: number;
  professorate: number;
  managementTeam: number;
  total: number;
}

export interface UserReview {
  userName: string;
  comment: string;
  rating: number;
}

export interface SchoolViewModel {
  info: SchoolInfo;
  score: Score;
  reviews: UserReview[];
}

export interface GetSchoolRatesResponse {
  schools: SchoolViewModel[];
}

export interface GetSchoolRatesParams {
  page?: number;
  pageSize?: number;
}
```

### 7.4 Schools API Service

**File:** `src/services/schools/schoolService.ts`

```typescript
import apiClient from '../api/apiClient';
import { GetSchoolRatesResponse, GetSchoolRatesParams } from '../../types/School';

export const schoolService = {
  async getSchoolRates(params: GetSchoolRatesParams = {}): Promise<GetSchoolRatesResponse> {
    const { page = 1, pageSize = 20 } = params;
    
    const response = await apiClient.get<GetSchoolRatesResponse>('/api/v1/schools', {
      params: { page, pageSize },
    });
    
    return response.data;
  },
};
```

### 7.5 Custom React Hook

**File:** `src/hooks/useSchools.ts`

```typescript
import { useState, useEffect } from 'react';
import { schoolService } from '../services/schools/schoolService';
import { SchoolViewModel } from '../types/School';

interface UseSchoolsResult {
  schools: SchoolViewModel[];
  loading: boolean;
  error: string | null;
  page: number;
  pageSize: number;
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  refetch: () => void;
}

export const useSchools = (
  initialPage: number = 1,
  initialPageSize: number = 20
): UseSchoolsResult => {
  const [schools, setSchools] = useState<SchoolViewModel[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState<number>(initialPage);
  const [pageSize, setPageSize] = useState<number>(initialPageSize);

  const fetchSchools = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await schoolService.getSchoolRates({ page, pageSize });
      setSchools(response.schools);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch schools');
      console.error('Error fetching schools:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSchools();
  }, [page, pageSize]);

  return {
    schools,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize,
    refetch: fetchSchools,
  };
};
```

### 7.6 School Card Component

**File:** `src/components/SchoolCard/SchoolCard.tsx`

```typescript
import React from 'react';
import { SchoolViewModel } from '../../types/School';
import './SchoolCard.css';

interface SchoolCardProps {
  school: SchoolViewModel;
  onClick?: () => void;
}

export const SchoolCard: React.FC<SchoolCardProps> = ({ school, onClick }) => {
  return (
    <div className="school-card" onClick={onClick}>
      <img 
        src={school.info.imageUrl} 
        alt={school.info.name}
        className="school-card__image"
      />
      
      <div className="school-card__content">
        <h3 className="school-card__name">{school.info.name}</h3>
        <p className="school-card__address">{school.info.address}</p>
        
        <div className="school-card__score">
          <span className="school-card__score-label">Overall:</span>
          <span className="school-card__score-value">
            {school.score.total.toFixed(1)} / 5.0
          </span>
        </div>
        
        <div className="school-card__dimensions">
          <div className="dimension">
            <span className="dimension__label">Innovation</span>
            <span className="dimension__value">{school.score.innovation.toFixed(1)}</span>
          </div>
          <div className="dimension">
            <span className="dimension__label">Building</span>
            <span className="dimension__value">{school.score.building.toFixed(1)}</span>
          </div>
          <div className="dimension">
            <span className="dimension__label">Faculty</span>
            <span className="dimension__value">{school.score.professorate.toFixed(1)}</span>
          </div>
          <div className="dimension">
            <span className="dimension__label">Management</span>
            <span className="dimension__value">{school.score.managementTeam.toFixed(1)}</span>
          </div>
        </div>
        
        <div className="school-card__reviews">
          <span>{school.reviews.length} reviews</span>
        </div>
      </div>
    </div>
  );
};
```

### 7.7 Schools List Page

**File:** `src/pages/SchoolsListPage/SchoolsListPage.tsx`

```typescript
import React from 'react';
import { useSchools } from '../../hooks/useSchools';
import { SchoolCard } from '../../components/SchoolCard/SchoolCard';
import './SchoolsListPage.css';

export const SchoolsListPage: React.FC = () => {
  const { schools, loading, error, page, pageSize, setPage, setPageSize } = useSchools();

  if (loading) {
    return <div className="loading">Loading schools...</div>;
  }

  if (error) {
    return <div className="error">Error: {error}</div>;
  }

  return (
    <div className="schools-list-page">
      <header className="page-header">
        <h1>Browse Schools</h1>
        <p>Discover and compare schools in your area</p>
      </header>

      <div className="controls">
        <div className="pagination-controls">
          <button
            onClick={() => setPage(Math.max(1, page - 1))}
            disabled={page === 1}
          >
            Previous
          </button>
          <span>Page {page}</span>
          <button
            onClick={() => setPage(page + 1)}
            disabled={schools.length < pageSize}
          >
            Next
          </button>
        </div>

        <div className="page-size-controls">
          <label>
            Show:
            <select
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value));
                setPage(1);
              }}
            >
              <option value="10">10</option>
              <option value="20">20</option>
              <option value="50">50</option>
            </select>
            per page
          </label>
        </div>
      </div>

      {schools.length === 0 ? (
        <div className="no-results">
          <p>No schools found</p>
        </div>
      ) : (
        <div className="schools-grid">
          {schools.map((school, index) => (
            <SchoolCard
              key={index}
              school={school}
              onClick={() => console.log('Navigate to school details', school)}
            />
          ))}
        </div>
      )}
    </div>
  );
};
```

### 7.8 Styling (Example)

**File:** `src/pages/SchoolsListPage/SchoolsListPage.css`

```css
.schools-list-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.page-header {
  text-align: center;
  margin-bottom: 2rem;
}

.page-header h1 {
  font-size: 2.5rem;
  margin-bottom: 0.5rem;
}

.controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  padding: 1rem;
  background: #f5f5f5;
  border-radius: 8px;
}

.pagination-controls {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.pagination-controls button {
  padding: 0.5rem 1rem;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.pagination-controls button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.schools-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.loading, .error, .no-results {
  text-align: center;
  padding: 3rem;
  font-size: 1.2rem;
}

.error {
  color: #dc3545;
}
```

### 7.9 Environment Configuration

**File:** `.env.development`

```env
VITE_API_BASE_URL=http://localhost:5000
```

**File:** `.env.production`

```env
VITE_API_BASE_URL=https://api.rateyourschool.com
```

### 7.10 Run Frontend

```bash
# Development mode
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

---

## 8. Implementation Checklist

### API Documentation
- [ ] Create OpenAPI schema file
- [ ] Create Arazzo workflow file
- [ ] Validate OpenAPI schema
- [ ] Test Arazzo workflow

### Backend
- [ ] Configure MongoDB connection settings
- [ ] Create MongoDB document models
- [ ] Implement MongoDbReadonlyRepository
- [ ] Add MongoDB service registration
- [ ] Update Program.cs with MongoDB and OpenTelemetry
- [ ] Add OpenAPI annotations to endpoint
- [ ] Test endpoint manually with Swagger UI

### Testing
- [ ] Create unit test project
- [ ] Write Handler unit tests
- [ ] Write Request validation tests
- [ ] Run unit tests and achieve >80% coverage
- [ ] Create integration test project
- [ ] Write Gherkin feature files
- [ ] Implement ReqnRoll/SpecFlow step definitions
- [ ] Setup Testcontainers for MongoDB
- [ ] Run integration tests

### Frontend
- [ ] Initialize React TypeScript project
- [ ] Create TypeScript type definitions
- [ ] Implement API client service
- [ ] Create useSchools custom hook
- [ ] Build SchoolCard component
- [ ] Build SchoolsListPage
- [ ] Add CSS styling
- [ ] Test frontend integration with backend

### DevOps
- [ ] Update CI/CD pipeline for tests
- [ ] Configure test coverage reporting
- [ ] Setup integration test environment
- [ ] Add OpenTelemetry exporters configuration

---

## 9. Notes for Subagent Implementation

When implementing this use case with multiple subagents, consider:

1. **Backend Subagent**: Focus on sections 4-4.7, implementing MongoDB integration and OpenTelemetry
2. **Testing Subagent**: Focus on sections 5 & 6, creating comprehensive test coverage
3. **API Documentation Subagent**: Focus on section 3, creating OpenAPI and Arazzo specs
4. **Frontend Subagent**: Focus on section 7, building React components and pages
5. **Integration Subagent**: Coordinate between subagents to ensure end-to-end functionality

Each subagent should:
- Read the relevant section thoroughly
- Follow the code examples provided
- Verify implementation with the acceptance criteria (section 2)
- Update this document with any deviations or improvements

---

**Document Version:** 1.0  
**Last Updated:** March 15, 2026  
**Status:** Ready for Implementation
