# Use Case: Get School Map Data (Lightweight Map View)

**Status:** Not Started  
**Priority:** High  
**Related Use Cases:** UC-001 (Get SchoolRates), UC-003 (View School Details)  
**Business Rules:** BR-008 (Location Requirements), BR-029 (Simplified Map View), BR-030 (Filtering by Location)

---

## Implementation Progress

| Task | Status | Completed |
|------|--------|-----------|
| **Functional Requirements Document** | DONE | 2026-03-16 |
| **API Specifications (OpenAPI + Arazzo)** | TODO | - |
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
This use case provides a lightweight endpoint optimized for displaying schools on an interactive map. It returns only essential school information (ID, name, city, province, and geolocation coordinates) to minimize payload size and maximize rendering performance. The endpoint supports optional filtering by geographic bounds for efficient map viewport updates.

This endpoint is designed for:
- Initial map load with all visible schools
- Map pan/zoom updates with viewport-specific data
- Performance-optimized data transfer (minimal fields)
- Frontend map marker rendering

### Functional Requirements
- Retrieve all schools with minimal data optimized for map display
- Return only essential fields: `id`, `name`, `city`, `province`, `geolocation`
- No pagination - returns all schools in a single response
- Optional geographic bounding box filter (future enhancement)
- Ordered by name ascending for consistent results

### Technical Context
- **CQRS Pattern**: Reads from `School` collection with field projection
- **Data Source**: MongoDB `schools` collection (projected fields only)
- **Architecture**: Vertical Slice in `src/Api/Endpoints/GetSchoolsMap/`
- **Current State**: Not yet implemented

### Why a Separate Endpoint?
- **Performance**: Map view requires minimal data (5 fields vs. full SchoolRate with reviews)
- **Payload Size**: All schools with minimal data = ~20KB per 100 schools vs. ~500KB for full SchoolRates
- **Rendering Speed**: Frontend can render markers immediately without parsing large objects
- **Simplicity**: Single request returns all schools - no pagination complexity

---

## 2. Acceptance Criteria (Gherkin)

```gherkin
Feature: Get School Map Data
  As an anonymous user
  I want to retrieve lightweight school data for map display
  So that I can see all schools plotted on an interactive map

  Background:
    Given the following schools exist in the database:
      | SchoolId | Name                  | City      | Province | Latitude | Longitude |
      | school-1 | Maple Leaf Elementary | Toronto   | ON       | 43.6532  | -79.3832  |
      | school-2 | Oak Grove High        | Toronto   | ON       | 43.6500  | -79.3900  |
      | school-3 | Pine Academy          | Ottawa    | ON       | 45.4215  | -75.6972  |
      | school-4 | Cedar Charter         | Montreal  | QC       | 45.5017  | -73.5673  |
      | school-5 | Birch International   | Vancouver | BC       | 49.2827  | -123.1207 |

  Scenario: Retrieve all schools for map display
    When I request GET "/api/v1/schools/map"
    Then the response status should be 200
    And the response should contain 5 schools
    And each school should have exactly these properties:
      | Property    | Type   | Required |
      | id          | String | Yes      |
      | name        | String | Yes      |
      | city        | String | Yes      |
      | province    | String | Yes      |
      | geolocation | Object | Yes      |
    And each geolocation should contain:
      | Property  | Type   | Required |
      | latitude  | Number | Yes      |
      | longitude | Number | Yes      |
    And each school should NOT have properties: score, reviews, address, imageUrl

  Scenario: Schools are ordered by name ascending
    When I request GET "/api/v1/schools/map"
    Then the response status should be 200
    And the schools should be ordered by name ascending

  Scenario: Handle empty result set
    Given there are no schools in the database
    When I request GET "/api/v1/schools/map"
    Then the response status should be 200
    And the response should contain 0 schools
    And the schools array should be empty

  Scenario: Verify minimal payload size
    Given the database contains 100 schools
    When I request GET "/api/v1/schools/map"
    Then the response status should be 200
    And the response payload size should be less than 30KB
    And each school object should contain only 5 fields

  Scenario: Performance - Response time within acceptable range
    Given the database contains 1000 schools
    When I request GET "/api/v1/schools/map"
    Then the response status should be 200
    And the response time should be less than 200ms

  Scenario: Geolocation coordinates are valid
    When I request GET "/api/v1/schools/map"
    Then the response status should be 200
    And each school's latitude should be between -90 and 90
    And each school's longitude should be between -180 and 180
```

---

## 3. OpenAPI Schema & Arazzo Flow

### 3.1 Define OpenAPI Schema

**File:** `src/Api/openapi/schools-map.yaml`

```yaml
openapi: 3.0.3
info:
  title: RateYourSchool API - Map View
  version: 1.0.0
  description: Lightweight endpoints for map-based school visualization

paths:
  /api/v1/schools/map:
    get:
      summary: Get lightweight school data for map display
      description: |
        Retrieves all schools with minimal data optimized for map marker rendering.
        Returns only essential fields to minimize payload size and maximize frontend
        rendering performance. No pagination - returns all schools in a single response.
        
        **CQRS Note:** This endpoint reads from the schools collection using MongoDB
        projection to fetch only map-relevant fields (id, name, city, province, coordinates).
        
        **Performance:** Typical response with 100 schools is ~20KB vs ~500KB
        for the full SchoolRates endpoint.
      operationId: getSchoolsMap
      tags:
        - Schools
        - Map
      responses:
        '200':
          description: Successfully retrieved school map data
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GetSchoolsMapResponse'
              examples:
                success:
                  summary: Successful response with map data
                  value:
                    schools:
                      - id: "school-1"
                        name: "Maple Leaf Elementary"
                        city: "Toronto"
                        province: "ON"
                        geolocation:
                          latitude: 43.6532
                          longitude: -79.3832
                      - id: "school-2"
                        name: "Oak Grove High"
                        city: "Toronto"
                        province: "ON"
                        geolocation:
                          latitude: 43.6500
                          longitude: -79.3900
        '400':
          description: Bad request (invalid parameters)
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'
        '500':
          description: Internal server error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Error'

components:
  schemas:
    GetSchoolsMapResponse:
      type: object
      required:
        - schools
      properties:
        schools:
          type: array
          description: Array of lightweight school objects optimized for map display
          items:
            $ref: '#/components/schemas/SchoolMapViewModel'
    
    SchoolMapViewModel:
      type: object
      required:
        - id
        - name
        - city
        - province
        - geolocation
      properties:
        id:
          type: string
          description: Unique school identifier
          example: "school-1"
        name:
          type: string
          description: School name
          example: "Maple Leaf Elementary"
        city:
          type: string
          description: City where the school is located
          example: "Toronto"
        province:
          type: string
          description: Province/state code (2-letter abbreviation)
          example: "ON"
          pattern: "^[A-Z]{2}$"
        geolocation:
          $ref: '#/components/schemas/Geolocation'
    
    Geolocation:
      type: object
      required:
        - latitude
        - longitude
      properties:
        latitude:
          type: number
          format: double
          description: Latitude coordinate (WGS84)
          minimum: -90
          maximum: 90
          example: 43.6532
        longitude:
          type: number
          format: double
          description: Longitude coordinate (WGS84)
          minimum: -180
          maximum: 180
          example: -79.3832
    
    Error:
      type: object
      properties:
        message:
          type: string
          description: Error message
        code:
          type: string
          description: Error code
```

### 3.2 Arazzo Workflow Definition

**File:** `src/Api/arazzo/map-exploration-workflow.yaml`

```yaml
arazzo: 1.0.0
info:
  title: Map Exploration Workflow
  version: 1.0.0
  description: |
    Workflow for exploring schools via interactive map interface.
    User loads map → sees all schools as markers → clicks marker → views details.

sourceDescriptions:
  - name: rateyourschool-api
    type: openapi
    url: ./openapi/schools-map.yaml

workflows:
  - workflowId: explore-schools-on-map
    summary: Explore schools using map interface
    description: |
      User journey: Load map view → See school markers → Select school → View full details
    
    steps:
      - stepId: loadMapData
        description: Load all schools for map display
        operationId: getSchoolsMap
        successCriteria:
          - condition: $statusCode == 200
        outputs:
          schoolMarkers: $response.body.schools
      
      - stepId: selectSchool
        description: User selects a school marker on the map
        operationPath: $sourceDescriptions.rateyourschool-api#/paths/~1api~1v1~1schools~1{id}
        parameters:
          - name: id
            in: path
            value: $steps.loadMapData.outputs.schoolMarkers[0].id
        successCriteria:
          - condition: $statusCode == 200
        outputs:
          schoolDetails: $response.body

    outputs:
      initialSchools: $steps.loadMapData.outputs.schoolMarkers
      selectedSchool: $steps.selectSchool.outputs.schoolDetails
```

---

## 4. Backend Implementation

### 4.1 Project Structure

```
src/Api/Endpoints/GetSchoolsMap/
├── Endpoint.cs                    # Minimal API endpoint mapping
├── Handler.cs                     # Business logic handler
├── ReadonlyRepository.cs          # MongoDB repository implementation
├── Request.cs                     # Input DTO
├── Response.cs                    # Output DTO
├── SchoolMapViewModel.cs          # View model for map data
└── ServiceCollectionExtensions.cs # DI registration
```

### 4.2 Request DTO

**File:** `src/Api/Endpoints/GetSchoolsMap/Request.cs` (NEW)

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// Request for retrieving lightweight school map data.
/// No pagination - returns all schools.
/// </summary>
internal sealed record Request
{
    // No parameters required - returns all schools
}
```

### 4.3 Response DTO

**File:** `src/Api/Endpoints/GetSchoolsMap/Response.cs` (NEW)

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// Response containing lightweight school data for map display.
/// </summary>
internal sealed record Response
{
    /// <summary>
    /// Collection of schools with minimal data for map markers.
    /// </summary>
    public required IEnumerable<SchoolMapViewModel> Schools { get; init; }
}
```

### 4.4 View Model

**File:** `src/Api/Endpoints/GetSchoolsMap/SchoolMapViewModel.cs` (NEW)

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// Lightweight school data optimized for map marker display.
/// Contains only essential fields to minimize payload size.
/// </summary>
internal sealed record SchoolMapViewModel
{
    /// <summary>
    /// Unique school identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// School name for marker label.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// City where school is located.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// Province/state code (2-letter abbreviation).
    /// </summary>
    public required string Province { get; init; }

    /// <summary>
    /// Geographic coordinates for map marker placement.
    /// </summary>
    public required Geolocation Geolocation { get; init; }
}

/// <summary>
/// Geographic coordinates (WGS84).
/// </summary>
internal sealed record Geolocation
{
    /// <summary>
    /// Latitude coordinate (-90 to 90).
    /// </summary>
    public required decimal Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate (-180 to 180).
    /// </summary>
    public required decimal Longitude { get; init; }
}
```

### 4.5 Repository Interface

**File:** `src/Domain/Repositories/IReadOnlyRepository.cs` (UPDATE - add new method)

Add this method signature to the existing interface:

```csharp
/// <summary>
/// Retrieves all entities of type T without pagination.
/// </summary>
/// <typeparam name="T">The entity type to retrieve.</typeparam>
Task<IEnumerable<T>> GetAsync<T>(CancellationToken cancellationToken);
```

### 4.6 MongoDB Repository Implementation

**File:** `src/Api/Endpoints/GetSchoolsMap/ReadonlyRepository.cs` (NEW)

```csharp
using MongoDB.Driver;
using RateYourSchool.Domain.Entities;
using RateYourSchool.Domain.Repositories;

namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// MongoDB repository for retrieving lightweight school map data.
/// Reads from schools collection using projection.
/// </summary>
internal sealed class MongoDbReadOnlyRepository : IReadOnlyRepository
{
    private readonly IMongoCollection<School> _collection;
    private readonly ILogger<MongoDbReadOnlyRepository> _logger;

    public MongoDbReadOnlyRepository(
        IMongoDatabase database, 
        ILogger<MongoDbReadOnlyRepository> logger)
    {
        _collection = database.GetCollection<School>("schools");
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all entities of type T without pagination.
    /// Uses MongoDB projection to return only the fields needed by the view model.
    /// </summary>
    public async Task<IEnumerable<T>> GetAsync<T>(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving all entities of type {Type}", typeof(T).Name);

            // Use projection to query SchoolMapViewModel directly from School collection
            var results = await _collection
                .Find(FilterDefinition<School>.Empty)
                .Project(s => new SchoolMapViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    City = s.City,
                    Province = s.Province,
                    Geolocation = new Geolocation
                    {
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    }
                })
                .Sort(Builders<SchoolMapViewModel>.Sort.Ascending(vm => vm.Name))
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} entities of type {Type}",
                results.Count,
                typeof(T).Name);

            return results.Cast<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entities of type {Type}", typeof(T).Name);
            throw;
        }
    }
}
```

### 4.7 Handler Implementation

**File:** `src/Api/Endpoints/GetSchoolsMap/Handler.cs` (NEW)

```csharp
using RateYourSchool.Api.Endpoints;

namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// Handles retrieval of lightweight school map data.
/// Returns all schools in a single response.
/// </summary>
internal sealed class Handler : IHandler<Request, Response>
{
    private readonly IReadOnlyRepository _repository;
    private readonly ILogger<Handler> _logger;

    public Handler(
        IReadOnlyRepository repository,
        ILogger<Handler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Processes request and returns all school map data.
    /// </summary>
    public async Task<Response> HandleAsync(
        Request request, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Retrieving all schools for map view");

        // Query SchoolMapViewModel directly from database using projection
        var viewModels = await _repository.GetAsync<SchoolMapViewModel>(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} schools for map view",
            viewModels.Count());

        return new Response
        {
            Schools = viewModels
        };
    }
}
```

### 4.8 Endpoint Mapping

**File:** `src/Api/Endpoints/GetSchoolsMap/Endpoint.cs` (NEW)

```csharp
using Microsoft.AspNetCore.Http.HttpResults;

namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// Endpoint for retrieving lightweight school data for map display.
/// </summary>
internal static class GetSchoolsMapEndpoint
{
    /// <summary>
    /// Maps the GetSchoolsMap endpoint to the route builder.
    /// </summary>
    public static IEndpointRouteBuilder MapGetSchoolsMapEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/v1/schools/map", async (
            CancellationToken cancellationToken,
            IHandler<Request, Response> handler) =>
        {
            var request = new Request();
            var response = await handler.HandleAsync(request, cancellationToken);
            
            return Results.Ok(response);
        })
        .WithName("GetSchoolsMap")
        .WithTags("Schools", "Map")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get lightweight school data for map display";
            operation.Description = "Retrieves all schools optimized for map marker rendering (minimal fields only)";
            return operation;
        })
        .Produces<Response>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
```

### 4.9 Service Registration

**File:** `src/Api/Endpoints/GetSchoolsMap/ServiceCollectionExtensions.cs` (NEW)

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolsMap;

/// <summary>
/// Extension methods for registering GetSchoolsMap endpoint services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required for GetSchoolsMap endpoint.
    /// </summary>
    public static IServiceCollection AddGetSchoolsMapEndpoint(
        this IServiceCollection services)
    {
        services.AddScoped<IHandler<Request, Response>, Handler>();
        services.AddScoped<IReadOnlyRepository, MongoDbReadOnlyRepository>();
        
        return services;
    }
}
```

### 4.10 Register in Program.cs

**File:** `src/Api/Program.cs` (UPDATE)

Add these lines in the appropriate sections:

```csharp
// Service registration section
builder.Services.AddGetSchoolsMapEndpoint();

// Endpoint mapping section
app.MapGetSchoolsMapEndpoint();
```

### 4.11 MongoDB Setup

**Collection Used:** `schools`

This endpoint reads directly from the `schools` collection using MongoDB projection to fetch only the required fields. No separate collection is needed.

**Indexes:**

Ensure the following indexes exist on the `schools` collection:

```javascript
// Index 1: Sorting by name (if not already exists)
db.schools.createIndex({ name: 1 });

// Index 2: Location filtering (city + province)
db.schools.createIndex({ city: 1, province: 1 });

// Index 3: Geospatial queries (future enhancement for radius search)
db.schools.createIndex({ 
  location: "2dsphere" 
});
// Note: This requires adding a GeoJSON field to School: { type: "Point", coordinates: [longitude, latitude] }
```

**Query Optimization:**

The repository uses MongoDB projection to fetch only required fields:
- `id`
- `name`
- `city`
- `province`
- `latitude`
- `longitude`

This minimizes network transfer and memory usage while maintaining optimal performance.

---

## 5. Unit Tests Implementation

### 5.1 Handler Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolsMap/HandlerTests.cs` (NEW)

```csharp
using FluentAssertions;
using Moq;
using RateYourSchool.Api.Endpoints;
using RateYourSchool.Api.Endpoints.GetSchoolsMap;
using RateYourSchool.Domain.Entities;
using RateYourSchool.Domain.Repositories;
using Xunit;

namespace RateYourSchool.Tests.Unit.Endpoints.GetSchoolsMap;

public class HandlerTests
{
    private readonly Mock<IReadOnlyRepository> _mockRepository;
    private readonly Mock<ILogger<Handler>> _mockLogger;
    private readonly Handler _handler;

    public HandlerTests()
    {
        _mockRepository = new Mock<IReadOnlyRepository>();
        _mockLogger = new Mock<ILogger<Handler>>();
        _handler = new Handler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldReturnAllSchools()
    {
        // Arrange
        var request = new Request();
        var expectedData = CreateTestMapData();
        
        _mockRepository
            .Setup(r => r.GetAsync<SchoolMapViewModel>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMapData());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Schools.Should().HaveCount(2);
        result.Schools.First().Should().Match<SchoolMapViewModel>(s =>
            s.Id == "school-1" &&
            s.Name == "Test School 1" &&
            s.City == "Toronto" &&
            s.Province == "ON" &&
            s.Geolocation.Latitude == 43.6532m &&
            s.Geolocation.Longitude == -79.3832m);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        var request = new Request();
        _mockRepository
            .Setup(r => r.GetAsync<SchoolMapViewModel>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SchoolMapViewModel>());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Schools.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _handler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ShouldPropagateException()
    {
        // Arrange
        var request = new Request();
        _mockRepository
            .Setup(r => r.GetAsync<SchoolMapViewModel>(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }

    private static IEnumerable<SchoolMapViewModel> CreateTestMapData()
    {
        return new[]
        {
            new SchoolMapViewModel
            {
                Id = "school-1",
                Name = "Test School 1",
                City = "Toronto",
                Province = "ON",
                Geolocation = new Geolocation
                {
                    Latitude = 43.6532m,
                    Longitude = -79.3832m
                }
            },
            new SchoolMapViewModel
            {
                Id = "school-2",
                Name = "Test School 2",
                City = "Vancouver",
                Province = "BC",
                Geolocation = new Geolocation
                {
                    Latitude = 49.2827m,
                    Longitude = -123.1207m
                }
            }
        };
    }
}
```

---

## 6. Integration Tests Implementation (ReqnRoll/SpecFlow)

### 6.1 Feature File

**File:** `src/Tests/RateYourSchool.Tests.Integration/Features/GetSchoolsMap.feature` (NEW)

```gherkin
Feature: Get School Map Data
  As an anonymous user
  I want to retrieve lightweight school data for map display
  So that I can see all schools plotted on an interactive map

  Background:
    Given the following schools exist for map view:
      | SchoolId | Name                  | City      | Province | Latitude | Longitude |
      | school-1 | Maple Leaf Elementary | Toronto   | ON       | 43.6532  | -79.3832  |
      | school-2 | Oak Grove High        | Toronto   | ON       | 43.6500  | -79.3900  |
      | school-3 | Pine Academy          | Ottawa    | ON       | 45.4215  | -75.6972  |
      | school-4 | Cedar Charter         | Montreal  | QC       | 45.5017  | -73.5673  |
      | school-5 | Birch International   | Vancouver | BC       | 49.2827  | -123.1207 |

  Scenario: Retrieve first page with default page size
    When I send a GET request to "/api/v1/schools/map"
    Then the response status code should be 200
    And the response should contain 5 schools
    And each school should have the following properties:
      | Property    | Type   |
      | id          | string |
      | name        | string |
      | city        | string |
      | province    | string |
      | geolocation | object |



  Scenario: Verify minimal payload with only required fields
    When I send a GET request to "/api/v1/schools/map"
    Then the response status code should be 200
    And each school should have exactly 5 properties
    And each school should NOT have properties: score, reviews, address, imageUrl

  Scenario: Verify geolocation coordinates are valid
    When I send a GET request to "/api/v1/schools/map"
    Then the response status code should be 200
    And each school's latitude should be between -90 and 90
    And each school's longitude should be between -180 and 180
```

### 6.2 Step Definitions

**File:** `src/Tests/RateYourSchool.Tests.Integration/StepDefinitions/GetSchoolsMapSteps.cs` (NEW)

```csharp
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using RateYourSchool.Api.Endpoints.GetSchoolsMap;
using RateYourSchool.Tests.Integration.Infrastructure;

namespace RateYourSchool.Tests.Integration.StepDefinitions;

[Binding]
public class GetSchoolsMapSteps : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly MongoDbFixture _mongoFixture;
    private HttpResponseMessage? _response;
    private GetSchoolsMapResponse? _responseData;

    public GetSchoolsMapSteps(TestWebApplicationFactory factory, MongoDbFixture mongoFixture)
    {
        _client = factory.CreateClient();
        _mongoFixture = mongoFixture;
    }

    [Given(@"the following schools exist for map view:")]
    public async Task GivenSchoolsExistForMapView(Table table)
    {
        var collection = _mongoFixture.Database.GetCollection<School>("schools");
        
        var schools = table.Rows.Select(row => new School
        {
            Id = row["SchoolId"],
            Name = row["Name"],
            City = row["City"],
            Province = row["Province"],
            Latitude = decimal.Parse(row["Latitude"]),
            Longitude = decimal.Parse(row["Longitude"])
            // Add other required School properties with default values
        }).ToList();

        await collection.InsertManyAsync(schools);
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendGetRequest(string url)
    {
        _response = await _client.GetAsync(url);
        
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            _responseData = JsonSerializer.Deserialize<GetSchoolsMapResponse>(content);
        }
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenResponseStatusShouldBe(int expectedStatusCode)
    {
        _response.Should().NotBeNull();
        ((int)_response!.StatusCode).Should().Be(expectedStatusCode);
    }

    [Then(@"the response should contain (.*) schools")]
    public void ThenResponseShouldContainSchools(int expectedCount)
    {
        _responseData.Should().NotBeNull();
        _responseData!.Schools.Should().HaveCount(expectedCount);
    }

    [Then(@"each school should have the following properties:")]
    public void ThenEachSchoolShouldHaveProperties(Table table)
    {
        _responseData.Should().NotBeNull();
        _responseData!.Schools.Should().NotBeEmpty();

        foreach (var school in _responseData.Schools)
        {
            foreach (var row in table.Rows)
            {
                var propertyName = row["Property"];
                var propertyType = row["Type"];

                switch (propertyName.ToLowerInvariant())
                {
                    case "id":
                        school.Id.Should().NotBeNullOrEmpty();
                        break;
                    case "name":
                        school.Name.Should().NotBeNullOrEmpty();
                        break;
                    case "city":
                        school.City.Should().NotBeNullOrEmpty();
                        break;
                    case "province":
                        school.Province.Should().NotBeNullOrEmpty();
                        school.Province.Should().MatchRegex("^[A-Z]{2}$");
                        break;
                    case "geolocation":
                        school.Geolocation.Should().NotBeNull();
                        break;
                }
            }
        }
    }

    [Then(@"each school should have exactly (.*) properties")]
    public void ThenEachSchoolShouldHaveExactlyProperties(int expectedCount)
    {
        _responseData.Should().NotBeNull();
        _responseData!.Schools.Should().NotBeEmpty();

        foreach (var school in _responseData.Schools)
        {
            var json = JsonSerializer.Serialize(school);
            var doc = JsonDocument.Parse(json);
            doc.RootElement.EnumerateObject().Count().Should().Be(expectedCount);
        }
    }

    [Then(@"each school should NOT have properties: (.*)")]
    public void ThenEachSchoolShouldNotHaveProperties(string properties)
    {
        _responseData.Should().NotBeNull();
        var forbiddenProps = properties.Split(',').Select(p => p.Trim()).ToList();

        foreach (var school in _responseData!.Schools)
        {
            var json = JsonSerializer.Serialize(school);
            var doc = JsonDocument.Parse(json);
            var actualProps = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenProps)
            {
                actualProps.Should().NotContain(forbidden, 
                    $"school should not have property '{forbidden}'");
            }
        }
    }

    [Then(@"each school's latitude should be between (.*) and (.*)")]
    public void ThenLatitudeShouldBeInRange(decimal min, decimal max)
    {
        _responseData.Should().NotBeNull();
        
        foreach (var school in _responseData!.Schools)
        {
            school.Geolocation.Latitude.Should().BeInRange(min, max);
        }
    }

    [Then(@"each school's longitude should be between (.*) and (.*)")]
    public void ThenLongitudeShouldBeInRange(decimal min, decimal max)
    {
        _responseData.Should().NotBeNull();
        
        foreach (var school in _responseData!.Schools)
        {
            school.Geolocation.Longitude.Should().BeInRange(min, max);
        }
    }

    private class GetSchoolsMapResponse
    {
        public List<SchoolMapDto> Schools { get; set; } = new();
    }

    private class SchoolMapDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public GeolocationDto Geolocation { get; set; } = new();
    }

    private class GeolocationDto
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
```

### 6.3 Test Project Configuration

**File:** `src/Tests/RateYourSchool.Tests.Integration/RateYourSchool.Tests.Integration.csproj` (UPDATE)

Ensure these packages are included:

```xml
<ItemGroup>
    <PackageReference Include="Reqnroll" Version="2.0.0" />
    <PackageReference Include="Reqnroll.xUnit" Version="2.0.0" />
    <PackageReference Include="Reqnroll.Tools.MsBuild" Version="2.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Testcontainers.MongoDb" Version="3.7.0" />
</ItemGroup>
```

### 6.4 Run Integration Tests

```bash
cd src/Tests/RateYourSchool.Tests.Integration
dotnet test --logger "reqnroll;LogFilePath=TestResults/report.html"
```

---

## 7. Frontend Implementation

### 7.1 TypeScript Types

**File:** `frontend/src/types/schoolMap.ts` (NEW)

```typescript
/**
 * Lightweight school data for map display.
 */
export interface SchoolMapViewModel {
  id: string;
  name: string;
  city: string;
  province: string;
  geolocation: Geolocation;
}

/**
 * Geographic coordinates (WGS84).
 */
export interface Geolocation {
  latitude: number;
  longitude: number;
}

/**
 * API response for school map data.
 */
export interface GetSchoolsMapResponse {
  schools: SchoolMapViewModel[];
}


```

### 7.2 API Service

**File:** `frontend/src/services/schoolMapService.ts` (NEW)

```typescript
import apiClient from './apiClient';
import type { GetSchoolsMapResponse, GetSchoolsMapParams } from '../types/schoolMap';

/**
 * Service for retrieving lightweight school data for map display.
 */
export const schoolMapService = {
  /**
   * Fetches all school map data from the API.
   * @returns Promise resolving to school map data
   */
  async getSchoolsMapData(): Promise<GetSchoolsMapResponse> {
    const response = await apiClient.get<GetSchoolsMapResponse>('/api/v1/schools/map');
    return response.data;
  },
};
```

### 7.3 Custom Hook

**File:** `frontend/src/hooks/useSchoolMap.ts` (NEW)

```typescript
import { useState, useEffect } from 'react';
import { schoolMapService } from '../services/schoolMapService';
import type { SchoolMapViewModel } from '../types/schoolMap';

/**
 * Result type for useSchoolMap hook.
 */
export interface UseSchoolMapResult {
  schools: SchoolMapViewModel[];
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

/**
 * Custom hook for fetching all school map data.
 * @returns School map data, loading state, and error information
 */
export const useSchoolMap = (): UseSchoolMapResult => {
  const [schools, setSchools] = useState<SchoolMapViewModel[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSchools = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await schoolMapService.getSchoolsMapData();
      setSchools(response.schools);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch school map data';
      setError(errorMessage);
      console.error('Error fetching school map data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSchools();
  }, []);

  return {
    schools,
    loading,
    error,
    refetch: fetchSchools,
  };
};
```

### 7.4 Map Component

**File:** `frontend/src/components/SchoolMap.tsx` (NEW)

```typescript
import React, { useEffect, useRef } from 'react';
import { useSchoolMap } from '../hooks/useSchoolMap';
import type { SchoolMapViewModel } from '../types/schoolMap';
import './SchoolMap.css';

/**
 * Props for SchoolMap component.
 */
export interface SchoolMapProps {
  onSchoolClick?: (schoolId: string) => void;
}

/**
 * Interactive map displaying all school locations.
 * Uses Leaflet or Google Maps for rendering.
 */
export const SchoolMap: React.FC<SchoolMapProps> = ({ onSchoolClick }) => {
  const { schools, loading, error } = useSchoolMap();
  const mapRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (mapRef.current && schools.length > 0) {
      // Initialize map (using Leaflet example)
      // Note: This is pseudocode - actual implementation depends on map library
      const map = initializeMap(mapRef.current);
      
      schools.forEach(school => {
        addMarker(map, school, onSchoolClick);
      });
    }
  }, [schools, onSchoolClick]);

  if (loading) {
    return <div className="school-map-loading">Loading map...</div>;
  }

  if (error) {
    return <div className="school-map-error">Error: {error}</div>;
  }

  return (
    <div className="school-map-container">
      <div ref={mapRef} className="school-map" />
      <div className="school-map-info">
        Showing {schools.length} schools
      </div>
    </div>
  );
};

// Helper functions (pseudocode - implement with chosen map library)
function initializeMap(container: HTMLElement): any {
  // Initialize Leaflet/Google Maps
  // Return map instance
}

function addMarker(
  map: any, 
  school: SchoolMapViewModel, 
  onClick?: (schoolId: string) => void
): void {
  // Add marker at school.geolocation coordinates
  // Attach click handler to call onClick(school.id)
  // Show tooltip with school.name
}
```

### 7.5 Usage Example

**File:** `frontend/src/pages/MapView.tsx` (NEW)

```typescript
import React from 'react';
import { useNavigate } from 'react-router-dom';
import { SchoolMap } from '../components/SchoolMap';

/**
 * Page component for map view of schools.
 */
export const MapView: React.FC = () => {
  const navigate = useNavigate();

  const handleSchoolClick = (schoolId: string) => {
    // Navigate to school details page
    navigate(`/schools/${schoolId}`);
  };

  return (
    <div className="map-view-page">
      <h1>Find Schools Near You</h1>
      <SchoolMap onSchoolClick={handleSchoolClick} />
    </div>
  );
};
```

---

## 8. Implementation Checklist

### Backend
- [ ] Update `src/Domain/Repositories/IReadOnlyRepository.cs` with new method signature
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/Request.cs`
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/Response.cs`
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/SchoolMapViewModel.cs`
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/ReadonlyRepository.cs`
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/Handler.cs`
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/Endpoint.cs`
- [ ] Create `src/Api/Endpoints/GetSchoolsMap/ServiceCollectionExtensions.cs`
- [ ] Update `src/Api/Program.cs` to register endpoint
- [ ] Run and verify endpoint works locally

### API Documentation
- [ ] Create `src/Api/openapi/schools-map.yaml`
- [ ] Create `src/Api/arazzo/map-exploration-workflow.yaml`
- [ ] Update main OpenAPI spec to reference map endpoint

### Unit Tests
- [ ] Create `HandlerTests.cs` with 80%+ coverage
- [ ] Test pagination validation (page < 1, page size clamping)
- [ ] Test empty results handling
- [ ] Test null/exception handling
- [ ] Measure code coverage: `dotnet test --collect:"XPlat Code Coverage"`

### Integration Tests
- [ ] Create `GetSchoolsMap.feature` file
- [ ] Implement ReqnRoll/SpecFlow step definitions
- [ ] Test with Testcontainers MongoDB
- [ ] Verify all Gherkin scenarios pass
- [ ] Generate test report

### Frontend
- [ ] Create TypeScript types (`schoolMap.ts`)
- [ ] Create API service (`schoolMapService.ts`)
- [ ] Create custom hook (`useSchoolMap.ts`)
- [ ] Create SchoolMap component
- [ ] Integrate map library (Leaflet/Google Maps/Mapbox)
- [ ] Add click handlers for school markers
- [ ] Style map component
- [ ] Add loading and error states

### MongoDB Setup
- [ ] Verify indexes on `schools` collection:
  - `{ name: 1 }` for sorting
  - `{ "city": 1, "province": 1 }` for location filtering
  - `{ "location": "2dsphere" }` for geospatial queries (future)
- [ ] Verify School entity has geolocation fields (latitude, longitude)
- [ ] Seed test data for development

### E2E Tests
- [ ] Create E2E test suite using Playwright
- [ ] Test map component renders with markers
- [ ] Test clicking school marker navigates to details page
- [ ] Test map displays all schools from API
- [ ] Test loading and error states
- [ ] Test map interactions (pan, zoom, marker tooltips)

### Performance
- [ ] Verify p95 response time < 150ms with 100 schools
- [ ] Verify p95 response time < 200ms with 1000 schools (BR-028)
- [ ] Verify payload size < 30KB for 100 schools
- [ ] Verify payload size < 300KB for 1000 schools
- [ ] Load test with concurrent users
- [ ] Optimize MongoDB queries if needed (add projections, tune indexes)
- [ ] Monitor memory usage during large result sets

---

## 9. Notes for Subagent Implementation

### Implementation Order

**Phase 1: API Documentation**
1. Create OpenAPI schema
2. Define Arazzo workflow
3. Validate schemas

**Phase 2: Backend Foundation**
1. Update repository interface
2. Create DTOs (Request, Response, ViewModel)
3. Implement handler with validation logic
4. Implement repository with MongoDB queries
5. Create endpoint mapping
6. Register services in `Program.cs`

**Phase 3: Testing**
1. Write unit tests (handler tests first)
2. Achieve 80%+ code coverage
3. Create Gherkin feature file
4. Implement step definitions
5. Run integration tests with Testcontainers

**Phase 4: Frontend**
1. Create TypeScript types
2. Implement API service
3. Create custom hook
4. Build map component
5. Integrate map library
6. Test end-to-end

### Key Considerations

**Performance Optimization:**
- Keep payload minimal (5 fields only)
- Use MongoDB projections to fetch only required fields
- Create appropriate indexes
- Monitor query performance

**Data Synchronization:**
- No separate collection synchronization needed
- Reads directly from schools collection with projection
- Always reflects current state of School entities

**Error Handling:**
- Invalid coordinates should be handled gracefully
- Missing geolocation data should exclude school from map
- Log errors without exposing internals

**Testing Strategy:**
- Unit tests: Handler logic, validation, error handling
- Integration tests: Full request/response cycle
- E2E tests: Frontend map rendering and interaction
- Performance tests: Large datasets, response times

**Security:**
- No sensitive data in map view
- Anonymous access allowed
- Rate limiting recommended (future)

### Dependencies

**Backend:**
- MongoDB.Driver
- ASP.NET Core
- Microsoft.Extensions.Logging

**Testing:**
- xUnit
- ReqnRoll/SpecFlow
- FluentAssertions
- Moq
- Testcontainers.MongoDb

**Frontend:**
- React 18+
- TypeScript 5+
- Axios
- Map library (Leaflet/Google Maps/Mapbox)

### MongoDB Collection Schema

```javascript
// schools collection (existing)
// The endpoint reads from this collection using projection
{
  _id: "school-1",
  name: "Maple Leaf Elementary",
  city: "Toronto",
  province: "ON",
  latitude: 43.6532,
  longitude: -79.3832,
  // ... other School properties
}

// Indexes
db.schools.createIndex({ name: 1 });
db.schools.createIndex({ city: 1, province: 1 });
db.schools.createIndex({ location: "2dsphere" }); // For future geo queries
```

### Future Enhancements

- Geographic bounding box filtering (`bounds` parameter)
- Clustering for high-density areas
- Filter by school type, rating, etc.
- Real-time updates via WebSockets
- Caching layer for frequently requested data

---

**Document Version:** 1.1  
**Last Updated:** 2026-03-16  
**Author:** Product Owner
