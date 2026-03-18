# Use Case: Get School Reviews (Paginated by School)

**Status:** Not Started  
**Priority:** High  
**Related Use Cases:** UC-001 (Get SchoolRates), UC-002 (Get SchoolRate), UC-004 (Create School Review)  
**Business Rules:** BR-024 through BR-028, BR-036 through BR-040

---

## Implementation Progress

| Task | Status | Completed |
|------|--------|-----------|
| **Functional Requirements Document** | done | 2026-03-18 |
| **API Specifications (OpenAPI + Arazzo)** | todo | - |
| **Backend Implementation (Vertical Slice)** | todo | - |
| **Frontend Implementation** | todo | - |
| **E2E Tests** | todo | - |
| **Penetration Tests** | todo | - |

**Status Legend:**
- `todo` - Not started
- `in-progress` - Currently being worked on
- `in-review` - Completed and under review
- `done` - Completed and verified

---

## 1. Summary

### Overview

This use case enables anonymous users to retrieve a paginated list of reviews for a specific school identified by its unique ID. The endpoint returns detailed review information including the reviewer's username, comment, multi-dimensional scores (Innovation, Building, Professorate, ManagementTeam), and a calculated average score across all four dimensions. This feature is essential for school detail pages where users can browse through all reviews to make informed decisions about schools.

Unlike the SchoolRate endpoint that returns only the last 25 reviews, this endpoint provides access to all reviews with pagination support, allowing users to view complete review history. The endpoint queries the UserReview write model directly with appropriate filtering and sorting.

The endpoint supports standard pagination parameters, proper error handling for missing schools (404 Not Found), and ensures optimal performance for large review datasets.

### Functional Requirements

- Retrieve paginated reviews for a specific school by school ID
- Return complete review information for each entry:
  - Username (reviewer identifier)
  - Comment (text review)
  - Innovation score (0-5 decimal)
  - Building score (0-5 decimal)
  - Professorate score (0-5 decimal)
  - ManagementTeam score (0-5 decimal)
  - Average score (calculated from the four dimensions)
  - Created timestamp
- Support configurable page size with validation (min: 1, max: 100)
- Default to page 1 and page size 20 if not specified (BR-025)
- Automatically correct invalid page numbers (< 1) to 1 (BR-026)
- Order reviews by creation date descending (most recent first)
- Return 404 Not Found when the specified school does not exist
- Return 200 with empty array when school exists but has no reviews
- Include pagination metadata (total count, current page, page size, total pages)
- Ensure response time meets performance requirements (< 200ms per BR-029)

### Technical Context

- **CQRS Pattern**: Reads from UserReview write model (filtered by schoolId)
- **Data Source**: MongoDB `userReviews` collection
- **Architecture**: Vertical Slice in `src/Api/Endpoints/GetSchoolReviews/`
- **Current State**: No existing implementation - new endpoint required
- **Score Calculation**: Average score calculated in-memory from the four dimension scores
- **Validation**: School existence validated against `schools` collection

---

## 2. Acceptance Criteria (Gherkin)

```gherkin
Feature: Get School Reviews (Paginated)
  As an anonymous user
  I want to retrieve a paginated list of reviews for a specific school
  So that I can read detailed feedback and ratings from other users

  Background:
    Given the following schools exist in the database:
      | SchoolId                             | Name                  | City    | Province |
      | 550e8400-e29b-41d4-a716-446655440001 | Maple Leaf Elementary | Toronto | ON       |
      | 550e8400-e29b-41d4-a716-446655440002 | Oak Grove High        | Toronto | ON       |
      | 550e8400-e29b-41d4-a716-446655440003 | Pine Academy          | Ottawa  | ON       |
    And the following reviews exist for school "550e8400-e29b-41d4-a716-446655440001":
      | ReviewId | Username     | Comment                      | Innovation | Building | Professorate | ManagementTeam | CreatedAt  |
      | rev-001  | ParentJohn   | Excellent facilities         | 4.5        | 4.8      | 4.2          | 4.0            | 2026-03-15 |
      | rev-002  | TeacherSarah | Great learning environment   | 4.0        | 4.5      | 5.0          | 4.5            | 2026-03-14 |
      | rev-003  | StudentMike  | Modern campus                | 3.8        | 5.0      | 4.0          | 3.5            | 2026-03-13 |
      | rev-004  | ParentMary   | Dedicated staff              | 4.2        | 4.0      | 4.8          | 4.6            | 2026-03-12 |
      | rev-005  | AlumniDave   | Good overall experience      | 4.0        | 3.8      | 4.5          | 4.2            | 2026-03-11 |

  Scenario: Retrieve first page with default page size
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status should be 200
    And the response should contain 5 reviews
    And each review should have the following properties:
      | Property       | Type     | Required |
      | ReviewId       | String   | Yes      |
      | Username       | String   | Yes      |
      | Comment        | String   | Yes      |
      | Innovation     | Decimal  | Yes      |
      | Building       | Decimal  | Yes      |
      | Professorate   | Decimal  | Yes      |
      | ManagementTeam | Decimal  | Yes      |
      | AverageScore   | Decimal  | Yes      |
      | CreatedAt      | DateTime | Yes      |
    And the pagination metadata should be:
      | Property   | Value |
      | TotalCount | 5     |
      | Page       | 1     |
      | PageSize   | 20    |
      | TotalPages | 1     |

  Scenario: Retrieve specific page with custom page size
    Given school "550e8400-e29b-41d4-a716-446655440001" has 30 reviews
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=2&pageSize=10"
    Then the response status should be 200
    And the response should contain 10 reviews
    And the pagination metadata should be:
      | Property   | Value |
      | TotalCount | 30    |
      | Page       | 2     |
      | PageSize   | 10    |
      | TotalPages | 3     |

  Scenario: Reviews are ordered by creation date descending
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status should be 200
    And the reviews should be ordered by CreatedAt descending:
      | Username     | CreatedAt  |
      | ParentJohn   | 2026-03-15 |
      | TeacherSarah | 2026-03-14 |
      | StudentMike  | 2026-03-13 |
      | ParentMary   | 2026-03-12 |
      | AlumniDave   | 2026-03-11 |

  Scenario: Average score is calculated correctly from four dimensions
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status should be 200
    And for the review by "ParentJohn" the AverageScore should be 4.375
    And for the review by "TeacherSarah" the AverageScore should be 4.5
    And for the review by "StudentMike" the AverageScore should be 4.075

  Scenario: Handle invalid page number (less than 1)
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=0"
    Then the response status should be 200
    And the page should be automatically corrected to 1
    And the response should contain reviews from page 1

  Scenario: Handle invalid page number (negative)
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=-5"
    Then the response status should be 200
    And the page should be automatically corrected to 1

  Scenario: Handle page size exceeding maximum
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?pageSize=200"
    Then the response status should be 200
    And the page size should be clamped to 100
    And the response should contain at most 100 reviews

  Scenario: Handle page size below minimum
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?pageSize=0"
    Then the response status should be 200
    And the page size should be clamped to 1
    And the response should contain exactly 1 review

  Scenario: School exists but has no reviews
    Given school "550e8400-e29b-41d4-a716-446655440003" has 0 reviews
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440003/reviews"
    Then the response status should be 200
    And the response should contain 0 reviews
    And the Reviews array should be empty
    And the pagination metadata should be:
      | Property   | Value |
      | TotalCount | 0     |
      | Page       | 1     |
      | PageSize   | 20    |
      | TotalPages | 0     |

  Scenario: School does not exist
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-999999999999/reviews"
    Then the response status should be 404
    And the response should contain a ProblemDetails object
    And the ProblemDetails should have the following properties:
      | Property | Value            |
      | Status   | 404              |
      | Title    | Not Found        |
      | Detail   | School not found |

  Scenario: Invalid school ID format
    When I request GET "/api/v1/schools/invalid-guid-format/reviews"
    Then the response status should be 400
    And the response should contain a ProblemDetails object
    And the ProblemDetails should have the following properties:
      | Property | Value                   |
      | Status   | 400                     |
      | Title    | Bad Request             |
      | Detail   | Invalid School ID format |

  Scenario: Request page beyond available data
    Given school "550e8400-e29b-41d4-a716-446655440001" has 5 reviews
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=10&pageSize=20"
    Then the response status should be 200
    And the response should contain 0 reviews
    And the pagination metadata should be:
      | Property   | Value |
      | TotalCount | 5     |
      | Page       | 10    |
      | PageSize   | 20    |
      | TotalPages | 1     |

  Scenario: Performance - Response time within acceptable range
    Given school "550e8400-e29b-41d4-a716-446655440001" has 1000 reviews
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=1&pageSize=20"
    Then the response status should be 200
    And the response time should be less than 200ms

  Scenario: Verify score precision
    Given a review exists with scores:
      | Innovation | Building | Professorate | ManagementTeam |
      | 4.33       | 3.67     | 4.89         | 4.11           |
    When I request GET "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status should be 200
    And the AverageScore should be calculated as 4.25 (rounded to 2 decimals)
    And all score values should be rounded to 2 decimal places
```

---

## 3. OpenAPI Schema & Arazzo Flow

### 3.1 Define OpenAPI Schema

**File:** `src/Api/openapi/school-reviews.yaml`

```yaml
openapi: 3.0.3
info:
  title: RateYourSchool API
  version: 1.0.0
  description: API for rating and reviewing schools

paths:
  /api/v1/schools/{schoolId}/reviews:
    get:
      summary: Get paginated reviews for a specific school
      description: |
        Retrieves a paginated list of all reviews for a specific school,
        ordered by creation date (most recent first). Each review includes
        multi-dimensional scores and a calculated average score.
        
        **CQRS Note:** This endpoint reads from the UserReview write model
        filtered by schoolId. Unlike the SchoolRate endpoint which shows
        only the last 25 reviews, this endpoint provides access to all
        reviews with pagination support.
      operationId: getSchoolReviews
      tags:
        - Reviews
      parameters:
        - name: schoolId
          in: path
          description: Unique identifier of the school (GUID format)
          required: true
          schema:
            type: string
            format: uuid
            example: "550e8400-e29b-41d4-a716-446655440001"
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
          description: Number of items per page. Clamped to [1, 100].
          required: false
          schema:
            type: integer
            minimum: 1
            maximum: 100
            default: 20
            example: 20
      responses:
        '200':
          description: Successfully retrieved school reviews
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GetSchoolReviewsResponse'
              examples:
                withReviews:
                  summary: School with multiple reviews
                  value:
                    reviews:
                      - reviewId: "rev-001"
                        username: "ParentJohn"
                        comment: "Excellent facilities and dedicated teachers"
                        innovation: 4.5
                        building: 4.8
                        professorate: 4.2
                        managementTeam: 4.0
                        averageScore: 4.38
                        createdAt: "2026-03-15T10:30:00Z"
                      - reviewId: "rev-002"
                        username: "TeacherSarah"
                        comment: "Great learning environment"
                        innovation: 4.0
                        building: 4.5
                        professorate: 5.0
                        managementTeam: 4.5
                        averageScore: 4.5
                        createdAt: "2026-03-14T14:20:00Z"
                    pagination:
                      totalCount: 42
                      page: 1
                      pageSize: 20
                      totalPages: 3
                noReviews:
                  summary: School with no reviews
                  value:
                    reviews: []
                    pagination:
                      totalCount: 0
                      page: 1
                      pageSize: 20
                      totalPages: 0
        '400':
          description: Bad request - Invalid school ID format
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              example:
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                title: "Bad Request"
                status: 400
                detail: "Invalid School ID format"
        '404':
          description: School not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              example:
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                title: "Not Found"
                status: 404
                detail: "School not found"
        '500':
          description: Internal server error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'

components:
  schemas:
    GetSchoolReviewsResponse:
      type: object
      required:
        - reviews
        - pagination
      properties:
        reviews:
          type: array
          description: List of reviews for the school
          items:
            $ref: '#/components/schemas/ReviewViewModel'
        pagination:
          $ref: '#/components/schemas/PaginationMetadata'

    ReviewViewModel:
      type: object
      required:
        - reviewId
        - username
        - comment
        - innovation
        - building
        - professorate
        - managementTeam
        - averageScore
        - createdAt
      properties:
        reviewId:
          type: string
          description: Unique identifier for the review
          example: "rev-001"
        username:
          type: string
          description: Name or identifier of the reviewer
          example: "ParentJohn"
        comment:
          type: string
          description: Text review/feedback
          example: "Excellent facilities and dedicated teachers"
        innovation:
          type: number
          format: double
          description: Innovation score (0-5)
          minimum: 0
          maximum: 5
          example: 4.5
        building:
          type: number
          format: double
          description: Building/facilities score (0-5)
          minimum: 0
          maximum: 5
          example: 4.8
        professorate:
          type: number
          format: double
          description: Teaching staff quality score (0-5)
          minimum: 0
          maximum: 5
          example: 4.2
        managementTeam:
          type: number
          format: double
          description: School management score (0-5)
          minimum: 0
          maximum: 5
          example: 4.0
        averageScore:
          type: number
          format: double
          description: Average of the four dimension scores
          minimum: 0
          maximum: 5
          example: 4.38
        createdAt:
          type: string
          format: date-time
          description: Timestamp when the review was created
          example: "2026-03-15T10:30:00Z"

    PaginationMetadata:
      type: object
      required:
        - totalCount
        - page
        - pageSize
        - totalPages
      properties:
        totalCount:
          type: integer
          description: Total number of reviews for this school
          example: 42
        page:
          type: integer
          description: Current page number (1-based)
          example: 1
        pageSize:
          type: integer
          description: Number of items per page
          example: 20
        totalPages:
          type: integer
          description: Total number of pages
          example: 3

    ProblemDetails:
      type: object
      properties:
        type:
          type: string
          example: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        title:
          type: string
          example: "Not Found"
        status:
          type: integer
          example: 404
        detail:
          type: string
          example: "School not found"
```

### 3.2 Arazzo Workflow Definition

**File:** `src/Api/arazzo/get-school-reviews-workflow.yaml`

```yaml
arazzo: 1.0.0
info:
  title: Get School Reviews Workflow
  version: 1.0.0
  description: |
    Workflow for retrieving and browsing paginated reviews for a specific school.
    Demonstrates pagination through multiple pages of reviews.

sourceDescriptions:
  - name: rateYourSchoolAPI
    type: openapi
    url: ../openapi/school-reviews.yaml

workflows:
  - workflowId: browseSchoolReviews
    summary: Browse through all reviews for a school
    description: |
      Retrieves reviews for a specific school with pagination support.
      Demonstrates fetching first page and navigating to subsequent pages.
    inputs:
      type: object
      properties:
        schoolId:
          type: string
          format: uuid
        pageSize:
          type: integer
          default: 20
    steps:
      - stepId: getFirstPage
        description: Retrieve the first page of reviews
        operationId: getSchoolReviews
        parameters:
          - name: schoolId
            in: path
            value: $inputs.schoolId
          - name: page
            in: query
            value: 1
          - name: pageSize
            in: query
            value: $inputs.pageSize
        successCriteria:
          - condition: $statusCode == 200
        outputs:
          firstPageReviews: $response.body.reviews
          totalCount: $response.body.pagination.totalCount
          totalPages: $response.body.pagination.totalPages

      - stepId: getSecondPage
        description: Retrieve the second page of reviews if available
        operationId: getSchoolReviews
        dependsOn: getFirstPage
        parameters:
          - name: schoolId
            in: path
            value: $inputs.schoolId
          - name: page
            in: query
            value: 2
          - name: pageSize
            in: query
            value: $inputs.pageSize
        successCriteria:
          - condition: $statusCode == 200
          - condition: $steps.getFirstPage.outputs.totalPages >= 2
        outputs:
          secondPageReviews: $response.body.reviews

    outputs:
      allRetrievedReviews:
        value: |
          {
            "firstPage": $steps.getFirstPage.outputs.firstPageReviews,
            "secondPage": $steps.getSecondPage.outputs.secondPageReviews,
            "totalCount": $steps.getFirstPage.outputs.totalCount
          }
```

---

## 4. Backend Implementation

### 4.1 Project Structure

Create the following vertical slice structure:

```
src/Api/Endpoints/GetSchoolReviews/
├── Endpoint.cs
├── Handler.cs
├── ReadonlyRepository.cs
├── Request.cs
├── Response.cs
├── ReviewViewModel.cs
├── PaginationMetadata.cs
└── ServiceCollectionExtensions.cs
```

### 4.2 Implementation Files

#### Request.cs

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Request model for retrieving paginated school reviews.
/// Business Rules: BR-024, BR-025, BR-026, BR-027
/// </summary>
internal sealed record Request
{
    /// <summary>
    /// Unique identifier of the school (GUID format)
    /// </summary>
    public required string SchoolId { get; init; }

    /// <summary>
    /// Page number (1-based). Values less than 1 are corrected to 1. (BR-026)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page. Clamped to [1, 100]. (BR-027)
    /// Default: 20 (BR-025)
    /// </summary>
    public int PageSize { get; init; } = 20;
}
```

#### ReviewViewModel.cs

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// View model representing a single school review with all dimension scores.
/// Business Rules: BR-036 (rating dimensions)
/// </summary>
internal sealed record ReviewViewModel
{
    public required string ReviewId { get; init; }
    public required string Username { get; init; }
    public required string Comment { get; init; }
    public required decimal Innovation { get; init; }
    public required decimal Building { get; init; }
    public required decimal Professorate { get; init; }
    public required decimal ManagementTeam { get; init; }
    
    /// <summary>
    /// Average score calculated from the four dimension scores
    /// </summary>
    public required decimal AverageScore { get; init; }
    
    public required DateTime CreatedAt { get; init; }
}
```

#### PaginationMetadata.cs

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Pagination metadata for response envelope.
/// Business Rules: BR-024, BR-028
/// </summary>
internal sealed record PaginationMetadata
{
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
```

#### Response.cs

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Response model containing paginated school reviews.
/// </summary>
internal sealed record Response
{
    public required IEnumerable<ReviewViewModel> Reviews { get; init; }
    public required PaginationMetadata Pagination { get; init; }
}
```

#### ReadonlyRepository.cs

```csharp
using MongoDB.Driver;
using RateYourSchool.Domain.Entities;

namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Repository interface for read-only review queries.
/// </summary>
internal interface IReadonlyRepository
{
    Task<bool> SchoolExistsAsync(string schoolId, CancellationToken cancellationToken);
    Task<IEnumerable<UserReview>> GetReviewsBySchoolIdAsync(
        string schoolId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken);
    Task<long> GetTotalReviewCountAsync(string schoolId, CancellationToken cancellationToken);
}

/// <summary>
/// MongoDB implementation of review repository.
/// Queries the UserReview write model filtered by schoolId.
/// </summary>
internal sealed class MongoDbReadonlyRepository : IReadonlyRepository
{
    private readonly IMongoCollection<School> _schoolsCollection;
    private readonly IMongoCollection<UserReview> _reviewsCollection;
    private readonly ILogger<MongoDbReadonlyRepository> _logger;

    public MongoDbReadonlyRepository(
        IMongoDatabase database,
        ILogger<MongoDbReadonlyRepository> logger)
    {
        _schoolsCollection = database.GetCollection<School>("schools");
        _reviewsCollection = database.GetCollection<UserReview>("userReviews");
        _logger = logger;
    }

    public async Task<bool> SchoolExistsAsync(string schoolId, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<School>.Filter.Eq(s => s.Id, schoolId);
            var count = await _schoolsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking school existence for {SchoolId}", schoolId);
            throw;
        }
    }

    public async Task<IEnumerable<UserReview>> GetReviewsBySchoolIdAsync(
        string schoolId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            
            var filter = Builders<UserReview>.Filter.Eq(r => r.SchoolId, schoolId);
            var sort = Builders<UserReview>.Sort.Descending(r => r.CreatedAt);

            var reviews = await _reviewsCollection
                .Find(filter)
                .Sort(sort)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} reviews for school {SchoolId} (page {Page}, size {PageSize})",
                reviews.Count,
                schoolId,
                page,
                pageSize);

            return reviews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for school {SchoolId}", schoolId);
            throw;
        }
    }

    public async Task<long> GetTotalReviewCountAsync(string schoolId, CancellationToken cancellationToken)
    {
        try
        {
            var filter = Builders<UserReview>.Filter.Eq(r => r.SchoolId, schoolId);
            var count = await _reviewsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Total review count for school {SchoolId}: {Count}",
                schoolId,
                count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting reviews for school {SchoolId}", schoolId);
            throw;
        }
    }
}
```

#### Handler.cs

```csharp
using RateYourSchool.Api.Endpoints;

namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Handler for retrieving paginated school reviews.
/// Business Rules: BR-024 through BR-028, BR-036 through BR-040
/// </summary>
internal sealed class Handler : IHandler<Request, Response>
{
    private readonly IReadonlyRepository _repository;
    private readonly ILogger<Handler> _logger;

    public Handler(
        IReadonlyRepository repository,
        ILogger<Handler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Response> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate school ID format
        if (!Guid.TryParse(request.SchoolId, out _))
        {
            _logger.LogWarning("Invalid school ID format: {SchoolId}", request.SchoolId);
            throw new ArgumentException("Invalid School ID format", nameof(request.SchoolId));
        }

        // Check if school exists
        var schoolExists = await _repository.SchoolExistsAsync(request.SchoolId, cancellationToken);
        if (!schoolExists)
        {
            _logger.LogWarning("School not found: {SchoolId}", request.SchoolId);
            throw new KeyNotFoundException($"School not found: {request.SchoolId}");
        }

        // BR-026: Correct invalid page numbers to 1
        var page = request.Page < 1 ? 1 : request.Page;
        
        // BR-027: Clamp page size to valid range [1, 100]
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        if (page != request.Page)
        {
            _logger.LogWarning(
                "Invalid page number {RequestedPage} corrected to {CorrectedPage}",
                request.Page,
                page);
        }

        if (pageSize != request.PageSize)
        {
            _logger.LogWarning(
                "Page size {RequestedSize} clamped to {ClampedSize}",
                request.PageSize,
                pageSize);
        }

        _logger.LogInformation(
            "Retrieving reviews for school {SchoolId}: page={Page}, pageSize={PageSize}",
            request.SchoolId,
            page,
            pageSize);

        // Get total count for pagination metadata
        var totalCount = await _repository.GetTotalReviewCountAsync(request.SchoolId, cancellationToken);

        // Calculate total pages
        var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;

        // Retrieve reviews
        var reviews = await _repository.GetReviewsBySchoolIdAsync(
            request.SchoolId,
            page,
            pageSize,
            cancellationToken);

        // Map to view models with calculated average score
        var reviewViewModels = reviews.Select(r => new ReviewViewModel
        {
            ReviewId = r.Id,
            Username = r.Username,
            Comment = r.Comment,
            Innovation = r.Innovation,
            Building = r.Building,
            Professorate = r.Professorate,
            ManagementTeam = r.ManagementTeam,
            AverageScore = CalculateAverageScore(r.Innovation, r.Building, r.Professorate, r.ManagementTeam),
            CreatedAt = r.CreatedAt
        });

        var paginationMetadata = new PaginationMetadata
        {
            TotalCount = (int)totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return new Response
        {
            Reviews = reviewViewModels,
            Pagination = paginationMetadata
        };
    }

    /// <summary>
    /// Calculate average score from four dimensions, rounded to 2 decimal places.
    /// </summary>
    private static decimal CalculateAverageScore(
        decimal innovation,
        decimal building,
        decimal professorate,
        decimal managementTeam)
    {
        var average = (innovation + building + professorate + managementTeam) / 4m;
        return Math.Round(average, 2);
    }
}
```

#### Endpoint.cs

```csharp
using Microsoft.AspNetCore.Mvc;

namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Endpoint for retrieving paginated school reviews.
/// </summary>
internal static class GetSchoolReviewsEndpoint
{
    public static IEndpointRouteBuilder MapGetSchoolReviewsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("api/v1/schools/{schoolId}/reviews", async (
            string schoolId,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken,
            IHandler<Request, Response> handler) =>
        {
            try
            {
                var request = new Request
                {
                    SchoolId = schoolId,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };

                var response = await handler.HandleAsync(request, cancellationToken);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request");
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found");
            }
        })
        .WithName("GetSchoolReviews")
        .WithTags("Reviews")
        .WithOpenApi()
        .Produces<Response>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
```

#### ServiceCollectionExtensions.cs

```csharp
namespace RateYourSchool.Api.Endpoints.GetSchoolReviews;

/// <summary>
/// Dependency injection registration for GetSchoolReviews endpoint.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGetSchoolReviewsEndpoint(this IServiceCollection services)
    {
        services.AddScoped<IHandler<Request, Response>, Handler>();
        services.AddScoped<IReadonlyRepository, MongoDbReadonlyRepository>();
        
        return services;
    }
}
```

### 4.3 Program.cs Registration

Add to `src/Api/Program.cs`:

```csharp
// Service registration (in ConfigureServices section)
builder.Services.AddGetSchoolReviewsEndpoint();

// Endpoint mapping (after app is built)
app.MapGetSchoolReviewsEndpoint();
```

### 4.4 Domain Entity (UserReview)

**Note:** If UserReview entity doesn't exist yet, add to `src/Domain/Entities/UserReview.cs`:

```csharp
namespace RateYourSchool.Domain.Entities;

/// <summary>
/// UserReview write model entity.
/// Business Rules: BR-036 through BR-040
/// </summary>
public class UserReview : Entity, IEntity
{
    public required string SchoolId { get; init; }
    public required string Username { get; init; }
    public required string Comment { get; init; }
    public required decimal Innovation { get; init; }
    public required decimal Building { get; init; }
    public required decimal Professorate { get; init; }
    public required decimal ManagementTeam { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

---

## 5. Unit Tests Implementation

### 5.1 Test Project Structure

Create tests in `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolReviews/`:

```
HandlerTests.cs
ReadonlyRepositoryTests.cs
```

### 5.2 Handler Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/GetSchoolReviews/HandlerTests.cs`

```csharp
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RateYourSchool.Api.Endpoints.GetSchoolReviews;
using RateYourSchool.Domain.Entities;
using Xunit;

namespace RateYourSchool.Tests.Unit.Endpoints.GetSchoolReviews;

public class HandlerTests
{
    private readonly Mock<IReadonlyRepository> _mockRepository;
    private readonly Mock<ILogger<Handler>> _mockLogger;
    private readonly Handler _handler;

    public HandlerTests()
    {
        _mockRepository = new Mock<IReadonlyRepository>();
        _mockLogger = new Mock<ILogger<Handler>>();
        _handler = new Handler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldReturnReviews()
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = 1,
            PageSize = 20
        };

        var testReviews = CreateTestReviews(schoolId, 5);
        
        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetTotalReviewCountAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _mockRepository.Setup(r => r.GetReviewsBySchoolIdAsync(schoolId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testReviews);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Reviews.Should().HaveCount(5);
        result.Pagination.TotalCount.Should().Be(5);
        result.Pagination.Page.Should().Be(1);
        result.Pagination.PageSize.Should().Be(20);
        result.Pagination.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidSchoolId_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new Request
        {
            SchoolId = "invalid-guid",
            Page = 1,
            PageSize = 20
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentSchool_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = 1,
            PageSize = 20
        };

        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-10, 1)]
    public async Task HandleAsync_WithInvalidPage_ShouldCorrectToOne(int invalidPage, int expectedPage)
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = invalidPage,
            PageSize = 20
        };

        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetTotalReviewCountAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetReviewsBySchoolIdAsync(schoolId, expectedPage, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserReview>());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Pagination.Page.Should().Be(expectedPage);
        _mockRepository.Verify(r => 
            r.GetReviewsBySchoolIdAsync(schoolId, expectedPage, 20, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(150, 100)]
    [InlineData(1000, 100)]
    public async Task HandleAsync_WithInvalidPageSize_ShouldClamp(int invalidPageSize, int expectedPageSize)
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = 1,
            PageSize = invalidPageSize
        };

        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetTotalReviewCountAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetReviewsBySchoolIdAsync(schoolId, 1, expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserReview>());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Pagination.PageSize.Should().Be(expectedPageSize);
        _mockRepository.Verify(r => 
            r.GetReviewsBySchoolIdAsync(schoolId, 1, expectedPageSize, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateAverageScoreCorrectly()
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = 1,
            PageSize = 20
        };

        var review = new UserReview
        {
            Id = "rev-1",
            SchoolId = schoolId,
            Username = "TestUser",
            Comment = "Test comment",
            Innovation = 4.5m,
            Building = 4.0m,
            Professorate = 5.0m,
            ManagementTeam = 4.2m,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetTotalReviewCountAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockRepository.Setup(r => r.GetReviewsBySchoolIdAsync(schoolId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { review });

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        var returnedReview = result.Reviews.First();
        var expectedAverage = Math.Round((4.5m + 4.0m + 5.0m + 4.2m) / 4m, 2);
        returnedReview.AverageScore.Should().Be(expectedAverage);
    }

    [Fact]
    public async Task HandleAsync_WithNoReviews_ShouldReturnEmptyList()
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = 1,
            PageSize = 20
        };

        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetTotalReviewCountAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetReviewsBySchoolIdAsync(schoolId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserReview>());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Reviews.Should().BeEmpty();
        result.Pagination.TotalCount.Should().Be(0);
        result.Pagination.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange
        var schoolId = Guid.NewGuid().ToString();
        var request = new Request
        {
            SchoolId = schoolId,
            Page = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.SchoolExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetTotalReviewCountAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _mockRepository.Setup(r => r.GetReviewsBySchoolIdAsync(schoolId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestReviews(schoolId, 10));

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Pagination.TotalPages.Should().Be(5); // Ceiling(42 / 10)
    }

    private static List<UserReview> CreateTestReviews(string schoolId, int count)
    {
        var reviews = new List<UserReview>();
        for (int i = 0; i < count; i++)
        {
            reviews.Add(new UserReview
            {
                Id = $"rev-{i + 1}",
                SchoolId = schoolId,
                Username = $"User{i + 1}",
                Comment = $"Comment {i + 1}",
                Innovation = 4.0m + (i * 0.1m),
                Building = 4.0m,
                Professorate = 4.0m,
                ManagementTeam = 4.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        return reviews;
    }
}
```

---

## 6. Integration Tests Implementation

### 6.1 Feature File

**File:** `src/Tests/RateYourSchool.Tests.Integration/Features/GetSchoolReviews.feature`

```gherkin
Feature: Get School Reviews (Paginated)
  As an anonymous user
  I want to retrieve a paginated list of reviews for a specific school
  So that I can read detailed feedback and ratings from other users

  Background:
    Given the following schools exist in the database:
      | SchoolId                             | Name                  | City    | Province |
      | 550e8400-e29b-41d4-a716-446655440001 | Maple Leaf Elementary | Toronto | ON       |
      | 550e8400-e29b-41d4-a716-446655440002 | Oak Grove High        | Toronto | ON       |

  Scenario: Retrieve first page with default page size
    Given the following reviews exist for school "550e8400-e29b-41d4-a716-446655440001":
      | Username     | Comment                  | Innovation | Building | Professorate | ManagementTeam | CreatedAt  |
      | ParentJohn   | Excellent facilities     | 4.5        | 4.8      | 4.2          | 4.0            | 2026-03-15 |
      | TeacherSarah | Great learning environment| 4.0        | 4.5      | 5.0          | 4.5            | 2026-03-14 |
      | StudentMike  | Modern campus             | 3.8        | 5.0      | 4.0          | 3.5            | 2026-03-13 |
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status code should be 200
    And the response should contain 3 reviews
    And the pagination metadata should show totalCount 3, page 1, pageSize 20, totalPages 1

  Scenario: Reviews ordered by creation date descending
    Given the following reviews exist for school "550e8400-e29b-41d4-a716-446655440001":
      | Username     | Comment      | Innovation | Building | Professorate | ManagementTeam | CreatedAt  |
      | User1        | First review | 4.0        | 4.0      | 4.0          | 4.0            | 2026-03-01 |
      | User2        | Second review| 4.0        | 4.0      | 4.0          | 4.0            | 2026-03-10 |
      | User3        | Third review | 4.0        | 4.0      | 4.0          | 4.0            | 2026-03-05 |
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status code should be 200
    And the reviews should be ordered with "User2" first, "User3" second, "User1" third

  Scenario: Average score calculated correctly
    Given the following reviews exist for school "550e8400-e29b-41d4-a716-446655440001":
      | Username   | Comment    | Innovation | Building | Professorate | ManagementTeam | CreatedAt  |
      | TestUser   | Test review| 4.5        | 4.0      | 5.0          | 4.2            | 2026-03-15 |
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews"
    Then the response status code should be 200
    And the review by "TestUser" should have averageScore 4.43

  Scenario: School not found
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-999999999999/reviews"
    Then the response status code should be 404
    And the response should contain error "School not found"

  Scenario: Invalid school ID format
    When I send a GET request to "/api/v1/schools/invalid-id/reviews"
    Then the response status code should be 400
    And the response should contain error "Invalid School ID format"

  Scenario: School with no reviews
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440002/reviews"
    Then the response status code should be 200
    And the response should contain 0 reviews
    And the pagination metadata should show totalCount 0, totalPages 0

  Scenario: Pagination with custom page size
    Given school "550e8400-e29b-41d4-a716-446655440001" has 25 reviews
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=2&pageSize=10"
    Then the response status code should be 200
    And the response should contain 10 reviews
    And the pagination metadata should show totalCount 25, page 2, pageSize 10, totalPages 3

  Scenario: Invalid page number corrected
    Given the following reviews exist for school "550e8400-e29b-41d4-a716-446655440001":
      | Username | Comment | Innovation | Building | Professorate | ManagementTeam | CreatedAt  |
      | User1    | Review1 | 4.0        | 4.0      | 4.0          | 4.0            | 2026-03-15 |
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?page=0"
    Then the response status code should be 200
    And the pagination metadata should show page 1

  Scenario: Page size clamped to maximum
    Given school "550e8400-e29b-41d4-a716-446655440001" has 10 reviews
    When I send a GET request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440001/reviews?pageSize=200"
    Then the response status code should be 200
    And the pagination metadata should show pageSize 100
```

### 6.2 Step Definitions

**File:** `src/Tests/RateYourSchool.Tests.Integration/StepDefinitions/GetSchoolReviewsSteps.cs`

```csharp
using FluentAssertions;
using ReqnRoll;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RateYourSchool.Tests.Integration.StepDefinitions;

[Binding]
public class GetSchoolReviewsSteps : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private readonly TestWebApplicationFactory _factory;

    public GetSchoolReviewsSteps(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Given(@"the following reviews exist for school ""(.*)"":")]
    public async Task GivenReviewsExistForSchool(string schoolId, Table table)
    {
        foreach (var row in table.Rows)
        {
            var review = new
            {
                Id = Guid.NewGuid().ToString(),
                SchoolId = schoolId,
                Username = row["Username"],
                Comment = row["Comment"],
                Innovation = decimal.Parse(row["Innovation"]),
                Building = decimal.Parse(row["Building"]),
                Professorate = decimal.Parse(row["Professorate"]),
                ManagementTeam = decimal.Parse(row["ManagementTeam"]),
                CreatedAt = DateTime.Parse(row["CreatedAt"])
            };

            await _factory.InsertReviewAsync(review);
        }
    }

    [Given(@"school ""(.*)"" has (.*) reviews")]
    public async Task GivenSchoolHasReviews(string schoolId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var review = new
            {
                Id = Guid.NewGuid().ToString(),
                SchoolId = schoolId,
                Username = $"User{i + 1}",
                Comment = $"Comment {i + 1}",
                Innovation = 4.0m,
                Building = 4.0m,
                Professorate = 4.0m,
                ManagementTeam = 4.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            };

            await _factory.InsertReviewAsync(review);
        }
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendGetRequest(string url)
    {
        _response = await _client.GetAsync(url);
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenResponseStatusCodeShouldBe(int statusCode)
    {
        _response.Should().NotBeNull();
        ((int)_response!.StatusCode).Should().Be(statusCode);
    }

    [Then(@"the response should contain (.*) reviews")]
    public async Task ThenResponseShouldContainReviews(int count)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        response.Should().NotBeNull();
        response!.Reviews.Should().HaveCount(count);
    }

    [Then(@"the pagination metadata should show totalCount (.*), page (.*), pageSize (.*), totalPages (.*)")]
    public async Task ThenPaginationMetadataShouldShow(int totalCount, int page, int pageSize, int totalPages)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Pagination.TotalCount.Should().Be(totalCount);
        response.Pagination.Page.Should().Be(page);
        response.Pagination.PageSize.Should().Be(pageSize);
        response.Pagination.TotalPages.Should().Be(totalPages);
    }

    [Then(@"the reviews should be ordered with ""(.*)"" first, ""(.*)"" second, ""(.*)"" third")]
    public async Task ThenReviewsShouldBeOrdered(string first, string second, string third)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Reviews.Should().HaveCountGreaterThanOrEqualTo(3);
        response.Reviews.ElementAt(0).Username.Should().Be(first);
        response.Reviews.ElementAt(1).Username.Should().Be(second);
        response.Reviews.ElementAt(2).Username.Should().Be(third);
    }

    [Then(@"the review by ""(.*)"" should have averageScore (.*)")]
    public async Task ThenReviewShouldHaveAverageScore(string username, decimal expectedAverage)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        var review = response!.Reviews.FirstOrDefault(r => r.Username == username);
        review.Should().NotBeNull();
        review!.AverageScore.Should().Be(expectedAverage);
    }

    [Then(@"the response should contain error ""(.*)""")]
    public async Task ThenResponseShouldContainError(string errorMessage)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        content.Should().Contain(errorMessage);
    }

    [Then(@"the pagination metadata should show totalCount (.*), totalPages (.*)")]
    public async Task ThenPaginationMetadataShouldShowPartial(int totalCount, int totalPages)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Pagination.TotalCount.Should().Be(totalCount);
        response.Pagination.TotalPages.Should().Be(totalPages);
    }

    [Then(@"the pagination metadata should show page (.*)")]
    public async Task ThenPaginationMetadataShouldShowPage(int page)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Pagination.Page.Should().Be(page);
    }

    [Then(@"the pagination metadata should show pageSize (.*)")]
    public async Task ThenPaginationMetadataShouldShowPageSize(int pageSize)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<GetSchoolReviewsResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        response.Should().NotBeNull();
        response!.Pagination.PageSize.Should().Be(pageSize);
    }

    // DTOs for deserialization
    private class GetSchoolReviewsResponse
    {
        public List<ReviewViewModel> Reviews { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }

    private class ReviewViewModel
    {
        public string ReviewId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public decimal Innovation { get; set; }
        public decimal Building { get; set; }
        public decimal Professorate { get; set; }
        public decimal ManagementTeam { get; set; }
        public decimal AverageScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class PaginationMetadata
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
```

---

## 7. Implementation Checklist

### Backend
- [ ] Create vertical slice folder structure (`src/Api/Endpoints/GetSchoolReviews/`)
- [ ] Implement Request.cs
- [ ] Implement ReviewViewModel.cs
- [ ] Implement PaginationMetadata.cs
- [ ] Implement Response.cs
- [ ] Implement ReadonlyRepository.cs (interface + MongoDB implementation)
- [ ] Implement Handler.cs with business logic
- [ ] Implement Endpoint.cs with route mapping
- [ ] Implement ServiceCollectionExtensions.cs
- [ ] Register services in Program.cs
- [ ] Map endpoint in Program.cs
- [ ] Verify UserReview entity exists in Domain layer
- [ ] Create MongoDB indexes on `userReviews.schoolId` and `userReviews.createdAt`

### Tests
- [ ] Create HandlerTests.cs with unit tests
- [ ] Test valid requests
- [ ] Test invalid school ID format
- [ ] Test non-existent school
- [ ] Test page number validation/correction
- [ ] Test page size clamping
- [ ] Test average score calculation
- [ ] Test empty result sets
- [ ] Create GetSchoolReviews.feature file
- [ ] Implement GetSchoolReviewsSteps.cs
- [ ] Run integration tests
- [ ] Verify code coverage meets 80% minimum

### API Documentation
- [ ] Create OpenAPI schema (`src/Api/openapi/school-reviews.yaml`)
- [ ] Define request/response models
- [ ] Add parameter descriptions
- [ ] Add response examples
- [ ] Create Arazzo workflow (`src/Api/arazzo/get-school-reviews-workflow.yaml`)
- [ ] Test API documentation rendering

### Frontend (Future)
- [ ] Create ReviewList component
- [ ] Implement pagination controls
- [ ] Add loading states
- [ ] Add error handling
- [ ] Style review cards
- [ ] Add review filtering/sorting options

---

## 8. Notes for Subagent Implementation

### Implementation Order

1. **Domain Layer** (if needed)
   - Verify UserReview entity exists
   - Add if missing

2. **Backend Implementation**
   - Create folder structure
   - Implement DTOs (Request, Response, ViewModels)
   - Implement Repository
   - Implement Handler with business logic
   - Implement Endpoint
   - Register in Program.cs

3. **Unit Tests**
   - Test all handler logic
   - Test edge cases
   - Verify code coverage

4. **Integration Tests**
   - Implement BDD scenarios
   - Test full request/response cycle
   - Verify database interactions

5. **API Documentation**
   - OpenAPI schema
   - Arazzo workflows
   - Examples

### Key Implementation Details

**Pagination Logic:**
```csharp
var page = request.Page < 1 ? 1 : request.Page;  // BR-026
var pageSize = Math.Clamp(request.PageSize, 1, 100);  // BR-027
var skip = (page - 1) * pageSize;
var totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
```

**Average Score Calculation:**
```csharp
var average = (innovation + building + professorate + managementTeam) / 4m;
return Math.Round(average, 2);
```

**MongoDB Query:**
```csharp
var filter = Builders<UserReview>.Filter.Eq(r => r.SchoolId, schoolId);
var sort = Builders<UserReview>.Sort.Descending(r => r.CreatedAt);

var reviews = await _reviewsCollection
    .Find(filter)
    .Sort(sort)
    .Skip(skip)
    .Limit(pageSize)
    .ToListAsync(cancellationToken);
```

### Error Handling

- **ArgumentException**: Invalid GUID format (400 Bad Request)
- **KeyNotFoundException**: School doesn't exist (404 Not Found)
- **Exception**: Generic errors (500 Internal Server Error)

### Performance Considerations

- Create compound index: `{ schoolId: 1, createdAt: -1 }`
- Limit page size to 100 to prevent large data transfers
- Use projections if only specific fields are needed
- Consider caching for frequently accessed schools

### Business Rules to Enforce

- BR-024: Pagination support
- BR-025: Default pagination values (page=1, pageSize=20)
- BR-026: Page validation (correct to 1 if < 1)
- BR-027: Page size constraints (clamp to [1,100])
- BR-028: Pagination metadata in response
- BR-029: Response time < 200ms
- BR-036: Review data structure
- BR-040: Reviews ordered by date descending

---

**Document Version:** 1.0  
**Created:** 2026-03-18  
**Last Updated:** 2026-03-18
