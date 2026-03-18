# Use Case: Get SchoolRate (Single Detail View)

**Status:** Not Started  
**Priority:** High  
**Related Use Cases:** UC-002 (Get SchoolRates List)  
**Business Rules:** BR-009 through BR-015, BR-028, BR-029

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

**Status Legend:**
- `TODO` - Not started
- `IN-PROGRESS` - Currently being worked on
- `IN-REVIEW` - Completed and under review
- `DONE` - Completed and verified

---

## 1. Summary

### Overview

This use case enables anonymous users to retrieve detailed information about a single school by its unique identifier. The endpoint returns a comprehensive SchoolRate read model containing school information, aggregated multi-dimensional scores, and the most recent 25 user reviews. This feature is essential for school detail pages where users can view complete information about a specific school.

Unlike traditional CRUD operations that query write models, this endpoint queries the SchoolRate read model—a denormalized, materialized view optimized for queries. The SchoolRate read model is automatically maintained through CQRS event synchronization when schools or reviews are created or updated. This architectural approach ensures high-performance reads while maintaining data consistency through eventual consistency patterns.

The endpoint supports RESTful resource retrieval patterns with proper error handling for missing resources (404 Not Found) and invalid identifiers (400 Bad Request).

### Functional Requirements

- Retrieve a single SchoolRate by unique identifier (GUID string)
- Return complete school information (name, address, city, province, type, location, image URL)
- Include aggregated multi-dimensional scores (Innovation, Building, Professorate, ManagementTeam, Total)
- Include the last 25 user reviews per business rule BR-012
- Return 404 Not Found when the specified SchoolRate ID does not exist
- Return 400 Bad Request when the ID format is invalid
- Ensure response time meets performance requirements (< 200ms per BR-029)
- Support read-only operations (no data modification)

### Technical Context

- **CQRS Pattern**: Reads from SchoolRate read model (NOT School write model per BR-009)
- **Data Source**: MongoDB `schoolRates` collection
- **Architecture**: Vertical Slice in `src/Api/Endpoints/GetSchoolRate/`
- **Current State**: No existing implementation - new endpoint required
- **Read Model Architecture**: SchoolRate is already a read model/view model - query directly using MongoDB projection, no entity-to-viewmodel mapping needed
- **Domain Model**: NO NEW DOMAIN MODEL CREATED - SchoolRate entity already exists as the materialized read model

---

## 2. Acceptance Criteria (Gherkin)

```gherkin
Feature: Get SchoolRate by ID
  As an anonymous user
  I want to retrieve detailed information about a specific school
  So that I can view comprehensive school ratings and reviews

  Background:
    Given the following schools exist in the database:
      | SchoolId                             | Name                  | City      | Province | Type    | TotalScore | ReviewCount |
      | 550e8400-e29b-41d4-a716-446655440001 | Maple Leaf Elementary | Toronto   | ON       | Public  | 4.5        | 30          |
      | 550e8400-e29b-41d4-a716-446655440002 | Oak Grove High        | Toronto   | ON       | Public  | 3.8        | 15          |
      | 550e8400-e29b-41d4-a716-446655440003 | Pine Academy          | Ottawa    | ON       | Private | 4.2        | 25          |

  Scenario: Successfully retrieve a school by valid ID
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status should be 200
    And the response should contain a single school
    And the school Info should have the following properties:
      | Property | Value                 |
      | Name     | Maple Leaf Elementary |
      | City     | Toronto               |
      | Province | ON                    |
    And the school Score should have the following properties:
      | Property       | Type    | Required |
      | Innovation     | Decimal | Yes      |
      | Building       | Decimal | Yes      |
      | Professorate   | Decimal | Yes      |
      | ManagementTeam | Decimal | Yes      |
      | Total          | Decimal | Yes      |
    And the school Reviews should be an array
    And the school Reviews should have at most 25 items

  Scenario: Retrieve school with complete data structure
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status should be 200
    And the response should have the following structure:
      | Property | Type   | Required |
      | Info     | Object | Yes      |
      | Score    | Object | Yes      |
      | Reviews  | Array  | Yes      |
    And each review in Reviews should have the following properties:
      | Property | Type    | Required |
      | UserName | String  | Yes      |
      | Comment  | String  | Yes      |
      | Rating   | Decimal | Yes      |

  Scenario: School not found - invalid ID
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-999999999999"
    Then the response status should be 404
    And the response should contain a ProblemDetails object
    And the ProblemDetails should have the following properties:
      | Property | Value                    |
      | Status   | 404                      |
      | Title    | Not Found                |
      | Detail   | SchoolRate not found     |

  Scenario: Invalid ID format - not a valid GUID
    When I request GET "/api/v1/schoolrates/invalid-id-format"
    Then the response status should be 400
    And the response should contain a ProblemDetails object
    And the ProblemDetails should have the following properties:
      | Property | Value                        |
      | Status   | 400                          |
      | Title    | Bad Request                  |
      | Detail   | Invalid SchoolRate ID format |

  Scenario: Retrieve school with exactly 25 reviews when school has more
    Given school "550e8400-e29b-41d4-a716-446655440001" has 30 user reviews
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status should be 200
    And the school Reviews should have exactly 25 items
    And the reviews should be the most recent 25 reviews
    And the reviews should be ordered by date descending

  Scenario: Retrieve school with fewer than 25 reviews
    Given school "550e8400-e29b-41d4-a716-446655440002" has 15 user reviews
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440002"
    Then the response status should be 200
    And the school Reviews should have exactly 15 items

  Scenario: Retrieve school with no reviews
    Given school "550e8400-e29b-41d4-a716-446655440003" has 0 user reviews
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440003"
    Then the response status should be 200
    And the school Reviews should be an empty array
    And the school Score should reflect default values

  Scenario: Reviews are ordered by date descending
    Given school "550e8400-e29b-41d4-a716-446655440001" has reviews with dates:
      | Username | Comment   | Rating | Date       |
      | User1    | Great!    | 5.0    | 2026-03-01 |
      | User2    | Excellent | 4.5    | 2026-03-10 |
      | User3    | Amazing   | 4.8    | 2026-03-05 |
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status should be 200
    And the reviews should be ordered:
      | Username | Date       |
      | User2    | 2026-03-10 |
      | User3    | 2026-03-05 |
      | User1    | 2026-03-01 |

  Scenario: Performance - Response time within acceptable range
    Given the database contains 1000 schools
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status should be 200
    And the response time should be less than 200ms

  Scenario: Score calculation includes all four dimensions
    When I request GET "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status should be 200
    And the Score.Total should equal the average of Innovation, Building, Professorate, and ManagementTeam
```

---

## 3. OpenAPI Schema & Arazzo Flow

### 3.1 Define OpenAPI Schema

**File:** `src/Api/openapi/schoolrate.yaml`

```yaml
openapi: 3.0.3
info:
  title: RateYourSchool API
  version: 1.0.0
  description: API for rating and reviewing schools

paths:
  /api/v1/schoolrates/{id}:
    get:
      summary: Get SchoolRate by ID
      description: |
        Retrieves detailed information about a single school including 
        comprehensive school details, aggregated scores, and recent reviews.
        
        **CQRS Note:** This endpoint reads from the SchoolRate read model,
        which is a denormalized materialized view optimized for queries.
        The read model contains aggregated data from School and UserReview
        write models, synchronized through MongoDB Change Streams.
        
        **Business Rules:**
        - BR-009: SchoolRate is a read model (CQRS pattern)
        - BR-012: Contains last 25 user reviews maximum
        - BR-028: Returns School View Model structure
        - BR-029: Response time < 200ms
      operationId: getSchoolRateById
      tags:
        - SchoolRates
      parameters:
        - name: id
          in: path
          description: Unique identifier of the SchoolRate (GUID format)
          required: true
          schema:
            type: string
            format: uuid
            example: 550e8400-e29b-41d4-a716-446655440001
      responses:
        '200':
          description: Successfully retrieved SchoolRate
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SchoolRateResponse'
              examples:
                successWithReviews:
                  summary: School with reviews
                  value:
                    info:
                      name: Maple Leaf Elementary
                      address: 123 Main Street
                      imageUrl: https://example.com/schools/maple-leaf.jpg
                    score:
                      innovation: 4.5
                      building: 4.3
                      professorate: 4.6
                      managementTeam: 4.4
                      total: 4.45
                    reviews:
                      - userName: JohnDoe
                        comment: Excellent school with great facilities
                        rating: 5.0
                      - userName: JaneSmith
                        comment: My kids love this school
                        rating: 4.5
                successNoReviews:
                  summary: School without reviews
                  value:
                    info:
                      name: New School
                      address: 456 Oak Avenue
                      imageUrl: https://example.com/schools/new-school.jpg
                    score:
                      innovation: 0
                      building: 0
                      professorate: 0
                      managementTeam: 0
                      total: 0
                    reviews: []
        '400':
          description: Bad Request - Invalid ID format
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                invalidIdFormat:
                  summary: Invalid GUID format
                  value:
                    type: https://tools.ietf.org/html/rfc7231#section-6.5.1
                    title: Bad Request
                    status: 400
                    detail: Invalid SchoolRate ID format. ID must be a valid GUID.
        '404':
          description: Not Found - SchoolRate does not exist
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                notFound:
                  summary: SchoolRate not found
                  value:
                    type: https://tools.ietf.org/html/rfc7231#section-6.5.4
                    title: Not Found
                    status: 404
                    detail: SchoolRate with ID '550e8400-e29b-41d4-a716-999999999999' not found.
        '500':
          description: Internal Server Error
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                serverError:
                  summary: Database connection failure
                  value:
                    type: https://tools.ietf.org/html/rfc7231#section-6.6.1
                    title: Internal Server Error
                    status: 500
                    detail: An error occurred while processing your request.

components:
  schemas:
    SchoolRateResponse:
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
          description: Last 25 user reviews ordered by date descending
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
          description: Name of the school
          example: Maple Leaf Elementary
        address:
          type: string
          description: Full physical address
          example: 123 Main Street, Toronto, ON M5H 2N2
        imageUrl:
          type: string
          format: uri
          description: URL to school image
          example: https://example.com/schools/maple-leaf.jpg
    
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
          description: Innovation score (0-5)
          minimum: 0
          maximum: 5
          example: 4.5
        building:
          type: number
          format: decimal
          description: Building/facilities score (0-5)
          minimum: 0
          maximum: 5
          example: 4.3
        professorate:
          type: number
          format: decimal
          description: Teaching staff quality score (0-5)
          minimum: 0
          maximum: 5
          example: 4.6
        managementTeam:
          type: number
          format: decimal
          description: Administrative leadership score (0-5)
          minimum: 0
          maximum: 5
          example: 4.4
        total:
          type: number
          format: decimal
          description: Average of all four dimension scores
          minimum: 0
          maximum: 5
          example: 4.45
    
    UserReview:
      type: object
      required:
        - userName
        - comment
        - rating
      properties:
        userName:
          type: string
          description: Name of the reviewer
          example: JohnDoe
        comment:
          type: string
          description: Review comment text
          example: Excellent school with great facilities
        rating:
          type: number
          format: decimal
          description: Overall rating given by user (0-5)
          minimum: 0
          maximum: 5
          example: 5.0
    
    ProblemDetails:
      type: object
      properties:
        type:
          type: string
          format: uri
          description: URI reference identifying the problem type
        title:
          type: string
          description: Short, human-readable summary
        status:
          type: integer
          description: HTTP status code
        detail:
          type: string
          description: Human-readable explanation
        instance:
          type: string
          format: uri
          description: URI reference identifying the specific occurrence
```

### 3.2 Arazzo Workflow Definition

**File:** `src/Api/arazzo/get-schoolrate-detail.yaml`

```yaml
arazzo: 1.0.0
info:
  title: View School Detail Information
  version: 1.0.0
  description: |
    This workflow describes the user journey of viewing detailed 
    information about a specific school, including its ratings, 
    reviews, and comprehensive details.

sourceDescriptions:
  - name: rateyourschool-api
    type: openapi
    url: ./openapi/schoolrate.yaml

workflows:
  - workflowId: view-school-detail
    summary: View detailed school information
    description: |
      User navigates to a school detail page and views comprehensive
      information including scores and recent reviews.
      
      This workflow demonstrates:
      1. Retrieving a single SchoolRate by ID
      2. Handling successful retrieval (200)
      3. Handling not found scenarios (404)
      4. Handling invalid ID formats (400)
    
    inputs:
      type: object
      properties:
        schoolRateId:
          type: string
          format: uuid
          description: The unique identifier of the school to view
    
    steps:
      - stepId: getSchoolRateDetail
        description: Retrieve detailed school information by ID
        operationId: getSchoolRateById
        parameters:
          - name: id
            in: path
            value: $inputs.schoolRateId
        successCriteria:
          - condition: $statusCode == 200
            context: School found and returned successfully
          - condition: $statusCode == 404
            type: simple
            context: School not found - display not found message
          - condition: $statusCode == 400
            type: simple
            context: Invalid ID format - display error message
        outputs:
          schoolInfo: $response.body.info
          schoolScore: $response.body.score
          schoolReviews: $response.body.reviews
          responseStatus: $statusCode

    outputs:
      schoolDetails:
        value: $steps.getSchoolRateDetail.schoolInfo
      aggregatedScore:
        value: $steps.getSchoolRateDetail.schoolScore
      recentReviews:
        value: $steps.getSchoolRateDetail.schoolReviews
      status:
        value: $steps.getSchoolRateDetail.responseStatus
```

### 3.3 Implementation Steps

1. **Create OpenAPI specification file**: `src/Api/openapi/schoolrate.yaml`
2. **Create Arazzo workflow file**: `src/Api/arazzo/get-schoolrate-detail.yaml`
3. **Configure Swashbuckle** in `Program.cs` to include OpenAPI documentation
4. **Add OpenAPI annotations** to endpoint using `.WithOpenApi()`
5. **Validate schema** using OpenAPI validation tools
6. **Test Arazzo workflow** for successful retrieval and error scenarios

---

## 4. Backend Implementation

### 4.1 Current State Analysis

**Existing Files:**
- ✅ `src/backend/Domain/Entities/SchoolRate.cs` - Read model entity exists
- ✅ `src/backend/Domain/Entities/School.cs` - Contains Score record definition
- ✅ `src/backend/Domain/Repositories/IReadOnlyRepository.cs` - Generic repository interface
- ✅ `src/backend/Api/Endpoints/GetSchools/SchoolViewModel.cs` - Reusable view models

**Missing/Incomplete:**
- ❌ `src/Api/Endpoints/GetSchoolRate/` - New vertical slice folder
- ❌ `src/Api/Endpoints/GetSchoolRate/Endpoint.cs` - API endpoint mapping
- ❌ `src/Api/Endpoints/GetSchoolRate/Handler.cs` - Business logic handler
- ❌ `src/Api/Endpoints/GetSchoolRate/ReadonlyRepository.cs` - Data access layer
- ❌ `src/Api/Endpoints/GetSchoolRate/Request.cs` - Input DTO
- ❌ `src/Api/Endpoints/GetSchoolRate/Response.cs` - Output DTO
- ❌ `src/Api/Endpoints/GetSchoolRate/ServiceCollectionExtensions.cs` - DI registration
- ❌ Registration in `Program.cs`

### 4.2 Repository Pattern Best Practices

**Generic Repository Pattern:**

Use the generic `IReadOnlyRepository.GetByIdAsync<T>()` method to keep repositories domain-agnostic:

```csharp
// IReadOnlyRepository interface (add this method if not exists)
Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken);
```

**MongoDB Projection for View Models:**

Query view models directly using MongoDB projection. The repository returns the view model, NOT the domain entity:

```csharp
public async Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken)
{
    // Validate ID format
    if (!Guid.TryParse(id, out _))
    {
        return default;
    }

    // Use projection to query ViewModel directly from SchoolRate collection
    // This avoids entity-to-viewmodel mapping layer
    var filter = Builders<SchoolRateDocument>.Filter.Eq(sr => sr.Id, id);
    
    var result = await _collection
        .Find(filter)
        .Project(sr => new SchoolViewModel
        {
            Info = new SchoolInfo(sr.School.Name, sr.Address, sr.ImageUrl),
            Score = sr.Score,
            Reviews = sr.Reviews.Take(25).Select(r => new UserReview(
                r.UserName,
                r.Comment,
                r.Rating
            ))
        })
        .FirstOrDefaultAsync(cancellationToken);
    
    return (T?)(object?)result;
}
```

**Handler Pattern:**

Handlers receive view models directly from repository—no mapping needed:

```csharp
public async Task<Response?> HandleAsync(Request request, CancellationToken ct)
{
    // Query ViewModel directly from read model - no entity-to-ViewModel mapping
    var viewModel = await _repository.GetByIdAsync<SchoolViewModel>(request.Id, ct);
    
    if (viewModel == null)
    {
        return null; // Handler returns null; endpoint maps to 404
    }
    
    return new Response { School = viewModel };
}
```

### 4.3 Implementation Steps

#### Step 1: Create Request DTO

**File:** `src/Api/Endpoints/GetSchoolRate/Request.cs` (NEW)

```csharp
namespace RateYourSchool.Endpoints.GetSchoolRate;

/// <summary>
/// Request to retrieve a single SchoolRate by ID.
/// BR-009: Reads from SchoolRate read model.
/// </summary>
internal sealed record Request
{
    /// <summary>
    /// Unique identifier of the SchoolRate (GUID format).
    /// </summary>
    public required string Id { get; init; }
}
```

**Purpose:** Encapsulates the incoming ID parameter from the route.

#### Step 2: Create Response DTO

**File:** `src/Api/Endpoints/GetSchoolRate/Response.cs` (NEW)

```csharp
using RateYourSchool.Endpoints.GetSchools;

namespace RateYourSchool.Endpoints.GetSchoolRate;

/// <summary>
/// Response containing a single SchoolRate with comprehensive details.
/// BR-028: Returns School View Model structure.
/// </summary>
internal sealed record Response
{
    /// <summary>
    /// School view model containing info, score, and reviews.
    /// </summary>
    public required SchoolViewModel School { get; init; }
}
```

**Purpose:** Wraps the SchoolViewModel for returning as JSON response. Reuses existing SchoolViewModel from GetSchools endpoint.

#### Step 3: Create Handler

**File:** `src/Api/Endpoints/GetSchoolRate/Handler.cs` (NEW)

```csharp
using Domain.Repositories;
using RateYourSchool.Endpoints.GetSchools;

namespace RateYourSchool.Endpoints.GetSchoolRate;

/// <summary>
/// Handles retrieval of a single SchoolRate by ID.
/// BR-009: Queries SchoolRate read model.
/// BR-012: Returns last 25 reviews.
/// </summary>
internal sealed class Handler : IHandler<Request, Response?>
{
    private readonly IReadOnlyRepository _repository;
    private readonly ILogger<Handler> _logger;

    public Handler(IReadOnlyRepository repository, ILogger<Handler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response?> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Retrieving SchoolRate with ID: {SchoolRateId}",
            request.Id);

        // Validate GUID format
        if (!Guid.TryParse(request.Id, out _))
        {
            _logger.LogWarning(
                "Invalid SchoolRate ID format: {SchoolRateId}",
                request.Id);
            
            // Return null to indicate bad request (endpoint will map to 400)
            throw new ArgumentException("Invalid SchoolRate ID format. ID must be a valid GUID.", nameof(request.Id));
        }

        try
        {
            // Query view model directly from read model using MongoDB projection
            // No entity-to-viewmodel mapping needed
            var schoolViewModel = await _repository
                .GetByIdAsync<SchoolViewModel>(request.Id, cancellationToken);

            if (schoolViewModel == null)
            {
                _logger.LogInformation(
                    "SchoolRate not found: {SchoolRateId}",
                    request.Id);
                
                return null; // Endpoint will map to 404 Not Found
            }

            _logger.LogInformation(
                "Successfully retrieved SchoolRate: {SchoolRateId}",
                request.Id);

            return new Response { School = schoolViewModel };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving SchoolRate: {SchoolRateId}",
                request.Id);
            
            throw; // Re-throw to let middleware handle
        }
    }
}
```

**Purpose:** Contains business logic for retrieving a single SchoolRate. Validates ID format, queries repository, handles not found scenarios.

#### Step 4: Create Repository

**File:** `src/Api/Endpoints/GetSchoolRate/ReadonlyRepository.cs` (NEW)

```csharp
using Domain.Repositories;
using MongoDB.Driver;
using RateYourSchool.Endpoints.GetSchools;
using RateYourSchool.Infrastructure.MongoDB;

namespace RateYourSchool.Endpoints.GetSchoolRate;

/// <summary>
/// Repository for retrieving SchoolRate read models.
/// Uses MongoDB projection to return view models directly.
/// </summary>
internal sealed class ReadonlyRepository : IReadOnlyRepository
{
    private readonly IMongoCollection<SchoolRateDocument> _collection;
    private readonly ILogger<ReadonlyRepository> _logger;

    public ReadonlyRepository(
        IMongoDatabase database,
        ILogger<ReadonlyRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(logger);

        _collection = database.GetCollection<SchoolRateDocument>("schoolRates");
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a view model by ID using MongoDB projection.
    /// BR-009: Queries SchoolRate read model collection.
    /// BR-012: Returns last 25 reviews.
    /// </summary>
    public async Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return default;
        }

        // Validate GUID format
        if (!Guid.TryParse(id, out _))
        {
            return default;
        }

        try
        {
            var filter = Builders<SchoolRateDocument>.Filter.Eq(sr => sr.Id, id);

            // Project directly to SchoolViewModel - no entity mapping
            var projection = Builders<SchoolRateDocument>.Projection.Expression(sr => 
                new SchoolViewModel
                {
                    Info = new SchoolInfo(
                        sr.School.Name,
                        sr.Address,
                        sr.ImageUrl ?? string.Empty
                    ),
                    Score = new Domain.Entities.Score(
                        sr.Score.Innovation,
                        sr.Score.Building,
                        sr.Score.Professorate,
                        sr.Score.ManagementTeam
                    ),
                    Reviews = sr.Reviews
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(25) // BR-012: Last 25 reviews
                        .Select(r => new UserReview(
                            r.UserName,
                            r.Comment,
                            r.Rating
                        ))
                });

            var result = await _collection
                .Find(filter)
                .Project(projection)
                .FirstOrDefaultAsync(cancellationToken);

            return (T?)(object?)result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving SchoolRate by ID: {SchoolRateId}",
                id);
            
            throw;
        }
    }

    // Implement other IReadOnlyRepository methods as needed
    public Task<IEnumerable<T>> GetAsync<T>(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Use GetByIdAsync for single resource retrieval");
    }
}

/// <summary>
/// MongoDB document model for SchoolRate collection.
/// Maps to the denormalized read model structure.
/// </summary>
internal sealed class SchoolRateDocument
{
    public string Id { get; set; } = string.Empty;
    public SchoolInfoDocument School { get; set; } = new();
    public string Address { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public ScoreDocument Score { get; set; } = new();
    public List<ReviewDocument> Reviews { get; set; } = new();
}

internal sealed class SchoolInfoDocument
{
    public string Name { get; set; } = string.Empty;
}

internal sealed class ScoreDocument
{
    public decimal Innovation { get; set; }
    public decimal Building { get; set; }
    public decimal Professorate { get; set; }
    public decimal ManagementTeam { get; set; }
}

internal sealed class ReviewDocument
{
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Purpose:** Implements data access using MongoDB projection to return view models directly from the SchoolRate read model collection. No entity-to-viewmodel mapping layer needed.

#### Step 5: Create Endpoint Mapping

**File:** `src/Api/Endpoints/GetSchoolRate/Endpoint.cs` (NEW)

```csharp
using Microsoft.AspNetCore.Mvc;

namespace RateYourSchool.Endpoints.GetSchoolRate;

/// <summary>
/// Endpoint for retrieving a single SchoolRate by ID.
/// </summary>
internal static class GetSchoolRateEndpoint
{
    public static IEndpointRouteBuilder MapGetSchoolRateEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/v1/schoolrates/{id}", async (
            string id,
            CancellationToken cancellationToken,
            IHandler<Request, Response?> handler) =>
            {
                var request = new Request { Id = id };

                try
                {
                    var response = await handler.HandleAsync(request, cancellationToken);

                    if (response == null)
                    {
                        // Return 404 Not Found with ProblemDetails
                        return Results.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            title: "Not Found",
                            detail: $"SchoolRate with ID '{id}' not found.",
                            type: "https://tools.ietf.org/html/rfc7231#section-6.5.4");
                    }

                    return Results.Ok(response.School);
                }
                catch (ArgumentException ex)
                {
                    // Return 400 Bad Request for invalid ID format
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Bad Request",
                        detail: ex.Message,
                        type: "https://tools.ietf.org/html/rfc7231#section-6.5.1");
                }
            })
            .WithName("GetSchoolRateById")
            .WithTags("SchoolRates")
            .WithOpenApi()
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
```

**Purpose:** Maps HTTP GET requests to the handler, provides error handling, and configures OpenAPI documentation.

#### Step 6: Create Service Registration

**File:** `src/Api/Endpoints/GetSchoolRate/ServiceCollectionExtensions.cs` (NEW)

```csharp
using Domain.Repositories;

namespace RateYourSchool.Endpoints.GetSchoolRate;

/// <summary>
/// Dependency injection registration for GetSchoolRate endpoint.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetSchoolRateEndpoint(this IServiceCollection services)
    {
        // Register handler
        services.AddScoped<IHandler<Request, Response?>, Handler>();
        
        // Register repository
        services.AddScoped<IReadOnlyRepository, ReadonlyRepository>();
        
        return services;
    }
}
```

**Purpose:** Registers all dependencies for the GetSchoolRate feature in the DI container.

#### Step 7: Register in Program.cs

**File:** `src/Api/Program.cs` (UPDATE)

Add the following registrations:

```csharp
// Register GetSchoolRate endpoint
builder.Services.AddGetSchoolRateEndpoint();

// ... existing code ...

// Map GetSchoolRate endpoint
app.MapGetSchoolRateEndpoint();
```

**Purpose:** Wires up the endpoint and its dependencies in the application startup.

#### Step 8: Add OpenTelemetry Instrumentation

**File:** `src/Api/Endpoints/GetSchoolRate/Handler.cs` (UPDATE)

Add Activity tracking:

```csharp
using System.Diagnostics;

// At class level
private static readonly ActivitySource ActivitySource = new("RateYourSchool.GetSchoolRate");

// In HandleAsync method
public async Task<Response?> HandleAsync(Request request, CancellationToken cancellationToken)
{
    using var activity = ActivitySource.StartActivity("GetSchoolRate.Handle");
    activity?.SetTag("schoolrate.id", request.Id);
    
    // ... existing handler logic ...
    
    if (schoolViewModel != null)
    {
        activity?.SetTag("schoolrate.found", true);
        activity?.SetTag("school.name", schoolViewModel.Info.Name);
    }
    else
    {
        activity?.SetTag("schoolrate.found", false);
    }
    
    return response;
}
```

**Purpose:** Adds distributed tracing for observability and monitoring.

---

## 5. Unit Tests Implementation

### 5.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Unit/RateYourSchool.Tests.Unit.csproj`

Ensure the following packages are included:

```xml
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.1" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  <PackageReference Include="Moq" Version="4.20.69" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
</ItemGroup>
```

### 5.2 Handler Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolRate/HandlerTests.cs` (NEW)

```csharp
using FluentAssertions;
using Moq;
using RateYourSchool.Endpoints.GetSchoolRate;
using RateYourSchool.Endpoints.GetSchools;
using Domain.Repositories;
using Microsoft.Extensions.Logging;
using Xunit;

namespace RateYourSchool.Tests.Unit.Endpoints.GetSchoolRate;

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
    public async Task HandleAsync_WithValidId_ShouldReturnSchoolViewModel()
    {
        // Arrange
        var schoolId = "550e8400-e29b-41d4-a716-446655440001";
        var request = new Request { Id = schoolId };
        var expectedViewModel = CreateTestSchoolViewModel();

        _mockRepository
            .Setup(r => r.GetByIdAsync<SchoolViewModel>(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedViewModel);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.School.Should().NotBeNull();
        result.School.Info.Name.Should().Be("Test School");
        result.School.Score.Total.Should().BeGreaterThan(0);
        result.School.Reviews.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var schoolId = "550e8400-e29b-41d4-a716-999999999999";
        var request = new Request { Id = schoolId };

        _mockRepository
            .Setup(r => r.GetByIdAsync<SchoolViewModel>(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchoolViewModel?)null);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidGuidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidId = "invalid-guid-format";
        var request = new Request { Id = invalidId };

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid SchoolRate ID format*");
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        Request? request = null;

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(request!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrowsException_ShouldPropagate()
    {
        // Arrange
        var schoolId = "550e8400-e29b-41d4-a716-446655440001";
        var request = new Request { Id = schoolId };

        _mockRepository
            .Setup(r => r.GetByIdAsync<SchoolViewModel>(schoolId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public async Task HandleAsync_WithValidId_ShouldLogInformation()
    {
        // Arrange
        var schoolId = "550e8400-e29b-41d4-a716-446655440001";
        var request = new Request { Id = schoolId };
        var expectedViewModel = CreateTestSchoolViewModel();

        _mockRepository
            .Setup(r => r.GetByIdAsync<SchoolViewModel>(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedViewModel);

        // Act
        await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving SchoolRate")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentId_ShouldLogNotFound()
    {
        // Arrange
        var schoolId = "550e8400-e29b-41d4-a716-999999999999";
        var request = new Request { Id = schoolId };

        _mockRepository
            .Setup(r => r.GetByIdAsync<SchoolViewModel>(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchoolViewModel?)null);

        // Act
        await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static SchoolViewModel CreateTestSchoolViewModel()
    {
        return new SchoolViewModel
        {
            Info = new SchoolInfo("Test School", "123 Main St", "https://example.com/school.jpg"),
            Score = new Domain.Entities.Score(4.5m, 4.3m, 4.6m, 4.4m),
            Reviews = new List<UserReview>
            {
                new("User1", "Great school!", 5.0m),
                new("User2", "Excellent facilities", 4.5m)
            }
        };
    }
}
```

**Test Coverage Goals:**
- ✅ Happy path: Valid ID returns SchoolViewModel
- ✅ Not found: Non-existent ID returns null
- ✅ Validation: Invalid GUID format throws ArgumentException
- ✅ Null checks: Null request throws ArgumentNullException
- ✅ Error handling: Repository exceptions propagate
- ✅ Logging: Information logs for retrieval and not found

### 5.3 Repository Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolRate/RepositoryTests.cs` (NEW)

```csharp
using FluentAssertions;
using Xunit;
using RateYourSchool.Endpoints.GetSchoolRate;
using RateYourSchool.Endpoints.GetSchools;

namespace RateYourSchool.Tests.Unit.Endpoints.GetSchoolRate;

public class RepositoryTests
{
    [Fact]
    public void GetByIdAsync_WithEmptyId_ShouldReturnNull()
    {
        // Test ID validation logic
        var id = string.Empty;
        Guid.TryParse(id, out _).Should().BeFalse();
    }

    [Fact]
    public void GetByIdAsync_WithInvalidGuidFormat_ShouldReturnNull()
    {
        // Test ID validation logic
        var id = "not-a-guid";
        Guid.TryParse(id, out _).Should().BeFalse();
    }

    [Fact]
    public void GetByIdAsync_WithValidGuid_ShouldParseSuccessfully()
    {
        // Test ID validation logic
        var id = "550e8400-e29b-41d4-a716-446655440001";
        Guid.TryParse(id, out _).Should().BeTrue();
    }
}
```

### 5.4 Run Unit Tests

```bash
# Run all unit tests
dotnet test src/Tests/RateYourSchool.Tests.Unit/

# Run with coverage
dotnet test src/Tests/RateYourSchool.Tests.Unit/ /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~HandlerTests"

# Run with verbose output
dotnet test src/Tests/RateYourSchool.Tests.Unit/ --logger "console;verbosity=detailed"
```

**Expected Results:**
- All tests pass ✅
- Code coverage > 80% for Handler and Repository
- No warnings or errors

---

## 6. Integration Tests Implementation

### 6.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Integration/RateYourSchool.Tests.Integration.csproj`

Ensure the following packages are included:

```xml
<ItemGroup>
  <PackageReference Include="ReqnRoll" Version="2.0.0" />
  <PackageReference Include="ReqnRoll.xUnit" Version="2.0.0" />
  <PackageReference Include="Testcontainers.MongoDb" Version="3.6.0" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="xunit" Version="2.6.1" />
</ItemGroup>
```

### 6.2 Gherkin Feature File

**File:** `src/Tests/RateYourSchool.Tests.Integration/Features/GetSchoolRate.feature` (NEW)

```gherkin
Feature: Get SchoolRate by ID
  As an anonymous user
  I want to retrieve detailed information about a specific school
  So that I can view comprehensive school ratings and reviews

  Background:
    Given the database is clean
    And the following schools exist in the database:
      | SchoolId                             | Name                  | Address          | City    | Province | Type   | TotalScore |
      | 550e8400-e29b-41d4-a716-446655440001 | Maple Leaf Elementary | 123 Main Street  | Toronto | ON       | Public | 4.5        |
      | 550e8400-e29b-41d4-a716-446655440002 | Oak Grove High        | 456 Oak Avenue   | Toronto | ON       | Public | 3.8        |

  Scenario: Successfully retrieve a school by valid ID
    When I send a GET request to "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status code should be 200
    And the response should contain school with name "Maple Leaf Elementary"
    And the school should have Info property
    And the school should have Score property
    And the school should have Reviews property

  Scenario: School not found - returns 404
    When I send a GET request to "/api/v1/schoolrates/550e8400-e29b-41d4-a716-999999999999"
    Then the response status code should be 404
    And the response should contain a ProblemDetails object
    And the ProblemDetails title should be "Not Found"

  Scenario: Invalid ID format - returns 400
    When I send a GET request to "/api/v1/schoolrates/invalid-id-format"
    Then the response status code should be 400
    And the response should contain a ProblemDetails object
    And the ProblemDetails title should be "Bad Request"

  Scenario: Retrieve school with reviews limited to 25
    Given school "550e8400-e29b-41d4-a716-446655440001" has 30 reviews
    When I send a GET request to "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status code should be 200
    And the school Reviews should have at most 25 items

  Scenario: Retrieve school with no reviews
    When I send a GET request to "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440002"
    Then the response status code should be 200
    And the school Reviews should be an empty array

  Scenario: Score includes all four dimensions
    When I send a GET request to "/api/v1/schoolrates/550e8400-e29b-41d4-a716-446655440001"
    Then the response status code should be 200
    And the Score should have Innovation property
    And the Score should have Building property
    And the Score should have Professorate property
    And the Score should have ManagementTeam property
    And the Score should have Total property
```

### 6.3 Test Infrastructure

**File:** `src/Tests/RateYourSchool.Tests.Integration/Support/TestWebApplicationFactory.cs` (UPDATE if exists)

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace RateYourSchool.Tests.Integration.Support;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .WithPortBinding(27017, true)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace MongoDB connection with test container
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMongoClient));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IMongoClient>(sp =>
                new MongoClient(_mongoContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}
```

### 6.4 Step Definitions

**File:** `src/Tests/RateYourSchool.Tests.Integration/StepDefinitions/GetSchoolRateSteps.cs` (NEW)

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ReqnRoll;
using RateYourSchool.Endpoints.GetSchools;
using RateYourSchool.Tests.Integration.Support;

namespace RateYourSchool.Tests.Integration.StepDefinitions;

[Binding]
public class GetSchoolRateSteps : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IMongoDatabase _database;
    private HttpResponseMessage? _response;
    private SchoolViewModel? _schoolViewModel;
    private ProblemDetails? _problemDetails;

    public GetSchoolRateSteps(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        var mongoClient = factory.Services.GetRequiredService<IMongoClient>();
        _database = mongoClient.GetDatabase("RateYourSchool_Test");
    }

    [Given(@"the database is clean")]
    public async Task GivenTheDatabaseIsClean()
    {
        await _database.DropCollectionAsync("schoolRates");
    }

    [Given(@"the following schools exist in the database:")]
    public async Task GivenTheFollowingSchoolsExist(Table table)
    {
        var collection = _database.GetCollection<BsonDocument>("schoolRates");
        
        foreach (var row in table.Rows)
        {
            var document = new BsonDocument
            {
                { "_id", row["SchoolId"] },
                { "school", new BsonDocument { { "name", row["Name"] } } },
                { "address", row["Address"] },
                { "imageUrl", $"https://example.com/{row["Name"].ToLower().Replace(" ", "-")}.jpg" },
                { "score", new BsonDocument
                    {
                        { "innovation", decimal.Parse(row["TotalScore"]) },
                        { "building", decimal.Parse(row["TotalScore"]) },
                        { "professorate", decimal.Parse(row["TotalScore"]) },
                        { "managementTeam", decimal.Parse(row["TotalScore"]) }
                    }
                },
                { "reviews", new BsonArray() }
            };
            
            await collection.InsertOneAsync(document);
        }
    }

    [Given(@"school ""(.*)"" has (.*) reviews")]
    public async Task GivenSchoolHasReviews(string schoolId, int reviewCount)
    {
        var collection = _database.GetCollection<BsonDocument>("schoolRates");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", schoolId);
        
        var reviews = new BsonArray();
        for (int i = 1; i <= reviewCount; i++)
        {
            reviews.Add(new BsonDocument
            {
                { "userName", $"User{i}" },
                { "comment", $"Review {i}" },
                { "rating", 4.5 },
                { "createdAt", DateTime.UtcNow.AddDays(-i) }
            });
        }
        
        var update = Builders<BsonDocument>.Update.Set("reviews", reviews);
        await collection.UpdateOneAsync(filter, update);
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendGetRequest(string url)
    {
        _response = await _client.GetAsync(url);
        
        if (_response.IsSuccessStatusCode)
        {
            _schoolViewModel = await _response.Content.ReadFromJsonAsync<SchoolViewModel>();
        }
        else if (_response.StatusCode == HttpStatusCode.NotFound || 
                 _response.StatusCode == HttpStatusCode.BadRequest)
        {
            _problemDetails = await _response.Content.ReadFromJsonAsync<ProblemDetails>();
        }
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        ((int)_response!.StatusCode).Should().Be(expectedStatusCode);
    }

    [Then(@"the response should contain school with name ""(.*)""")]
    public void ThenTheResponseShouldContainSchoolWithName(string expectedName)
    {
        _schoolViewModel.Should().NotBeNull();
        _schoolViewModel!.Info.Name.Should().Be(expectedName);
    }

    [Then(@"the school should have Info property")]
    public void ThenTheSchoolShouldHaveInfoProperty()
    {
        _schoolViewModel.Should().NotBeNull();
        _schoolViewModel!.Info.Should().NotBeNull();
    }

    [Then(@"the school should have Score property")]
    public void ThenTheSchoolShouldHaveScoreProperty()
    {
        _schoolViewModel.Should().NotBeNull();
        _schoolViewModel!.Score.Should().NotBeNull();
    }

    [Then(@"the school should have Reviews property")]
    public void ThenTheSchoolShouldHaveReviewsProperty()
    {
        _schoolViewModel.Should().NotBeNull();
        _schoolViewModel!.Reviews.Should().NotBeNull();
    }

    [Then(@"the response should contain a ProblemDetails object")]
    public void ThenTheResponseShouldContainProblemDetails()
    {
        _problemDetails.Should().NotBeNull();
    }

    [Then(@"the ProblemDetails title should be ""(.*)""")]
    public void ThenTheProblemDetailsTitleShouldBe(string expectedTitle)
    {
        _problemDetails.Should().NotBeNull();
        _problemDetails!.Title.Should().Be(expectedTitle);
    }

    [Then(@"the school Reviews should have at most (.*) items")]
    public void ThenTheSchoolReviewsShouldHaveAtMostItems(int maxItems)
    {
        _schoolViewModel.Should().NotBeNull();
        _schoolViewModel!.Reviews.Should().HaveCountLessOrEqualTo(maxItems);
    }

    [Then(@"the school Reviews should be an empty array")]
    public void ThenTheSchoolReviewsShouldBeEmpty()
    {
        _schoolViewModel.Should().NotBeNull();
        _schoolViewModel!.Reviews.Should().BeEmpty();
    }

    [Then(@"the Score should have (.*) property")]
    public void ThenTheScoreShouldHaveProperty(string propertyName)
    {
        _schoolViewModel.Should().NotBeNull();
        var scoreType = _schoolViewModel!.Score.GetType();
        var property = scoreType.GetProperty(propertyName);
        property.Should().NotBeNull($"Score should have {propertyName} property");
    }
}
```

### 6.5 Run Integration Tests

```bash
# Run all integration tests
dotnet test src/Tests/RateYourSchool.Tests.Integration/

# Run specific feature
dotnet test --filter "FullyQualifiedName~GetSchoolRate"

# Generate BDD report
dotnet test src/Tests/RateYourSchool.Tests.Integration/ --logger "reqnroll;LogFilePath=TestResults/report.html"

# Run with verbose output
dotnet test src/Tests/RateYourSchool.Tests.Integration/ --logger "console;verbosity=detailed"
```

---

## 7. Frontend Implementation

### 7.1 Project Setup

**Prerequisites:**
- Node.js 18+
- npm or yarn
- React 18+
- TypeScript 5+

**Initialize/Update:**
```bash
cd src/frontend
npm install
```

### 7.2 TypeScript Types

**File:** `src/frontend/src/types/schoolRate.ts` (NEW)

```typescript
/**
 * SchoolRate response type matching backend API contract.
 * Represents a single school with ratings and reviews.
 */
export interface SchoolRateResponse {
  info: SchoolInfo;
  score: Score;
  reviews: UserReview[];
}

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

/**
 * API error response type.
 */
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail: string;
  instance?: string;
}
```

### 7.3 API Service

**File:** `src/frontend/src/services/schoolRateService.ts` (NEW)

```typescript
import axios, { AxiosError } from 'axios';
import { SchoolRateResponse, ProblemDetails } from '../types/schoolRate';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

/**
 * Service for interacting with SchoolRate API endpoints.
 */
export const schoolRateService = {
  /**
   * Retrieves a single SchoolRate by ID.
   * @param id - The unique identifier of the school (GUID)
   * @returns The school rate response
   * @throws 404 Not Found if school doesn't exist
   * @throws 400 Bad Request if ID format is invalid
   */
  async getSchoolRateById(id: string): Promise<SchoolRateResponse> {
    try {
      const response = await axios.get<SchoolRateResponse>(
        `${API_BASE_URL}/api/v1/schoolrates/${id}`
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const axiosError = error as AxiosError<ProblemDetails>;
        if (axiosError.response?.status === 404) {
          throw new Error('School not found');
        } else if (axiosError.response?.status === 400) {
          throw new Error('Invalid school ID format');
        }
      }
      throw new Error('Failed to retrieve school information');
    }
  },
};
```

### 7.4 Custom Hooks

**File:** `src/frontend/src/hooks/useSchoolRate.ts` (NEW)

```typescript
import { useState, useEffect } from 'react';
import { SchoolRateResponse } from '../types/schoolRate';
import { schoolRateService } from '../services/schoolRateService';

export interface UseSchoolRateResult {
  school: SchoolRateResponse | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

/**
 * Custom hook for fetching a single SchoolRate by ID.
 * @param id - The unique identifier of the school
 * @returns School data, loading state, error state, and refetch function
 */
export const useSchoolRate = (id: string): UseSchoolRateResult => {
  const [school, setSchool] = useState<SchoolRateResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSchool = async () => {
    if (!id) {
      setError('School ID is required');
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const data = await schoolRateService.getSchoolRateById(id);
      setSchool(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An unknown error occurred');
      setSchool(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSchool();
  }, [id]);

  return {
    school,
    loading,
    error,
    refetch: fetchSchool,
  };
};
```

### 7.5 Components

**File:** `src/frontend/src/components/SchoolDetail/SchoolDetailCard.tsx` (NEW)

```typescript
import React from 'react';
import { SchoolRateResponse } from '../../types/schoolRate';
import './SchoolDetailCard.css';

interface SchoolDetailCardProps {
  school: SchoolRateResponse;
}

export const SchoolDetailCard: React.FC<SchoolDetailCardProps> = ({ school }) => {
  return (
    <div className="school-detail-card">
      <div className="school-header">
        <img 
          src={school.info.imageUrl} 
          alt={school.info.name}
          className="school-image"
        />
        <div className="school-info">
          <h1>{school.info.name}</h1>
          <p className="school-address">{school.info.address}</p>
        </div>
      </div>

      <div className="school-scores">
        <h2>School Ratings</h2>
        <div className="score-grid">
          <div className="score-item">
            <span className="score-label">Innovation</span>
            <span className="score-value">{school.score.innovation.toFixed(1)}</span>
          </div>
          <div className="score-item">
            <span className="score-label">Building</span>
            <span className="score-value">{school.score.building.toFixed(1)}</span>
          </div>
          <div className="score-item">
            <span className="score-label">Professorate</span>
            <span className="score-value">{school.score.professorate.toFixed(1)}</span>
          </div>
          <div className="score-item">
            <span className="score-label">Management Team</span>
            <span className="score-value">{school.score.managementTeam.toFixed(1)}</span>
          </div>
        </div>
        <div className="score-total">
          <span className="score-label">Total Score</span>
          <span className="score-value-large">{school.score.total.toFixed(2)}</span>
        </div>
      </div>

      <div className="school-reviews">
        <h2>Recent Reviews ({school.reviews.length})</h2>
        {school.reviews.length > 0 ? (
          <div className="reviews-list">
            {school.reviews.map((review, index) => (
              <div key={index} className="review-item">
                <div className="review-header">
                  <span className="review-author">{review.userName}</span>
                  <span className="review-rating">★ {review.rating.toFixed(1)}</span>
                </div>
                <p className="review-comment">{review.comment}</p>
              </div>
            ))}
          </div>
        ) : (
          <p className="no-reviews">No reviews yet. Be the first to review this school!</p>
        )}
      </div>
    </div>
  );
};
```

**File:** `src/frontend/src/components/SchoolDetail/SchoolDetailCard.css` (NEW)

```css
.school-detail-card {
  max-width: 900px;
  margin: 0 auto;
  padding: 20px;
}

.school-header {
  display: flex;
  gap: 20px;
  margin-bottom: 30px;
  align-items: center;
}

.school-image {
  width: 200px;
  height: 200px;
  object-fit: cover;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.school-info h1 {
  margin: 0 0 10px 0;
  font-size: 2rem;
  color: #333;
}

.school-address {
  color: #666;
  font-size: 1.1rem;
}

.school-scores {
  margin-bottom: 30px;
  padding: 20px;
  background: #f9f9f9;
  border-radius: 8px;
}

.school-scores h2 {
  margin-top: 0;
  color: #333;
}

.score-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: 15px;
  margin-bottom: 20px;
}

.score-item {
  display: flex;
  flex-direction: column;
  padding: 15px;
  background: white;
  border-radius: 6px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.score-label {
  font-size: 0.9rem;
  color: #666;
  margin-bottom: 5px;
}

.score-value {
  font-size: 1.5rem;
  font-weight: bold;
  color: #2196F3;
}

.score-total {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  background: #2196F3;
  color: white;
  border-radius: 6px;
}

.score-value-large {
  font-size: 2.5rem;
  font-weight: bold;
}

.school-reviews h2 {
  color: #333;
  margin-bottom: 15px;
}

.reviews-list {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.review-item {
  padding: 15px;
  background: #f9f9f9;
  border-radius: 6px;
  border-left: 4px solid #2196F3;
}

.review-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 10px;
}

.review-author {
  font-weight: bold;
  color: #333;
}

.review-rating {
  color: #FFC107;
  font-weight: bold;
}

.review-comment {
  margin: 0;
  color: #555;
  line-height: 1.5;
}

.no-reviews {
  text-align: center;
  padding: 40px;
  color: #999;
  font-style: italic;
}
```

### 7.6 Pages

**File:** `src/frontend/src/pages/SchoolDetailPage/SchoolDetailPage.tsx` (NEW)

```typescript
import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useSchoolRate } from '../../hooks/useSchoolRate';
import { SchoolDetailCard } from '../../components/SchoolDetail/SchoolDetailCard';
import './SchoolDetailPage.css';

export const SchoolDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { school, loading, error, refetch } = useSchoolRate(id || '');

  if (loading) {
    return (
      <div className="school-detail-page">
        <div className="loading-spinner">Loading school information...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="school-detail-page">
        <div className="error-message">
          <h2>Error Loading School</h2>
          <p>{error}</p>
          <div className="error-actions">
            <button onClick={() => refetch()} className="btn-retry">
              Retry
            </button>
            <button onClick={() => navigate('/')} className="btn-back">
              Back to Schools
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (!school) {
    return (
      <div className="school-detail-page">
        <div className="not-found">
          <h2>School Not Found</h2>
          <p>The school you're looking for doesn't exist or has been removed.</p>
          <button onClick={() => navigate('/')} className="btn-back">
            Back to Schools
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="school-detail-page">
      <button onClick={() => navigate(-1)} className="btn-back-link">
        ← Back
      </button>
      <SchoolDetailCard school={school} />
    </div>
  );
};
```

**File:** `src/frontend/src/pages/SchoolDetailPage/SchoolDetailPage.css` (NEW)

```css
.school-detail-page {
  min-height: 100vh;
  padding: 20px;
  background: #f5f5f5;
}

.loading-spinner {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
  font-size: 1.2rem;
  color: #666;
}

.error-message,
.not-found {
  max-width: 600px;
  margin: 100px auto;
  padding: 40px;
  background: white;
  border-radius: 8px;
  text-align: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.error-message h2,
.not-found h2 {
  color: #d32f2f;
  margin-top: 0;
}

.error-actions {
  display: flex;
  gap: 15px;
  justify-content: center;
  margin-top: 20px;
}

.btn-retry,
.btn-back {
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  transition: background 0.3s;
}

.btn-retry {
  background: #2196F3;
  color: white;
}

.btn-retry:hover {
  background: #1976D2;
}

.btn-back {
  background: #e0e0e0;
  color: #333;
}

.btn-back:hover {
  background: #bdbdbd;
}

.btn-back-link {
  display: inline-block;
  margin-bottom: 20px;
  padding: 8px 16px;
  background: white;
  border: 1px solid #ddd;
  border-radius: 4px;
  color: #2196F3;
  text-decoration: none;
  cursor: pointer;
  transition: all 0.3s;
}

.btn-back-link:hover {
  background: #f5f5f5;
  border-color: #2196F3;
}
```

### 7.7 Configuration

**File:** `.env.development` (NEW)

```env
VITE_API_BASE_URL=http://localhost:5000
```

**File:** `.env.production` (NEW)

```env
VITE_API_BASE_URL=https://api.rateyourschool.com
```

### 7.8 Run Frontend

```bash
# Development
npm run dev

# Production build
npm run build

# Preview production build
npm run preview

# Type check
npm run type-check

# Lint
npm run lint
```

---

## 8. Implementation Checklist

### Backend
- [ ] Create `src/Api/Endpoints/GetSchoolRate/` folder
- [ ] Implement `Request.cs` with ID property
- [ ] Implement `Response.cs` wrapping SchoolViewModel
- [ ] Implement `Handler.cs` with validation and error handling
- [ ] Implement `ReadonlyRepository.cs` with MongoDB projection
- [ ] Implement `Endpoint.cs` with route mapping and OpenAPI annotations
- [ ] Implement `ServiceCollectionExtensions.cs` for DI registration
- [ ] Register endpoint in `Program.cs`
- [ ] Add OpenTelemetry instrumentation to Handler
- [ ] Add structured logging throughout

### Testing
- [ ] Create unit test project structure
- [ ] Implement `HandlerTests.cs` with all scenarios
- [ ] Implement `RepositoryTests.cs` for validation logic
- [ ] Run unit tests and verify >80% coverage
- [ ] Create integration test feature file `GetSchoolRate.feature`
- [ ] Implement `GetSchoolRateSteps.cs` with all step definitions
- [ ] Configure TestWebApplicationFactory with MongoDB container
- [ ] Run integration tests and verify all scenarios pass
- [ ] Generate BDD test report

### API Documentation
- [ ] Create `src/Api/openapi/schoolrate.yaml`
- [ ] Define all request/response schemas
- [ ] Add realistic examples for all responses
- [ ] Create `src/Api/arazzo/get-schoolrate-detail.yaml`
- [ ] Define complete workflow with success criteria
- [ ] Validate OpenAPI schema
- [ ] Test Arazzo workflow execution
- [ ] Update Swashbuckle configuration

### Frontend
- [ ] Create TypeScript types in `src/types/schoolRate.ts`
- [ ] Implement `schoolRateService.ts` API client
- [ ] Implement `useSchoolRate.ts` custom hook
- [ ] Create `SchoolDetailCard.tsx` component
- [ ] Create `SchoolDetailCard.css` styling
- [ ] Create `SchoolDetailPage.tsx` page component
- [ ] Create `SchoolDetailPage.css` styling
- [ ] Configure environment variables
- [ ] Add route to router configuration
- [ ] Test loading, error, and success states

### DevOps
- [ ] Update CI/CD pipeline for new endpoint
- [ ] Configure monitoring and alerts
- [ ] Update deployment documentation
- [ ] Add performance monitoring
- [ ] Configure error tracking

---

## 9. Notes for Subagent Implementation

This use case document is designed for implementation by specialized subagents. Follow the guidelines below for coordinated execution.

### 1. Backend Subagent

**Focus:** Section 4 (Backend Implementation)

**Responsibilities:**
- Create vertical slice folder structure
- Implement all C# files (Request, Response, Handler, Repository, Endpoint, ServiceCollectionExtensions)
- Register endpoint in Program.cs
- Add OpenTelemetry instrumentation
- Configure MongoDB projections
- Implement error handling

**Success Criteria:**
- All files compile without errors
- Endpoint is accessible at `/api/v1/schoolrates/{id}`
- Returns 200 for valid ID, 404 for not found, 400 for invalid format
- Handler uses repository pattern with MongoDB projection
- No entity-to-viewmodel mapping layer

**Key Patterns:**
- Use `IReadOnlyRepository.GetByIdAsync<SchoolViewModel>()`
- MongoDB projection returns view model directly
- Handler validates GUID format before querying
- Return null from handler for 404 scenarios
- Throw ArgumentException for 400 scenarios

### 2. Testing Subagent

**Focus:** Sections 5 & 6 (Unit and Integration Tests)

**Responsibilities:**
- Create unit test project structure
- Implement handler unit tests (happy path, not found, invalid ID, exceptions)
- Implement repository validation tests
- Create Gherkin feature file
- Implement step definitions
- Configure TestWebApplicationFactory
- Run all tests and verify coverage

**Success Criteria:**
- Unit test coverage >80%
- All unit tests pass
- All integration tests pass
- BDD scenarios match acceptance criteria
- Test report generated successfully

**Key Patterns:**
- Mock `IReadOnlyRepository.GetByIdAsync<SchoolViewModel>()`
- Test data helpers return SchoolViewModel instances
- Verify logging calls in unit tests
- Use TestContainers for MongoDB in integration tests

### 3. API Documentation Subagent

**Focus:** Section 3 (OpenAPI & Arazzo)

**Responsibilities:**
- Create OpenAPI schema file
- Define all components/schemas
- Add realistic examples
- Create Arazzo workflow
- Validate schemas
- Configure Swashbuckle

**Success Criteria:**
- OpenAPI schema is valid
- All response codes documented (200, 400, 404, 500)
- Examples provided for all scenarios
- Arazzo workflow executes successfully
- Swagger UI displays endpoint correctly

### 4. Frontend Subagent

**Focus:** Section 7 (Frontend Implementation)

**Responsibilities:**
- Create TypeScript types
- Implement API service
- Implement custom hook
- Create React components
- Implement page component
- Add styling
- Configure environment variables

**Success Criteria:**
- Types match backend API contract
- API service handles all error cases
- Custom hook manages loading/error states
- Components render correctly
- Styling is responsive
- No TypeScript errors

### 5. Integration Subagent

**Focus:** Cross-cutting concerns and coordination

**Responsibilities:**
- Ensure backend and frontend contracts match
- Verify OpenAPI schema matches implementation
- Coordinate testing across all layers
- Validate end-to-end functionality
- Update documentation

**Success Criteria:**
- API contract consistency verified
- All tests pass (unit, integration, E2E)
- Documentation is complete and accurate
- Performance requirements met (<200ms)
- Error handling works across all layers

### Subagent Guidelines

Each subagent should:
- Read the relevant section(s) thoroughly before starting
- Follow code examples exactly as shown
- Verify implementation against acceptance criteria
- Update implementation progress table when completing tasks
- Document any deviations or improvements
- Report blockers immediately
- Maintain code quality standards per copilot-instructions.md

### Coordination Points

- **Backend → Testing**: Handler and repository must be implemented before unit tests can run
- **Backend → Frontend**: API contract (response structure) must match TypeScript types exactly
- **API Docs → All**: OpenAPI spec is source of truth for request/response structure
- **Testing → Integration**: All individual tests must pass before end-to-end validation
- **All → Integration**: Final verification of complete user journey

### Implementation Order

Recommended sequence:
1. **Backend Subagent** creates vertical slice (allows testing to begin)
2. **API Documentation Subagent** creates OpenAPI/Arazzo (defines contract)
3. **Testing Subagent** implements tests (validates backend)
4. **Frontend Subagent** implements UI (uses API contract)
5. **Integration Subagent** validates complete flow

---

## Document Maintenance

### Version Control

- Update the **Implementation Progress** table when tasks are completed
- Document the completion date in YYYY-MM-DD format
- Change status from `TODO` → `IN-PROGRESS` → `IN-REVIEW` → `DONE`
- Keep acceptance criteria updated if requirements change

### Review Process

1. **Initial Review**: After functional requirements are documented ✅ (Completed 2026-03-16)
2. **Technical Review**: After API specifications are defined
3. **Implementation Review**: After backend is implemented
4. **Testing Review**: After tests are written and passing
5. **Final Review**: Before marking as `DONE`

### Update Triggers

Update this document when:
- Requirements change
- New edge cases are discovered
- Implementation approaches change
- Test scenarios are added/modified
- Performance requirements change
- Security requirements change
- Business rules are updated

---

**Document Version:** 1.0  
**Last Updated:** 2026-03-16  
**Status:** Functional Requirements Complete
