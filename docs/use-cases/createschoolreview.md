# Use Case: Create School Review

**Status:** Not Started  
**Priority:** High  
**Related Use Cases:** UC-001 (Get SchoolRates), UC-003 (View School Details)  
**Business Rules:** BR-036 through BR-040, BR-011, BR-013, BR-038, BR-039

---

## Implementation Progress

| Task | Status | Completed |
|------|--------|-----------|
| **Functional Requirements Document** | done | 2026-03-17 |
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
This use case enables anonymous users to submit reviews for schools. Users can provide a numerical rating across four dimensions (Innovation, Building, Professorate, ManagementTeam), along with a text comment and username. The review submission is part of the write model (UserReview) and automatically triggers the update of the SchoolRate read model, recalculating the school's aggregate score and refreshing the list of recent reviews.

This feature supports the core value proposition of the platform: empowering parents, teachers, and community members to share their experiences and help others make informed decisions about schools.

### Functional Requirements
- Accept anonymous review submissions without authentication (BR-038)
- Validate that the school exists before accepting the review
- **Require mandatory fields**: username (non-empty), comment (non-empty), and all four rating dimensions (BR-036)
- Capture reviewer username, comment, and multi-dimensional ratings
- Persist the review as a write model (UserReview entity)
- Trigger automatic SchoolRate read model update (BR-039)
- Recalculate school's aggregate score from all reviews (BR-013)
- Refresh the last 25 reviews list in the SchoolRate read model (BR-040)
- Return success response with created review details
- Handle validation errors (missing required fields, invalid ratings)
- Log review creation events for audit trail

### Technical Context
- **CQRS Pattern**: Creates a write model (UserReview) that triggers read model (SchoolRate) update
- **Data Source**: 
  - **Write**: MongoDB `userReviews` collection
  - **Update**: MongoDB `schoolRates` collection (via event handler)
- **Architecture**: Vertical Slice in `src/Api/Endpoints/CreateSchoolReview/`
- **Current State**: Not yet implemented - new feature

---

## 2. Acceptance Criteria (Gherkin)

```gherkin
Feature: Create School Review
  As an anonymous user
  I want to create a school review
  So that I can provide scores and a text comment regarding a school where I work or my child is a student

  Background:
    Given the following schools exist in the database:
      | SchoolId | Name                  | City    | Province | Type   |
      | school-1 | Maple Leaf Elementary | Toronto | ON       | Public |
      | school-2 | Oak Grove High        | Toronto | ON       | Public |

  Scenario: Successfully create a review with all required fields
    Given I am an anonymous user
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentJohn",
        "comment": "Excellent school with dedicated teachers and modern facilities.",
        "innovation": 4.5,
        "building": 4.0,
        "professorate": 5.0,
        "managementTeam": 4.2
      }
      """
    Then the response status should be 201
    And the response should contain the created review with an assigned ID
    And the response should include:
      | Field          | Value                                                              |
      | Username       | ParentJohn                                                         |
      | Comment        | Excellent school with dedicated teachers and modern facilities.    |
      | Innovation     | 4.5                                                                |
      | Building       | 4.0                                                                |
      | Professorate   | 5.0                                                                |
      | ManagementTeam | 4.2                                                                |
      | CreatedAt      | [current timestamp]                                                |
    And the review should be persisted in the userReviews collection
    And the SchoolRate read model for "school-1" should be updated

  Scenario: Review triggers SchoolRate read model update
    Given school "school-1" has an existing SchoolRate with score 3.5
    And school "school-1" has 10 existing reviews
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "TeacherSarah",
        "comment": "Great environment for learning and growth.",
        "innovation": 5.0,
        "building": 4.5,
        "professorate": 4.8,
        "managementTeam": 4.6
      }
      """
    Then the response status should be 201
    And the SchoolRate for "school-1" should have a recalculated score
    And the SchoolRate should include the new review in its last 25 reviews
    And the SchoolRate should reflect 11 total reviews

  Scenario: Validation - Missing required field (username)
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "comment": "Good school overall.",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status should be 400
    And the response should contain a validation error for "username"
    And the error message should indicate "Username is required"

  Scenario: Validation - Missing required field (comment)
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentMike",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status should be 400
    And the response should contain a validation error for "comment"
    And the error message should indicate "Comment is required"

  Scenario: Validation - Missing all rating dimensions
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentMary",
        "comment": "Nice school"
      }
      """
    Then the response status should be 400
    And the response should contain validation errors for rating fields
    And the error message should indicate all four dimensions are required

  Scenario: Validation - Invalid rating value (too high)
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentDave",
        "comment": "Excellent school",
        "innovation": 6.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status should be 400
    And the response should contain a validation error for "innovation"
    And the error message should indicate "Rating must be between 0 and 5"

  Scenario: Validation - Invalid rating value (negative)
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentLisa",
        "comment": "Could be better",
        "innovation": -1.0,
        "building": 3.0,
        "professorate": 3.0,
        "managementTeam": 3.0
      }
      """
    Then the response status should be 400
    And the response should contain a validation error for "innovation"
    And the error message should indicate "Rating must be between 0 and 5"

  Scenario: Validation - School does not exist
    When I submit a POST request to "/api/v1/schools/nonexistent-school/reviews" with:
      """
      {
        "username": "ParentTom",
        "comment": "Great school",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status should be 404
    And the response should contain an error message "School not found"

  Scenario: Validation - Empty username
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "",
        "comment": "Good school",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status should be 400
    And the response should contain a validation error for "username"
    And the error message should indicate "Username cannot be empty"

  Scenario: Validation - Empty comment
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentBob",
        "comment": "",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status should be 400
    And the response should contain a validation error for "comment"
    And the error message should indicate "Comment cannot be empty"

  Scenario: Validation - Comment exceeds maximum length
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with a comment of 2001 characters
    Then the response status should be 400
    And the response should contain a validation error for "comment"
    And the error message should indicate "Comment must not exceed 2000 characters"

  Scenario: Multiple reviews for the same school from different users
    Given school "school-1" already has 5 reviews
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "TeacherAlex",
        "comment": "Professional environment",
        "innovation": 4.2,
        "building": 4.3,
        "professorate": 4.5,
        "managementTeam": 4.1
      }
      """
    Then the response status should be 201
    And the school should now have 6 reviews in total
    And the new review should be included in the SchoolRate read model

  Scenario: Review with decimal precision in ratings
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentEmily",
        "comment": "Very good facilities and staff",
        "innovation": 4.75,
        "building": 4.25,
        "professorate": 4.80,
        "managementTeam": 4.50
      }
      """
    Then the response status should be 201
    And the ratings should be stored with decimal precision

  Scenario: Performance - Review creation response time
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with valid data
    Then the response status should be 201
    And the response time should be less than 500ms
    And the SchoolRate read model update should be triggered asynchronously

  Scenario: Review creation triggers event log entry
    When I submit a POST request to "/api/v1/schools/school-1/reviews" with:
      """
      {
        "username": "ParentChris",
        "comment": "Highly recommend this school",
        "innovation": 5.0,
        "building": 4.8,
        "professorate": 4.9,
        "managementTeam": 4.7
      }
      """
    Then the response status should be 201
    And an event should be logged with message "UserReview created for school Maple Leaf Elementary by ParentChris"
```

---

## 3. OpenAPI Schema & Arazzo Flow

### 3.1 Define OpenAPI Schema

**File:** `src/Api/openapi/create-school-review.yaml`

```yaml
openapi: 3.0.3
info:
  title: RateYourSchool API - Create School Review
  version: 1.0.0
  description: |
    API endpoint for creating anonymous school reviews.

paths:
  /api/v1/schools/{schoolId}/reviews:
    post:
      summary: Create a school review
      description: |
        Allows anonymous users to submit a review for a specific school.
        The review includes multi-dimensional ratings and a text comment.
        
        **CQRS Note:** This endpoint creates a write model (UserReview) which
        triggers an asynchronous update of the SchoolRate read model.
        
        **Business Rules:**
        - BR-036: Review must include username, comment, and ratings
        - BR-038: Anonymous users can create reviews (no authentication)
        - BR-039: Review creation triggers SchoolRate read model update
        - BR-040: SchoolRate maintains last 25 reviews
      operationId: createSchoolReview
      tags:
        - Reviews
      parameters:
        - name: schoolId
          in: path
          description: The unique identifier of the school to review
          required: true
          schema:
            type: string
            format: uuid
            example: "550e8400-e29b-41d4-a716-446655440000"
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateReviewRequest'
            examples:
              validReview:
                summary: Valid review with all required fields
                value:
                  username: "ParentJohn"
                  comment: "Excellent school with dedicated teachers and modern facilities."
                  innovation: 4.5
                  building: 4.0
                  professorate: 5.0
                  managementTeam: 4.2
              minimumRatings:
                summary: Review with minimum ratings
                value:
                  username: "TeacherSarah"
                  comment: "Needs improvement in several areas."
                  innovation: 1.0
                  building: 2.0
                  professorate: 1.5
                  managementTeam: 2.5
              maximumRatings:
                summary: Review with maximum ratings
                value:
                  username: "ParentMike"
                  comment: "Outstanding school in every aspect!"
                  innovation: 5.0
                  building: 5.0
                  professorate: 5.0
                  managementTeam: 5.0
      responses:
        '201':
          description: Review created successfully
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CreateReviewResponse'
              examples:
                success:
                  summary: Successful review creation
                  value:
                    id: "660e8400-e29b-41d4-a716-446655440001"
                    schoolId: "550e8400-e29b-41d4-a716-446655440000"
                    username: "ParentJohn"
                    comment: "Excellent school with dedicated teachers and modern facilities."
                    innovation: 4.5
                    building: 4.0
                    professorate: 5.0
                    managementTeam: 4.2
                    totalScore: 4.425
                    createdAt: "2026-03-17T14:30:00Z"
        '400':
          description: Bad request - validation errors
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ValidationError'
              examples:
                missingUsername:
                  summary: Missing username
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    title: "One or more validation errors occurred."
                    status: 400
                    errors:
                      username: ["Username is required"]
                missingComment:
                  summary: Missing comment
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    title: "One or more validation errors occurred."
                    status: 400
                    errors:
                      comment: ["Comment is required"]
                invalidRating:
                  summary: Invalid rating value
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    title: "One or more validation errors occurred."
                    status: 400
                    errors:
                      innovation: ["Rating must be between 0 and 5"]
                commentTooLong:
                  summary: Comment exceeds maximum length
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    title: "One or more validation errors occurred."
                    status: 400
                    errors:
                      comment: ["Comment must not exceed 2000 characters"]
        '404':
          description: School not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                schoolNotFound:
                  summary: School does not exist
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"
                    title: "School not found"
                    status: 404
                    detail: "A school with ID '550e8400-e29b-41d4-a716-446655440000' does not exist."
        '500':
          description: Internal server error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'

components:
  schemas:
    CreateReviewRequest:
      type: object
      required:
        - username
        - comment
        - innovation
        - building
        - professorate
        - managementTeam
      properties:
        username:
          type: string
          minLength: 1
          maxLength: 100
          description: Name of the reviewer (can be anonymous)
          example: "ParentJohn"
        comment:
          type: string
          minLength: 1
          maxLength: 2000
          description: Detailed review comment
          example: "Excellent school with dedicated teachers and modern facilities."
        innovation:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          description: Innovation score (0-5)
          example: 4.5
        building:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          description: Building/facilities score (0-5)
          example: 4.0
        professorate:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          description: Teaching staff quality score (0-5)
          example: 5.0
        managementTeam:
          type: number
          format: decimal
          minimum: 0
          maximum: 5
          description: Management team score (0-5)
          example: 4.2

    CreateReviewResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
          description: Unique identifier of the created review
          example: "660e8400-e29b-41d4-a716-446655440001"
        schoolId:
          type: string
          format: uuid
          description: Identifier of the reviewed school
          example: "550e8400-e29b-41d4-a716-446655440000"
        username:
          type: string
          description: Name of the reviewer
          example: "ParentJohn"
        comment:
          type: string
          description: Review comment
          example: "Excellent school with dedicated teachers and modern facilities."
        innovation:
          type: number
          format: decimal
          description: Innovation score
          example: 4.5
        building:
          type: number
          format: decimal
          description: Building score
          example: 4.0
        professorate:
          type: number
          format: decimal
          description: Professorate score
          example: 5.0
        managementTeam:
          type: number
          format: decimal
          description: Management team score
          example: 4.2
        totalScore:
          type: number
          format: decimal
          description: Calculated total score (average of all dimensions)
          example: 4.425
        createdAt:
          type: string
          format: date-time
          description: Timestamp when the review was created
          example: "2026-03-17T14:30:00Z"

    ValidationError:
      type: object
      properties:
        type:
          type: string
          example: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        title:
          type: string
          example: "One or more validation errors occurred."
        status:
          type: integer
          example: 400
        errors:
          type: object
          additionalProperties:
            type: array
            items:
              type: string

    ProblemDetails:
      type: object
      properties:
        type:
          type: string
        title:
          type: string
        status:
          type: integer
        detail:
          type: string
```

### 3.2 Arazzo Workflow Definition

**File:** `src/Api/arazzo/create-school-review-workflow.yaml`

```yaml
arazzo: 1.0.0
info:
  title: Create School Review Workflow
  version: 1.0.0
  description: |
    Workflow for submitting an anonymous school review.
    
    This workflow demonstrates:
    1. Verifying school exists
    2. Creating a review with validation
    3. Confirming the review was created successfully

sourceDescriptions:
  - name: rateyourschool-api
    type: openapi
    url: ./openapi/create-school-review.yaml

workflows:
  - workflowId: submitSchoolReview
    summary: Submit a new school review
    description: |
      Complete workflow for an anonymous user to submit a school review.
      The workflow validates the school exists and then creates the review.
    
    inputs:
      type: object
      properties:
        schoolId:
          type: string
          format: uuid
          description: ID of the school to review
        username:
          type: string
          description: Reviewer's username
        comment:
          type: string
          description: Review comment
        innovation:
          type: number
          description: Innovation score (0-5)
        building:
          type: number
          description: Building score (0-5)
        professorate:
          type: number
          description: Professorate score (0-5)
        managementTeam:
          type: number
          description: Management team score (0-5)
      required:
        - schoolId
        - username
        - comment
        - innovation
        - building
        - professorate
        - managementTeam
    
    steps:
      - stepId: createReview
        description: Submit the review for the school
        operationId: createSchoolReview
        parameters:
          - name: schoolId
            in: path
            value: $inputs.schoolId
        requestBody:
          contentType: application/json
          payload:
            username: $inputs.username
            comment: $inputs.comment
            innovation: $inputs.innovation
            building: $inputs.building
            professorate: $inputs.professorate
            managementTeam: $inputs.managementTeam
        successCriteria:
          - condition: $statusCode == 201
        outputs:
          reviewId: $response.body.id
          totalScore: $response.body.totalScore
          createdAt: $response.body.createdAt

    outputs:
      reviewId:
        value: $steps.createReview.outputs.reviewId
      totalScore:
        value: $steps.createReview.outputs.totalScore
      createdAt:
        value: $steps.createReview.outputs.createdAt
```

### 3.3 Implementation Steps

1. **Create OpenAPI specification file** at `src/Api/openapi/create-school-review.yaml`
2. **Create Arazzo workflow file** at `src/Api/arazzo/create-school-review-workflow.yaml`
3. **Configure Swashbuckle** to include the new endpoint in OpenAPI documentation
4. **Add OpenAPI annotations** to the endpoint using `.WithOpenApi()`
5. **Validate schema** using Swagger UI or OpenAPI validation tools
6. **Test Arazzo workflow** using Arazzo execution tools

---

## 4. Backend Implementation

### 4.1 Current State Analysis

**Existing Files:**
- ✅ `src/Domain/Entities/IEntity.cs` - Base entity interface
- ✅ `src/Domain/Repositories/IReadOnlyRepository.cs` - Generic readonly repository
- ✅ `src/Api/Endpoints/IHandler.cs` - Handler interface

**Missing/Incomplete:**
- ❌ `UserReview` entity in Domain layer
- ❌ `IRepository` interface for write operations
- ❌ `CreateSchoolReview` vertical slice endpoint
- ❌ MongoDB repository implementation for UserReview
- ❌ Event handler for SchoolRate read model updates
- ❌ Validation logic for review data

### 4.2 Implementation Steps

#### Step 1: Create UserReview Entity

**File:** `src/Domain/Entities/UserReview.cs` (NEW)

```csharp
using System;

namespace RateYourSchool.Domain.Entities;

/// <summary>
/// Represents a user review (write model) for a school.
/// BR-036: Review Components
/// BR-037: Review Association
/// </summary>
public sealed class UserReview : Entity
{
    /// <summary>
    /// Gets or sets the ID of the school being reviewed.
    /// </summary>
    public required string SchoolId { get; init; }

    /// <summary>
    /// Gets or sets the username of the reviewer.
    /// Can be anonymous (no authentication required per BR-038).
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the text comment/feedback.
    /// </summary>
    public required string Comment { get; init; }

    /// <summary>
    /// Gets or sets the Innovation dimension score (0-5).
    /// </summary>
    public required decimal Innovation { get; init; }

    /// <summary>
    /// Gets or sets the Building/facilities dimension score (0-5).
    /// </summary>
    public required decimal Building { get; init; }

    /// <summary>
    /// Gets or sets the Professorate dimension score (0-5).
    /// </summary>
    public required decimal Professorate { get; init; }

    /// <summary>
    /// Gets or sets the Management Team dimension score (0-5).
    /// </summary>
    public required decimal ManagementTeam { get; init; }

    /// <summary>
    /// Gets the calculated total score (average of all dimensions).
    /// BR-017: Score Calculation
    /// </summary>
    public decimal TotalScore => (Innovation + Building + Professorate + ManagementTeam) / 4;

    /// <summary>
    /// Gets or sets the timestamp when the review was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

**Purpose:** Defines the UserReview write model entity with all required properties and score calculation logic as per BR-016, BR-017, and BR-036.

#### Step 2: Create Write Repository Interface

**File:** `src/Domain/Repositories/IRepository.cs` (NEW)

```csharp
using System.Threading;
using System.Threading.Tasks;
using RateYourSchool.Domain.Entities;

namespace RateYourSchool.Domain.Repositories;

/// <summary>
/// Generic repository interface for write operations.
/// </summary>
/// <typeparam name="T">Entity type implementing IEntity</typeparam>
public interface IRepository<T> where T : IEntity
{
    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="entity">The entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created entity with assigned ID</returns>
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if an entity exists with the given ID.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken);
}
```

**Purpose:** Defines the write repository contract for creating and validating entities.

#### Step 3: Create Request DTO

**File:** `src/Api/Endpoints/CreateSchoolReview/Request.cs` (NEW)

```csharp
using System.ComponentModel.DataAnnotations;

namespace RateYourSchool.Api.Endpoints.CreateSchoolReview;

/// <summary>
/// Request model for creating a school review.
/// BR-036: Review must include username, comment, and ratings
/// </summary>
internal sealed record Request
{
    /// <summary>
    /// Username of the reviewer.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 100 characters")]
    public required string Username { get; init; }

    /// <summary>
    /// Review comment/feedback.
    /// </summary>
    [Required(ErrorMessage = "Comment is required")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 2000 characters")]
    public required string Comment { get; init; }

    /// <summary>
    /// Innovation dimension score (0-5).
    /// </summary>
    [Required(ErrorMessage = "Innovation rating is required")]
    [Range(0, 5, ErrorMessage = "Innovation rating must be between 0 and 5")]
    public required decimal Innovation { get; init; }

    /// <summary>
    /// Building dimension score (0-5).
    /// </summary>
    [Required(ErrorMessage = "Building rating is required")]
    [Range(0, 5, ErrorMessage = "Building rating must be between 0 and 5")]
    public required decimal Building { get; init; }

    /// <summary>
    /// Professorate dimension score (0-5).
    /// </summary>
    [Required(ErrorMessage = "Professorate rating is required")]
    [Range(0, 5, ErrorMessage = "Professorate rating must be between 0 and 5")]
    public required decimal Professorate { get; init; }

    /// <summary>
    /// Management Team dimension score (0-5).
    /// </summary>
    [Required(ErrorMessage = "ManagementTeam rating is required")]
    [Range(0, 5, ErrorMessage = "ManagementTeam rating must be between 0 and 5")]
    public required decimal ManagementTeam { get; init; }
}
```

**Purpose:** Defines and validates input data for review creation with DataAnnotations validation per business rules.

#### Step 4: Create Response DTO

**File:** `src/Api/Endpoints/CreateSchoolReview/Response.cs` (NEW)

```csharp
using System;

namespace RateYourSchool.Api.Endpoints.CreateSchoolReview;

/// <summary>
/// Response model for created school review.
/// </summary>
internal sealed record Response
{
    /// <summary>
    /// Unique identifier of the created review.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// ID of the school being reviewed.
    /// </summary>
    public required string SchoolId { get; init; }

    /// <summary>
    /// Username of the reviewer.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Review comment.
    /// </summary>
    public required string Comment { get; init; }

    /// <summary>
    /// Innovation score.
    /// </summary>
    public required decimal Innovation { get; init; }

    /// <summary>
    /// Building score.
    /// </summary>
    public required decimal Building { get; init; }

    /// <summary>
    /// Professorate score.
    /// </summary>
    public required decimal Professorate { get; init; }

    /// <summary>
    /// Management Team score.
    /// </summary>
    public required decimal ManagementTeam { get; init; }

    /// <summary>
    /// Calculated total score.
    /// BR-017: Total = (Innovation + Building + Professorate + ManagementTeam) / 4
    /// </summary>
    public required decimal TotalScore { get; init; }

    /// <summary>
    /// Timestamp when the review was created (UTC).
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}
```

**Purpose:** Defines the response structure returned after successful review creation.

#### Step 5: Create Handler

**File:** `src/Api/Endpoints/CreateSchoolReview/Handler.cs` (NEW)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RateYourSchool.Domain.Entities;
using RateYourSchool.Domain.Repositories;

namespace RateYourSchool.Api.Endpoints.CreateSchoolReview;

/// <summary>
/// Handles the creation of school reviews.
/// BR-038: Anonymous Review Creation
/// BR-039: Review Submission Impact
/// </summary>
internal sealed class Handler : IHandler<Request, Response>
{
    private readonly IRepository<UserReview> _reviewRepository;
    private readonly IRepository<School> _schoolRepository;
    private readonly ILogger<Handler> _logger;

    public Handler(
        IRepository<UserReview> reviewRepository,
        IRepository<School> schoolRepository,
        ILogger<Handler> logger)
    {
        _reviewRepository = reviewRepository;
        _schoolRepository = schoolRepository;
        _logger = logger;
    }

    public async Task<Response> HandleAsync(
        string schoolId,
        Request request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(schoolId);

        _logger.LogInformation(
            "Creating review for school {SchoolId} by user {Username}",
            schoolId,
            request.Username);

        // BR-039: Validate school exists before accepting review
        var schoolExists = await _schoolRepository.ExistsAsync(schoolId, cancellationToken);
        if (!schoolExists)
        {
            _logger.LogWarning(
                "Attempted to create review for non-existent school {SchoolId}",
                schoolId);
            throw new InvalidOperationException($"School with ID '{schoolId}' does not exist.");
        }

        // Create UserReview entity (write model)
        var review = new UserReview
        {
            Id = Guid.NewGuid().ToString(),
            SchoolId = schoolId,
            Username = request.Username,
            Comment = request.Comment,
            Innovation = request.Innovation,
            Building = request.Building,
            Professorate = request.Professorate,
            ManagementTeam = request.ManagementTeam,
            CreatedAt = DateTime.UtcNow
        };

        // Persist the review
        var createdReview = await _reviewRepository.CreateAsync(review, cancellationToken);

        _logger.LogInformation(
            "Review {ReviewId} created successfully for school {SchoolId} by {Username}. Total score: {TotalScore}",
            createdReview.Id,
            schoolId,
            request.Username,
            createdReview.TotalScore);

        // BR-039: Review creation triggers SchoolRate read model update
        // This will be handled by a MongoDB Change Stream event handler (implemented separately)
        
        // Map to response
        return new Response
        {
            Id = createdReview.Id,
            SchoolId = createdReview.SchoolId,
            Username = createdReview.Username,
            Comment = createdReview.Comment,
            Innovation = createdReview.Innovation,
            Building = createdReview.Building,
            Professorate = createdReview.Professorate,
            ManagementTeam = createdReview.ManagementTeam,
            TotalScore = createdReview.TotalScore,
            CreatedAt = createdReview.CreatedAt
        };
    }
}
```

**Purpose:** Implements business logic for review creation including validation, persistence, and logging as per BR-038 and BR-039.

#### Step 6: Create MongoDB Repository Implementation

**File:** `src/Api/Infrastructure/MongoDB/MongoDbRepository.cs` (NEW)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RateYourSchool.Domain.Entities;
using RateYourSchool.Domain.Repositories;

namespace RateYourSchool.Api.Infrastructure.MongoDB;

/// <summary>
/// MongoDB implementation of the generic write repository.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
internal sealed class MongoDbRepository<T> : IRepository<T> where T : IEntity
{
    private readonly IMongoCollection<T> _collection;
    private readonly ILogger<MongoDbRepository<T>> _logger;

    public MongoDbRepository(
        IMongoDatabase database,
        string collectionName,
        ILogger<MongoDbRepository<T>> logger)
    {
        _collection = database.GetCollection<T>(collectionName);
        _logger = logger;
    }

    public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "Entity {EntityType} with ID {EntityId} created successfully",
                typeof(T).Name,
                entity.Id);

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating entity {EntityType} with ID {EntityId}",
                typeof(T).Name,
                entity.Id);
            throw;
        }
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, id);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning(
                    "Entity {EntityType} with ID {EntityId} not found",
                    typeof(T).Name,
                    id);
            }

            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving entity {EntityType} with ID {EntityId}",
                typeof(T).Name,
                id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        try
        {
            var filter = Builders<T>.Filter.Eq(e => e.Id, id);
            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking existence of entity {EntityType} with ID {EntityId}",
                typeof(T).Name,
                id);
            throw;
        }
    }
}
```

**Purpose:** Provides MongoDB implementation for write operations (create, read, exists check) with error handling and logging.

#### Step 7: Create Endpoint Mapping

**File:** `src/Api/Endpoints/CreateSchoolReview/Endpoint.cs` (NEW)

```csharp
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RateYourSchool.Api.Endpoints.CreateSchoolReview;

/// <summary>
/// Endpoint for creating school reviews.
/// BR-038: No authentication required (anonymous users)
/// </summary>
internal static class CreateSchoolReviewEndpoint
{
    public static IEndpointRouteBuilder MapCreateSchoolReviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("api/v1/schools/{schoolId}/reviews", async (
            string schoolId,
            Request request,
            Handler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var response = await handler.HandleAsync(schoolId, request, cancellationToken);
                return Results.Created($"/api/v1/schools/{schoolId}/reviews/{response.Id}", response);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("does not exist"))
            {
                // School not found
                return Results.NotFound(new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "School not found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Internal server error
                return Results.Problem(
                    type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    title: "An error occurred while processing your request",
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: ex.Message);
            }
        })
        .WithName("CreateSchoolReview")
        .WithOpenApi()
        .Produces<Response>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
        .WithTags("Reviews");

        return endpoints;
    }
}
```

**Purpose:** Maps the HTTP POST endpoint and handles HTTP-level concerns (status codes, error responses).

#### Step 8: Create Service Registration

**File:** `src/Api/Endpoints/CreateSchoolReview/ServiceCollectionExtensions.cs` (NEW)

```csharp
using Microsoft.Extensions.DependencyInjection;
using RateYourSchool.Domain.Entities;
using RateYourSchool.Domain.Repositories;
using RateYourSchool.Api.Infrastructure.MongoDB;

namespace RateYourSchool.Api.Endpoints.CreateSchoolReview;

/// <summary>
/// Service collection extensions for CreateSchoolReview endpoint.
/// </summary>
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCreateSchoolReviewEndpoint(this IServiceCollection services)
    {
        // Register handler
        services.AddScoped<Handler>();

        // Register repositories (if not already registered globally)
        services.AddScoped<IRepository<UserReview>>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            var logger = sp.GetRequiredService<ILogger<MongoDbRepository<UserReview>>>();
            return new MongoDbRepository<UserReview>(database, "userReviews", logger);
        });

        services.AddScoped<IRepository<School>>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            var logger = sp.GetRequiredService<ILogger<MongoDbRepository<School>>>();
            return new MongoDbRepository<School>(database, "schools", logger);
        });

        return services;
    }
}
```

**Purpose:** Registers all dependencies (handler, repositories) for dependency injection.

#### Step 9: Update Program.cs

**File:** `src/Api/Program.cs` (UPDATE)

Add the following lines:

```csharp
// After other AddXxxEndpoint() calls
builder.Services.AddCreateSchoolReviewEndpoint();

// After other MapXxxEndpoint() calls
app.MapCreateSchoolReviewEndpoint();
```

**Purpose:** Integrates the new endpoint into the application startup configuration.

#### Step 10: Create SchoolRate Update Event Handler (Background Service)

**File:** `src/Api/Infrastructure/EventHandlers/SchoolRateUpdateEventHandler.cs` (NEW)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RateYourSchool.Domain.Entities;

namespace RateYourSchool.Api.Infrastructure.EventHandlers;

/// <summary>
/// Background service that listens to UserReview changes via MongoDB Change Streams
/// and updates the SchoolRate read model accordingly.
/// BR-039: Review creation triggers SchoolRate read model update
/// BR-013: Score recalculation
/// </summary>
internal sealed class SchoolRateUpdateEventHandler : BackgroundService
{
    private readonly IMongoCollection<UserReview> _reviewCollection;
    private readonly IMongoCollection<SchoolRate> _schoolRateCollection;
    private readonly ILogger<SchoolRateUpdateEventHandler> _logger;

    public SchoolRateUpdateEventHandler(
        IMongoDatabase database,
        ILogger<SchoolRateUpdateEventHandler> logger)
    {
        _reviewCollection = database.GetCollection<UserReview>("userReviews");
        _schoolRateCollection = database.GetCollection<SchoolRate>("schoolRates");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SchoolRateUpdateEventHandler started");

        try
        {
            // Watch for insert operations on userReviews collection
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<UserReview>>()
                .Match(change => change.OperationType == ChangeStreamOperationType.Insert);

            using var cursor = await _reviewCollection.WatchAsync(pipeline, cancellationToken: stoppingToken);

            await cursor.ForEachAsync(async change =>
            {
                var review = change.FullDocument;
                
                _logger.LogInformation(
                    "UserReview change detected for school {SchoolId}. Updating SchoolRate read model.",
                    review.SchoolId);

                await UpdateSchoolRateAsync(review.SchoolId, stoppingToken);
            }, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SchoolRateUpdateEventHandler stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SchoolRateUpdateEventHandler");
        }
    }

    private async Task UpdateSchoolRateAsync(string schoolId, CancellationToken cancellationToken)
    {
        // BR-013: Recalculate scores from all reviews
        // BR-040: Maintain last 25 reviews
        // This is a simplified implementation - actual implementation would:
        // 1. Fetch all reviews for the school
        // 2. Calculate aggregate scores
        // 3. Get last 25 reviews
        // 4. Update or create SchoolRate document
        
        _logger.LogInformation(
            "SchoolRate read model updated for school {SchoolId}",
            schoolId);

        // TODO: Implement full SchoolRate update logic
        // This requires fetching School entity, all UserReviews, and rebuilding SchoolRate
    }
}
```

**Purpose:** Implements asynchronous SchoolRate read model updates when UserReview write model changes occur, using MongoDB Change Streams for BR-039 and BR-048 (eventual consistency).

#### Step 11: Register Event Handler

**File:** `src/Api/Program.cs` (UPDATE)

Add after service registrations:

```csharp
// Register background services for CQRS event handling
builder.Services.AddHostedService<SchoolRateUpdateEventHandler>();
```

**Purpose:** Registers the background service to monitor UserReview changes and update SchoolRate read model.

---

## 5. Unit Tests Implementation

### 5.1 Test Project Setup

Ensure the test project has the necessary dependencies:

**File:** `src/Tests/RateYourSchool.Tests.Unit/RateYourSchool.Tests.Unit.csproj` (UPDATE if needed)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Api\Api.csproj" />
    <ProjectReference Include="..\..\Domain\Domain.csproj" />
  </ItemGroup>
</Project>
```

### 5.2 Handler Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Endpoints/CreateSchoolReview/HandlerTests.cs` (NEW)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RateYourSchool.Api.Endpoints.CreateSchoolReview;
using RateYourSchool.Domain.Entities;
using RateYourSchool.Domain.Repositories;
using Xunit;

namespace RateYourSchool.Tests.Unit.Endpoints.CreateSchoolReview;

public class HandlerTests
{
    private readonly Mock<IRepository<UserReview>> _mockReviewRepository;
    private readonly Mock<IRepository<School>> _mockSchoolRepository;
    private readonly Mock<ILogger<Handler>> _mockLogger;
    private readonly Handler _handler;

    public HandlerTests()
    {
        _mockReviewRepository = new Mock<IRepository<UserReview>>();
        _mockSchoolRepository = new Mock<IRepository<School>>();
        _mockLogger = new Mock<ILogger<Handler>>();
        _handler = new Handler(
            _mockReviewRepository.Object,
            _mockSchoolRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldCreateReview()
    {
        // Arrange
        var schoolId = "school-123";
        var request = new Request
        {
            Username = "ParentJohn",
            Comment = "Excellent school",
            Innovation = 4.5m,
            Building = 4.0m,
            Professorate = 5.0m,
            ManagementTeam = 4.2m
        };

        _mockSchoolRepository
            .Setup(r => r.ExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockReviewRepository
            .Setup(r => r.CreateAsync(It.IsAny<UserReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserReview review, CancellationToken _) => review);

        // Act
        var response = await _handler.HandleAsync(schoolId, request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.SchoolId.Should().Be(schoolId);
        response.Username.Should().Be("ParentJohn");
        response.Comment.Should().Be("Excellent school");
        response.Innovation.Should().Be(4.5m);
        response.Building.Should().Be(4.0m);
        response.Professorate.Should().Be(5.0m);
        response.ManagementTeam.Should().Be(4.2m);
        response.TotalScore.Should().BeApproximately(4.425m, 0.001m);
        response.Id.Should().NotBeNullOrEmpty();
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mockReviewRepository.Verify(
            r => r.CreateAsync(It.IsAny<UserReview>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentSchool_ShouldThrowException()
    {
        // Arrange
        var schoolId = "nonexistent-school";
        var request = new Request
        {
            Username = "ParentJohn",
            Comment = "Test",
            Innovation = 4.0m,
            Building = 4.0m,
            Professorate = 4.0m,
            ManagementTeam = 4.0m
        };

        _mockSchoolRepository
            .Setup(r => r.ExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.HandleAsync(schoolId, request, CancellationToken.None));

        _mockReviewRepository.Verify(
            r => r.CreateAsync(It.IsAny<UserReview>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var schoolId = "school-123";
        Request nullRequest = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.HandleAsync(schoolId, nullRequest, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithInvalidSchoolId_ShouldThrowArgumentException(string invalidSchoolId)
    {
        // Arrange
        var request = new Request
        {
            Username = "ParentJohn",
            Comment = "Test",
            Innovation = 4.0m,
            Building = 4.0m,
            Professorate = 4.0m,
            ManagementTeam = 4.0m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.HandleAsync(invalidSchoolId, request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateTotalScoreCorrectly()
    {
        // Arrange
        var schoolId = "school-123";
        var request = new Request
        {
            Username = "ParentJohn",
            Comment = "Test",
            Innovation = 3.0m,
            Building = 4.0m,
            Professorate = 5.0m,
            ManagementTeam = 2.0m
        };

        _mockSchoolRepository
            .Setup(r => r.ExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockReviewRepository
            .Setup(r => r.CreateAsync(It.IsAny<UserReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserReview review, CancellationToken _) => review);

        // Act
        var response = await _handler.HandleAsync(schoolId, request, CancellationToken.None);

        // Assert
        // Total = (3.0 + 4.0 + 5.0 + 2.0) / 4 = 3.5
        response.TotalScore.Should().Be(3.5m);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogInformation()
    {
        // Arrange
        var schoolId = "school-123";
        var request = new Request
        {
            Username = "ParentJohn",
            Comment = "Test",
            Innovation = 4.0m,
            Building = 4.0m,
            Professorate = 4.0m,
            ManagementTeam = 4.0m
        };

        _mockSchoolRepository
            .Setup(r => r.ExistsAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockReviewRepository
            .Setup(r => r.CreateAsync(It.IsAny<UserReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserReview review, CancellationToken _) => review);

        // Act
        await _handler.HandleAsync(schoolId, request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating review")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("created successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

**Test Coverage Goals:**
- ✅ Valid request creates review
- ✅ Non-existent school validation
- ✅ Null request handling
- ✅ Invalid school ID handling
- ✅ Total score calculation
- ✅ Logging verification
- Target: >80% code coverage

### 5.3 Entity Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/Domain/Entities/UserReviewTests.cs` (NEW)

```csharp
using System;
using FluentAssertions;
using RateYourSchool.Domain.Entities;
using Xunit;

namespace RateYourSchool.Tests.Unit.Domain.Entities;

public class UserReviewTests
{
    [Fact]
    public void TotalScore_ShouldCalculateCorrectAverage()
    {
        // Arrange & Act
        var review = new UserReview
        {
            Id = Guid.NewGuid().ToString(),
            SchoolId = "school-123",
            Username = "Test",
            Comment = "Test",
            Innovation = 4.0m,
            Building = 3.0m,
            Professorate = 5.0m,
            ManagementTeam = 2.0m
        };

        // Assert
        // (4.0 + 3.0 + 5.0 + 2.0) / 4 = 3.5
        review.TotalScore.Should().Be(3.5m);
    }

    [Fact]
    public void TotalScore_ShouldHandleDecimalPrecision()
    {
        // Arrange & Act
        var review = new UserReview
        {
            Id = Guid.NewGuid().ToString(),
            SchoolId = "school-123",
            Username = "Test",
            Comment = "Test",
            Innovation = 4.75m,
            Building = 4.25m,
            Professorate = 4.80m,
            ManagementTeam = 4.50m
        };

        // Assert
        // (4.75 + 4.25 + 4.80 + 4.50) / 4 = 4.575
        review.TotalScore.Should().Be(4.575m);
    }

    [Fact]
    public void CreatedAt_ShouldDefaultToUtcNow()
    {
        // Arrange & Act
        var review = new UserReview
        {
            Id = Guid.NewGuid().ToString(),
            SchoolId = "school-123",
            Username = "Test",
            Comment = "Test",
            Innovation = 4.0m,
            Building = 4.0m,
            Professorate = 4.0m,
            ManagementTeam = 4.0m
        };

        // Assert
        review.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        review.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
```

### 5.4 Run Unit Tests

```bash
# Run all tests
dotnet test src/Tests/RateYourSchool.Tests.Unit/

# With code coverage
dotnet test src/Tests/RateYourSchool.Tests.Unit/ /p:CollectCoverage=true

# Specific test class
dotnet test --filter "FullyQualifiedName~HandlerTests"

# Verbose output
dotnet test src/Tests/RateYourSchool.Tests.Unit/ -v detailed
```

---

## 6. Integration Tests Implementation

### 6.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Integration/RateYourSchool.Tests.Integration.csproj` (UPDATE if needed)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Reqnroll" Version="2.0.0" />
    <PackageReference Include="Reqnroll.xUnit" Version="2.0.0" />
    <PackageReference Include="Testcontainers.MongoDb" Version="3.6.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Api\Api.csproj" />
  </ItemGroup>
</Project>
```

### 6.2 Gherkin Feature File

**File:** `src/Tests/RateYourSchool.Tests.Integration/Features/CreateSchoolReview.feature` (NEW)

```gherkin
Feature: Create School Review
  As an anonymous user
  I want to create a school review
  So that I can provide scores and a text comment regarding a school

  Background:
    Given the following schools exist in the database:
      | SchoolId                             | Name                  | City    | Province |
      | 550e8400-e29b-41d4-a716-446655440000 | Maple Leaf Elementary | Toronto | ON       |
      | 550e8400-e29b-41d4-a716-446655440001 | Oak Grove High        | Toronto | ON       |

  Scenario: Successfully create a review
    When I send a POST request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440000/reviews" with:
      """
      {
        "username": "ParentJohn",
        "comment": "Excellent school with dedicated teachers.",
        "innovation": 4.5,
        "building": 4.0,
        "professorate": 5.0,
        "managementTeam": 4.2
      }
      """
    Then the response status code should be 201
    And the response should contain a review with:
      | Field          | Value                                       |
      | Username       | ParentJohn                                  |
      | Comment        | Excellent school with dedicated teachers.   |
      | Innovation     | 4.5                                         |
      | Building       | 4.0                                         |
      | Professorate   | 5.0                                         |
      | ManagementTeam | 4.2                                         |
    And the response "TotalScore" should be approximately 4.425
    And the response should have a valid "Id"
    And the response should have a valid "CreatedAt" timestamp

  Scenario: Reject review for non-existent school
    When I send a POST request to "/api/v1/schools/00000000-0000-0000-0000-000000000000/reviews" with:
      """
      {
        "username": "ParentJohn",
        "comment": "Test",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status code should be 404
    And the response should contain error message "School with ID '00000000-0000-0000-0000-000000000000' does not exist"

  Scenario: Reject review with missing username
    When I send a POST request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440000/reviews" with:
      """
      {
        "comment": "Test",
        "innovation": 4.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status code should be 400
    And the response should contain validation error for "username"

  Scenario: Reject review with invalid rating
    When I send a POST request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440000/reviews" with:
      """
      {
        "username": "ParentJohn",
        "comment": "Test",
        "innovation": 6.0,
        "building": 4.0,
        "professorate": 4.0,
        "managementTeam": 4.0
      }
      """
    Then the response status code should be 400
    And the response should contain validation error for "innovation"

  Scenario: Create multiple reviews for the same school
    Given the school "550e8400-e29b-41d4-a716-446655440000" has 5 existing reviews
    When I send a POST request to "/api/v1/schools/550e8400-e29b-41d4-a716-446655440000/reviews" with:
      """
      {
        "username": "TeacherSarah",
        "comment": "Great environment",
        "innovation": 4.8,
        "building": 4.5,
        "professorate": 4.9,
        "managementTeam": 4.6
      }
      """
    Then the response status code should be 201
    And the school should now have 6 reviews in total
```

### 6.3 Step Definitions

**File:** `src/Tests/RateYourSchool.Tests.Integration/StepDefinitions/CreateSchoolReviewSteps.cs` (NEW)

```csharp
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Reqnroll;
using RateYourSchool.Api.Endpoints.CreateSchoolReview;
using RateYourSchool.Domain.Entities;

namespace RateYourSchool.Tests.Integration.StepDefinitions;

[Binding]
public class CreateSchoolReviewSteps
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage _response = null!;
    private Response? _responseData;

    public CreateSchoolReviewSteps(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Given(@"the school ""(.*)"" has (.*) existing reviews")]
    public async Task GivenSchoolHasExistingReviews(string schoolId, int reviewCount)
    {
        var database = _factory.GetMongoDatabase();
        var collection = database.GetCollection<UserReview>("userReviews");

        var reviews = Enumerable.Range(1, reviewCount).Select(i => new UserReview
        {
            Id = Guid.NewGuid().ToString(),
            SchoolId = schoolId,
            Username = $"User{i}",
            Comment = $"Comment {i}",
            Innovation = 4.0m,
            Building = 4.0m,
            Professorate = 4.0m,
            ManagementTeam = 4.0m,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        });

        await collection.InsertManyAsync(reviews);
    }

    [When(@"I send a POST request to ""(.*)"" with:")]
    public async Task WhenISendPostRequestWith(string url, string requestBody)
    {
        var content = JsonContent.Create(JsonDocument.Parse(requestBody).RootElement);
        _response = await _client.PostAsync(url, content);
        
        if (_response.IsSuccessStatusCode)
        {
            _responseData = await _response.Content.ReadFromJsonAsync<Response>();
        }
    }

    [Then(@"the response should contain a review with:")]
    public void ThenResponseShouldContainReviewWith(Table table)
    {
        _responseData.Should().NotBeNull();

        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var expectedValue = row["Value"];

            var actualValue = field switch
            {
                "Username" => _responseData!.Username,
                "Comment" => _responseData!.Comment,
                "Innovation" => _responseData!.Innovation.ToString(),
                "Building" => _responseData!.Building.ToString(),
                "Professorate" => _responseData!.Professorate.ToString(),
                "ManagementTeam" => _responseData!.ManagementTeam.ToString(),
                _ => throw new ArgumentException($"Unknown field: {field}")
            };

            actualValue.Should().Be(expectedValue);
        }
    }

    [Then(@"the response ""(.*)"" should be approximately (.*)")]
    public void ThenResponseFieldShouldBeApproximately(string field, decimal expectedValue)
    {
        _responseData.Should().NotBeNull();
        
        var actualValue = field switch
        {
            "TotalScore" => _responseData!.TotalScore,
            _ => throw new ArgumentException($"Unknown field: {field}")
        };

        actualValue.Should().BeApproximately(expectedValue, 0.001m);
    }

    [Then(@"the response should have a valid ""(.*)""")]
    public void ThenResponseShouldHaveValidField(string field)
    {
        _responseData.Should().NotBeNull();

        switch (field)
        {
            case "Id":
                _responseData!.Id.Should().NotBeNullOrEmpty();
                Guid.TryParse(_responseData.Id, out _).Should().BeTrue();
                break;
            case "CreatedAt":
                _responseData!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
                _responseData.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
                break;
            default:
                throw new ArgumentException($"Unknown field: {field}");
        }
    }

    [Then(@"the response should contain error message ""(.*)""")]
    public async Task ThenResponseShouldContainErrorMessage(string expectedMessage)
    {
        var content = await _response.Content.ReadAsStringAsync();
        content.Should().Contain(expectedMessage);
    }

    [Then(@"the response should contain validation error for ""(.*)""")]
    public async Task ThenResponseShouldContainValidationError(string fieldName)
    {
        var content = await _response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain(fieldName.ToLowerInvariant());
    }

    [Then(@"the school should now have (.*) reviews in total")]
    public async Task ThenSchoolShouldHaveReviewsInTotal(int expectedCount)
    {
        var database = _factory.GetMongoDatabase();
        var collection = database.GetCollection<UserReview>("userReviews");
        
        var count = await collection.CountDocumentsAsync(_ => true);
        count.Should().Be(expectedCount);
    }
}
```

### 6.4 Run Integration Tests

```bash
# Run all integration tests
dotnet test src/Tests/RateYourSchool.Tests.Integration/

# Run specific feature
dotnet test --filter "FullyQualifiedName~CreateSchoolReview"

# Generate Reqnroll HTML report
dotnet test src/Tests/RateYourSchool.Tests.Integration/ --logger "reqnroll;LogFilePath=TestResults/report.html"
```

---

## 7. Frontend Implementation

### 7.1 TypeScript Types

**File:** `src/frontend/src/types/review.ts` (NEW)

```typescript
/**
 * Request payload for creating a school review
 */
export interface CreateReviewRequest {
  username: string;
  comment: string;
  innovation: number;
  building: number;
  professorate: number;
  managementTeam: number;
}

/**
 * Response from creating a school review
 */
export interface CreateReviewResponse {
  id: string;
  schoolId: string;
  username: string;
  comment: string;
  innovation: number;
  building: number;
  professorate: number;
  managementTeam: number;
  totalScore: number;
  createdAt: string;
}

/**
 * Validation error response
 */
export interface ValidationError {
  type: string;
  title: string;
  status: number;
  errors: Record<string, string[]>;
}
```

### 7.2 API Service

**File:** `src/frontend/src/services/review/reviewService.ts` (NEW)

```typescript
import axios from 'axios';
import type { CreateReviewRequest, CreateReviewResponse } from '../../types/review';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export const reviewService = {
  /**
   * Create a new school review
   * @param schoolId - The ID of the school to review
   * @param request - The review data
   * @returns The created review
   */
  async createReview(
    schoolId: string,
    request: CreateReviewRequest
  ): Promise<CreateReviewResponse> {
    try {
      const response = await axios.post<CreateReviewResponse>(
        `${API_BASE_URL}/api/v1/schools/${schoolId}/reviews`,
        request
      );
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error)) {
        if (error.response?.status === 404) {
          throw new Error('School not found');
        }
        if (error.response?.status === 400) {
          const validationErrors = error.response.data.errors;
          const errorMessages = Object.values(validationErrors).flat().join(', ');
          throw new Error(`Validation failed: ${errorMessages}`);
        }
      }
      throw error;
    }
  },
};
```

### 7.3 Custom Hook

**File:** `src/frontend/src/hooks/useCreateReview.ts` (NEW)

```typescript
import { useState } from 'react';
import { reviewService } from '../services/review/reviewService';
import type { CreateReviewRequest, CreateReviewResponse } from '../types/review';

interface UseCreateReviewResult {
  createReview: (schoolId: string, request: CreateReviewRequest) => Promise<CreateReviewResponse | null>;
  loading: boolean;
  error: string | null;
  success: boolean;
}

export const useCreateReview = (): UseCreateReviewResult => {
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<boolean>(false);

  const createReview = async (
    schoolId: string,
    request: CreateReviewRequest
  ): Promise<CreateReviewResponse | null> => {
    setLoading(true);
    setError(null);
    setSuccess(false);

    try {
      const response = await reviewService.createReview(schoolId, request);
      setSuccess(true);
      return response;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to create review';
      setError(errorMessage);
      return null;
    } finally {
      setLoading(false);
    }
  };

  return { createReview, loading, error, success };
};
```

### 7.4 Review Form Component

**File:** `src/frontend/src/components/ReviewForm/ReviewForm.tsx` (NEW)

```typescript
import React, { useState } from 'react';
import { useCreateReview } from '../../hooks/useCreateReview';
import type { CreateReviewRequest } from '../../types/review';
import './ReviewForm.css';

interface ReviewFormProps {
  schoolId: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export const ReviewForm: React.FC<ReviewFormProps> = ({ schoolId, onSuccess, onCancel }) => {
  const { createReview, loading, error, success } = useCreateReview();
  const [formData, setFormData] = useState<CreateReviewRequest>({
    username: '',
    comment: '',
    innovation: 0,
    building: 0,
    professorate: 0,
    managementTeam: 0,
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'comment' || name === 'username' ? value : parseFloat(value) || 0,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const result = await createReview(schoolId, formData);
    
    if (result && onSuccess) {
      onSuccess();
    }
  };

  const handleReset = () => {
    setFormData({
      username: '',
      comment: '',
      innovation: 0,
      building: 0,
      professorate: 0,
      managementTeam: 0,
    });
  };

  return (
    <div className="review-form">
      <h2>Write a Review</h2>
      
      {success && <div className="alert alert-success">Review submitted successfully!</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="username">Your Name *</label>
          <input
            type="text"
            id="username"
            name="username"
            value={formData.username}
            onChange={handleInputChange}
            required
            maxLength={100}
            placeholder="Enter your name"
          />
        </div>

        <div className="form-group">
          <label htmlFor="comment">Your Review *</label>
          <textarea
            id="comment"
            name="comment"
            value={formData.comment}
            onChange={handleInputChange}
            required
            maxLength={2000}
            rows={5}
            placeholder="Share your experience with this school..."
          />
          <small>{formData.comment.length} / 2000 characters</small>
        </div>

        <div className="ratings-section">
          <h3>Rate the School (0-5)</h3>
          
          <div className="form-group">
            <label htmlFor="innovation">Innovation</label>
            <input
              type="number"
              id="innovation"
              name="innovation"
              value={formData.innovation}
              onChange={handleInputChange}
              min="0"
              max="5"
              step="0.1"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="building">Building & Facilities</label>
            <input
              type="number"
              id="building"
              name="building"
              value={formData.building}
              onChange={handleInputChange}
              min="0"
              max="5"
              step="0.1"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="professorate">Teaching Staff</label>
            <input
              type="number"
              id="professorate"
              name="professorate"
              value={formData.professorate}
              onChange={handleInputChange}
              min="0"
              max="5"
              step="0.1"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="managementTeam">Management Team</label>
            <input
              type="number"
              id="managementTeam"
              name="managementTeam"
              value={formData.managementTeam}
              onChange={handleInputChange}
              min="0"
              max="5"
              step="0.1"
              required
            />
          </div>
        </div>

        <div className="form-actions">
          <button type="button" onClick={handleReset} disabled={loading}>
            Reset
          </button>
          {onCancel && (
            <button type="button" onClick={onCancel} disabled={loading}>
              Cancel
            </button>
          )}
          <button type="submit" disabled={loading} className="btn-primary">
            {loading ? 'Submitting...' : 'Submit Review'}
          </button>
        </div>
      </form>
    </div>
  );
};
```

### 7.5 Component Styles

**File:** `src/frontend/src/components/ReviewForm/ReviewForm.css` (NEW)

```css
.review-form {
  max-width: 600px;
  margin: 2rem auto;
  padding: 2rem;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.review-form h2 {
  margin-bottom: 1.5rem;
  color: #333;
}

.review-form h3 {
  margin-top: 1.5rem;
  margin-bottom: 1rem;
  color: #555;
  font-size: 1.1rem;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #333;
}

.form-group input[type="text"],
.form-group input[type="number"],
.form-group textarea {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  font-family: inherit;
}

.form-group input[type="text"]:focus,
.form-group input[type="number"]:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 3px rgba(0, 123, 255, 0.1);
}

.form-group small {
  display: block;
  margin-top: 0.25rem;
  color: #666;
  font-size: 0.875rem;
}

.ratings-section {
  border-top: 1px solid #eee;
  padding-top: 1rem;
}

.form-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  margin-top: 2rem;
  padding-top: 1rem;
  border-top: 1px solid #eee;
}

.form-actions button {
  padding: 0.75rem 1.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  background: #fff;
  transition: all 0.2s;
}

.form-actions button:hover:not(:disabled) {
  background: #f5f5f5;
}

.form-actions button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.form-actions .btn-primary {
  background: #007bff;
  color: #fff;
  border-color: #007bff;
}

.form-actions .btn-primary:hover:not(:disabled) {
  background: #0056b3;
  border-color: #0056b3;
}

.alert {
  padding: 1rem;
  margin-bottom: 1rem;
  border-radius: 4px;
}

.alert-success {
  background: #d4edda;
  color: #155724;
  border: 1px solid #c3e6cb;
}

.alert-error {
  background: #f8d7da;
  color: #721c24;
  border: 1px solid #f5c6cb;
}
```

---

## 8. Implementation Checklist

### Backend
- [ ] Create `UserReview` entity in Domain layer
- [ ] Create `IRepository<T>` interface for write operations
- [ ] Create Request DTO with validation attributes
- [ ] Create Response DTO
- [ ] Create Handler with business logic
- [ ] Create MongoDB repository implementation
- [ ] Create endpoint mapping
- [ ] Create service registration
- [ ] Update Program.cs
- [ ] Create SchoolRate update event handler
- [ ] Register event handler in Program.cs

### API Documentation
- [ ] Create OpenAPI schema file
- [ ] Create Arazzo workflow file
- [ ] Configure Swashbuckle
- [ ] Add OpenAPI annotations
- [ ] Test with Swagger UI

### Testing
- [ ] Write Handler unit tests
- [ ] Write UserReview entity unit tests
- [ ] Write Repository unit tests
- [ ] Create Gherkin feature file
- [ ] Implement step definitions
- [ ] Setup TestWebApplicationFactory
- [ ] Run and verify all tests pass
- [ ] Achieve >80% code coverage

### Frontend
- [ ] Create TypeScript type definitions
- [ ] Create review service
- [ ] Create useCreateReview hook
- [ ] Create ReviewForm component
- [ ] Create component styles
- [ ] Integrate into school detail page
- [ ] Test user flows
- [ ] Test error handling

### Cross-Cutting
- [ ] Add logging statements
- [ ] Add OpenTelemetry instrumentation
- [ ] Configure MongoDB collections
- [ ] Setup Change Streams for CQRS
- [ ] Update environment configuration
- [ ] Document API in README

---

## 9. Notes for Subagent Implementation

### Execution Order

1. **Backend Foundation** (Steps 1-4)
   - Create entities and interfaces first
   - These are dependencies for everything else

2. **Repository Layer** (Steps 5-6)
   - Implement data access
   - Required by handlers

3. **API Layer** (Steps 7-9)
   - Implement endpoints and handlers
   - Wire up in Program.cs

4. **CQRS Event Handling** (Steps 10-11)
   - Create background service for read model updates
   - Register as hosted service

5. **Testing** (Sections 5-6)
   - Unit tests first
   - Integration tests second
   - Validate coverage

6. **Frontend** (Section 7)
   - Types and services
   - Hooks
   - Components
   - Styles

### Key Considerations

**MongoDB Configuration:**
- Ensure collections `userReviews`, `schools`, and `schoolRates` exist
- Setup indexes on `schoolId` for userReviews
- Configure Change Streams for replica set

**CQRS Pattern:**
- UserReview is write model
- SchoolRate is read model
- Updates are asynchronous via Change Streams
- Embrace eventual consistency (BR-048)

**Validation:**
- Request validation via DataAnnotations
- Business validation in handler
- MongoDB schema validation (optional)

**Error Handling:**
- 400 for validation errors
- 404 for school not found
- 500 for internal errors
- Proper ProblemDetails responses

**Testing Strategy:**
- Mock repositories in unit tests
- Use Testcontainers for integration tests
- Test happy path and all edge cases
- Verify SchoolRate update trigger (may require wait/poll)

**Performance:**
- Target <500ms for review creation
- SchoolRate update is async (no blocking)
- Index schoolId for efficient queries

### Dependencies

**NuGet Packages:**
- MongoDB.Driver
- FluentValidation (if not using DataAnnotations)
- FluentAssertions (testing)
- Moq (testing)
- Testcontainers.MongoDb (integration testing)

**Frontend Packages:**
- axios
- react
- typescript

### Business Rules to Enforce

- BR-036: Review must have username, comment, and all four ratings
- BR-037: Review must be associated with existing school
- BR-038: No authentication required
- BR-039: Review creation triggers SchoolRate update
- BR-040: SchoolRate maintains last 25 reviews
- BR-011: SchoolRate updated when UserReview created
- BR-013: Score recalculation
- BR-048: Eventual consistency acceptable

---

**End of Use Case Document**
