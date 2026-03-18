# Use Case Template & Instructions

This template provides a comprehensive structure for documenting use cases in the RateYourSchool application. Each use case document serves as a complete implementation guide that can be executed by multiple specialized subagents.

---

## Purpose

Use case documents serve as:
- **Single Source of Truth**: Complete specification for a feature
- **Implementation Guide**: Step-by-step instructions for development
- **Testing Blueprint**: Acceptance criteria and test scenarios
- **Subagent Instructions**: Detailed tasks for automated implementation
- **Documentation**: Reference for current and future team members

---

## When to Create a Use Case Document

Create a use case document when:
- Implementing a new API endpoint
- Adding a new user-facing feature
- Building a complete vertical slice (API + tests + frontend)
- Work requires coordination across multiple layers (backend, frontend, tests)
- Feature complexity requires detailed specification

---

## Document Structure

### Required Sections

1. **Header & Metadata**
2. **Implementation Progress Table**
3. **Summary**
4. **Acceptance Criteria (Gherkin)**
5. **OpenAPI Schema & Arazzo Flow**
6. **Backend Implementation**
7. **Unit Tests Implementation**
8. **Integration Tests Implementation**
9. **Frontend Implementation**
10. **Implementation Checklist**
11. **Notes for Subagent Implementation**

---

## Template Structure

```markdown
# Use Case: [Feature Name] ([Brief Description])

**Status:** [Not Started | Partially Implemented | Completed]  
**Priority:** [Low | Medium | High | Critical]  
**Related Use Cases:** [UC-XXX references]  
**Business Rules:** [BR-XXX through BR-YYY]

---

## Implementation Progress

| Task | Status | Completed |
|------|--------|-----------|
| **Functional Requirements Document** | [todo/in-progress/in-review/done] | [YYYY-MM-DD or -] |
| **API Specifications** | [todo/in-progress/in-review/done] | [YYYY-MM-DD or -] |
| **Backend Implementation** | [todo/in-progress/in-review/done] | [YYYY-MM-DD or -] |
| **Frontend Implementation** | [todo/in-progress/in-review/done] | [YYYY-MM-DD or -] |
| **E2E Tests** | [todo/in-progress/in-review/done] | [YYYY-MM-DD or -] |
| **Penetration Tests** | [todo/in-progress/in-review/done] | [YYYY-MM-DD or -] |

**Status Legend:**
- `todo` - Not started
- `in-progress` - Currently being worked on
- `in-review` - Completed and under review
- `done` - Completed and verified

---

## 1. Summary

### Overview
[Provide a high-level description of what this use case accomplishes. 
Explain the business value and user benefit. 2-3 paragraphs.]

### Functional Requirements
- [List specific functional requirements]
- [What data is retrieved/created/updated]
- [What validations are applied]
- [What business rules are enforced]
- [What defaults are used]

### Technical Context
- **CQRS Pattern**: [Describe read model vs write model usage]
- **Data Source**: [MongoDB collection names]
- **Architecture**: [Vertical Slice path, e.g., `src/Api/Endpoints/[FeatureName]/`]
- **Current State**: [What exists, what's missing]

---

## 2. Acceptance Criteria (Gherkin)

```gherkin
Feature: [Feature Name]
  As a [user type]
  I want to [action]
  So that [benefit]

  Background:
    Given [common setup for all scenarios]

  Scenario: [Primary happy path scenario]
    Given [preconditions]
    When [action]
    Then [expected results]
    And [additional assertions]

  Scenario: [Edge case or validation scenario]
    Given [preconditions]
    When [action with invalid/edge case data]
    Then [expected error handling or correction]

  Scenario: [Performance scenario]
    Given [large dataset or specific conditions]
    When [action]
    Then [performance expectations]
    And [response time should be less than XXXms]

  [Add 5-10 scenarios covering:]
  - Happy path
  - Edge cases (empty results, boundaries)
  - Validation failures
  - Error handling
  - Performance requirements
```

---

## 3. OpenAPI Schema & Arazzo Flow

### 3.1 Define OpenAPI Schema

**File:** `src/Api/openapi/[feature-name].yaml`

```yaml
openapi: 3.0.3
info:
  title: RateYourSchool API
  version: 1.0.0

paths:
  /api/v1/[resource]:
    [method]:
      summary: [Brief description]
      description: |
        [Detailed description]
        
        **CQRS Note:** [Explain read/write model usage]
      operationId: [operationName]
      tags:
        - [TagName]
      parameters:
        - name: [paramName]
          in: [query/path]
          description: [Parameter description]
          required: [true/false]
          schema:
            type: [type]
            [constraints]
      requestBody:
        [If POST/PUT, define request body]
      responses:
        '200':
          description: [Success description]
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/[ResponseSchema]'
              examples:
                success:
                  summary: [Example description]
                  value:
                    [example data]
        '400':
          description: Bad request
        '500':
          description: Internal server error

components:
  schemas:
    [Define all request/response schemas]
```

### 3.2 Arazzo Workflow Definition

**File:** `src/Api/arazzo/[workflow-name].yaml`

```yaml
arazzo: 1.0.0
info:
  title: [Workflow Name]
  version: 1.0.0
  description: |
    [Describe the user journey this workflow represents]

sourceDescriptions:
  - name: rateyourschool-api
    type: openapi
    url: ./openapi/[feature-name].yaml

workflows:
  - workflowId: [workflow-id]
    summary: [Brief summary]
    description: |
      [Detailed workflow description]
    
    inputs:
      type: object
      properties:
        [Define inputs]
    
    steps:
      - stepId: [stepName]
        description: [Step description]
        operationId: [operationId]
        parameters:
          - name: [paramName]
            in: [location]
            value: [value or $input reference]
        successCriteria:
          - condition: [condition expression]
        outputs:
          [outputName]: [value expression]

    outputs:
      [Define workflow outputs]
```

### 3.3 Implementation Steps

1. **Create OpenAPI specification file**
2. **Create Arazzo workflow file**
3. **Configure Swashbuckle** for OpenAPI documentation
4. **Add OpenAPI annotations** to endpoint
5. **Validate schema** using tools
6. **Test Arazzo workflow**

---

## 4. Backend Implementation

### 4.1 Current State Analysis

**Existing Files:**
- [List what already exists with ✅]

**Missing/Incomplete:**
- [List what needs to be created with ❌]

### 4.2 Repository Pattern Best Practices

**Generic Repository Pattern:**

Use the generic `IReadOnlyRepository.GetAsync<T>()` method to keep repositories domain-agnostic:

```csharp
// IReadOnlyRepository interface
Task<IEnumerable<T>> GetAsync<T>(CancellationToken cancellationToken);
```

**MongoDB Projection for View Models:**

Query view models directly using MongoDB projection (view models are subsets of domain entities):

```csharp
public async Task<IEnumerable<T>> GetAsync<T>(CancellationToken cancellationToken)
{
    // Use projection to query ViewModel directly from entity collection
    var results = await _collection
        .Find(FilterDefinition<EntityType>.Empty)
        .Project(e => new ViewModel
        {
            Id = e.Id,
            Name = e.Name,
            // Map only required fields
        })
        .Sort(Builders<ViewModel>.Sort.Ascending(vm => vm.SortField))
        .ToListAsync(cancellationToken);
    
    return results.Cast<T>();
}
```

**Handler Pattern:**

Handlers receive view models directly from repository, no mapping needed:

```csharp
public async Task<Response> HandleAsync(Request request, CancellationToken ct)
{
    // Query ViewModel directly - no entity-to-ViewModel mapping
    var viewModels = await _repository.GetAsync<ViewModel>(ct);
    
    return new Response { Data = viewModels };
}
```

### 4.3 Implementation Steps

#### Step 1: [Configuration/Setup]

**File:** `[path/to/file]` [(NEW) or (UPDATE)]

```[language]
[Complete code example]
```

**Purpose:** [Explain what this does and why]

#### Step 2: [Next Implementation Step]

[Continue with numbered steps for:]
- Database configuration
- Models/DTOs
- Repository implementation (using generic pattern above)
- Service registration
- Middleware/handlers
- Endpoint mapping
- Error handling
- Logging
- OpenTelemetry instrumentation

[Each step should include:]
- File path
- Complete code example
- Explanation of purpose
- Dependencies/prerequisites

---

## 5. Unit Tests Implementation

### 5.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Unit/[Path]/[TestClass].csproj`

```xml
[Project file with dependencies]
```

### 5.2 [Component] Unit Tests

**File:** `src/Tests/RateYourSchool.Tests.Unit/[Path]/[TestClass].cs`

```csharp
using FluentAssertions;
using Moq;
using Xunit;

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
    public async Task HandleAsync_WithValidRequest_ShouldReturnData()
    {
        // Arrange
        var request = new Request();
        _mockRepository
            .Setup(r => r.GetAsync<ViewModel>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestData());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        var request = new Request();
        _mockRepository
            .Setup(r => r.GetAsync<ViewModel>(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ViewModel>());

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    private static IEnumerable<ViewModel> CreateTestData()
    {
        return new[] { /* test data */ };
    }
}

[Include tests for:]
- Happy path scenarios
- Edge cases
- Validation failures
- Null handling
- Error conditions
- Boundary values
```

**Test Coverage Goals:**
- Achieve >80% code coverage
- Test all public methods
- Test all business rules
- Test error paths

### 5.3 Run Unit Tests

```bash
# Commands to run tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Specific tests
dotnet test --filter "FullyQualifiedName~[TestClass]"
```

---

## 6. Integration Tests Implementation

### 6.1 Test Project Setup

**File:** `src/Tests/RateYourSchool.Tests.Integration/[Feature].csproj`

```xml
[Project file with ReqnRoll/SpecFlow, Testcontainers, etc.]
```

### 6.2 Gherkin Feature File

**File:** `src/Tests/RateYourSchool.Tests.Integration/Features/[Feature].feature`

```gherkin
Feature: [Feature Name]
  [Feature description]

  Background:
    [Common setup]

  Scenario: [Scenario name]
    Given [preconditions]
    When [action]
    Then [expected results]

  [Include 5-8 scenarios matching acceptance criteria]
```

### 6.3 Test Infrastructure

**File:** `src/Tests/RateYourSchool.Tests.Integration/Support/TestWebApplicationFactory.cs`

```csharp
[TestWebApplicationFactory implementation with Testcontainers]
```

### 6.4 Step Definitions

**File:** `src/Tests/RateYourSchool.Tests.Integration/StepDefinitions/[Feature]Steps.cs`

```csharp
[Complete step definition class with:]
- Given steps (setup)
- When steps (actions)
- Then steps (assertions)
- Cleanup methods
```

### 6.5 Run Integration Tests

```bash
# Commands to run integration tests
dotnet test

# Generate BDD reports
dotnet test --logger "reqnroll;LogFilePath=TestResults/report.html"
```

---

## 7. Frontend Implementation

### 7.1 Project Setup

**Prerequisites:**
- [List required tools and versions]

**Initialize/Update:**
```bash
[Commands to setup or update frontend]
```

### 7.2 TypeScript Types

**File:** `src/types/[Feature].ts`

```typescript
[Complete type definitions for:]
- Request types
- Response types
- Domain models
- API parameters
```

### 7.3 API Service

**File:** `src/services/[feature]/[feature]Service.ts`

```typescript
[API service implementation with:]
- HTTP client methods
- Error handling
- Type safety
```

### 7.4 Custom Hooks

**File:** `src/hooks/use[Feature].ts`

```typescript
[React custom hooks for:]
- Data fetching
- State management
- Side effects
- Loading/error states
```

### 7.5 Components

**File:** `src/components/[Component]/[Component].tsx`

```typescript
[Component implementations]
```

**File:** `src/components/[Component]/[Component].css`

```css
[Component styles]
```

### 7.6 Pages

**File:** `src/pages/[Page]/[Page].tsx`

```typescript
[Page component combining hooks and components]
```

### 7.7 Configuration

**File:** `.env.development`

```env
[Development environment variables]
```

**File:** `.env.production`

```env
[Production environment variables]
```

### 7.8 Run Frontend

```bash
# Development
npm run dev

# Production build
npm run build

# Preview
npm run preview
```

---

## 8. Implementation Checklist

### Backend
- [ ] [List all backend tasks]
- [ ] [Configuration]
- [ ] [Models]
- [ ] [Repository]
- [ ] [Services]
- [ ] [Endpoint]
- [ ] [Error handling]
- [ ] [Logging]
- [ ] [Documentation]

### Testing
- [ ] [Unit test tasks]
- [ ] [Integration test tasks]
- [ ] [Test coverage verification]

### API Documentation
- [ ] [OpenAPI tasks]
- [ ] [Arazzo tasks]
- [ ] [Validation]

### Frontend
- [ ] [Frontend tasks]
- [ ] [Types]
- [ ] [Services]
- [ ] [Hooks]
- [ ] [Components]
- [ ] [Pages]
- [ ] [Styling]
- [ ] [Integration testing]

### DevOps
- [ ] [CI/CD updates]
- [ ] [Deployment tasks]
- [ ] [Monitoring]

---

## 9. Notes for Subagent Imple (especially repository patterns in 4.2)
- Use generic `IReadOnlyRepository.GetAsync<T>()` for all read operations
- Query view models directly using MongoDB projection (no entity-to-ViewModel mapping)
- Verify implementation against acceptance criteria
- Update implementation progress table when completing tasks
- Document any deviations or improvements
- Report blockers immediately
- Maintain code quality standards

### Key Implementation Patterns

**Repository Pattern:**
- Use generic `GetAsync<T>()` to keep repository domain-agnostic
- MongoDB projection returns view models directly (subset of domain entities)
- Sort using view model fields, not entity fields

**Handler Pattern:**
- Receive view models directly from repository
- No mapping layer between repository and handler
- Handler focuses on business logic, not data transformation

**Test Pattern:**
- Mock `GetAsync<ViewModel>()` not `GetAsync<Entity>()`
- Test data helpers return view models
- Verify view models contain only subset of fiel
1. **Backend Subagent**
   - Focus: Section 4 (Backend Implementation)
   - Responsibilities:
     - Database models and configuration
     - Repository implementation
     - Service registration
     - OpenTelemetry instrumentation
   - Success Criteria: [Specific criteria]

2. **Testing Subagent**
   - Focus: Sections 5 & 6 (Unit and Integration Tests)
   - Responsibilities:
     - xUnit unit tests
     - ReqnRoll/SpecFlow BDD tests
     - Test infrastructure
     - Coverage reporting
   - Success Criteria: [Specific criteria]

3. **API Documentation Subagent**
   - Focus: Section 3 (OpenAPI & Arazzo)
   - Responsibilities:
     - OpenAPI schema creation
     - Arazzo workflow definition
     - Schema validation
     - Example generation
   - Success Criteria: [Specific criteria]

4. **Frontend Subagent**
   - Focus: Section 7 (Frontend Implementation)
   - Responsibilities:
     - React components
     - TypeScript types
     - API integration
     - Styling
   - Success Criteria: [Specific criteria]

5. **Integration Subagent**
   - Focus: Cross-cutting concerns
   - Responsibilities:
     - Coordinate between subagents
     - End-to-end verification
     - Integration issues
     - Final testing
   - Success Criteria: [Specific criteria]

### Subagent Guidelines

Each subagent should:
- Read the relevant section(s) thoroughly
- Follow code examples exactly
- Verify implementation against acceptance criteria
- Update implementation progress table when completing tasks
- Document any deviations or improvements
- Report blockers immediately
- Maintain code quality standards

### Coordination Points

- **Backend → Testing**: Ensure endpoints are ready before integration tests
- **Backend → Frontend**: API contracts must match TypeScript types
- **API Docs → All**: OpenAPI spec is source of truth
- **All → Integration**: Final verification of complete flow

---

## Document Maintenance

### Version Control

- Update the Implementation Progress table when tasks are completed
- Document the completion date in YYYY-MM-DD format
- Change status from `todo` → `in-progress` → `in-review` → `done`
- Keep acceptance criteria updated if requirements change

### Review Process

1. **Initial Review**: After functional requirements are documented
2. **Technical Review**: After API specifications are defined
3. **Implementation Review**: After backend/frontend are implemented
4. **Testing Review**: After tests are written and passing
5. **Final Review**: Before marking as `done`

### Update Triggers

Update this document when:
- Requirements change
- New edge cases are discovered
- Implementation approaches change
- Test scenarios are added/modified
- Performance requirements change
- Security requirements change

---

**Document Version:** 1.0  
**Last Updated:** [YYYY-MM-DD]  
**Status:** [Draft | Ready for Implementation | Under Implementation | Completed]
```

---

## Instructions for Using This Template

### Step 1: Copy and Rename

```bash
cp docs/use-cases/USE-CASE-TEMPLATE.md docs/use-cases/[feature-name].md
```

### Step 2: Fill in Metadata

- Replace `[Feature Name]` with actual feature name
- Set initial status (usually "Not Started")
- Set priority based on business value
- Link related use cases and business rules
- Initialize Implementation Progress table with `todo` status

### Step 3: Document Summary

- Write overview explaining what the feature does
- List functional requirements clearly
- Describe technical context (CQRS, data sources, architecture)
- Assess current state (what exists, what's needed)

### Step 4: Define Acceptance Criteria

- Write Gherkin scenarios covering:
  - Happy path (primary use case)
  - Edge cases (boundaries, empty results)
  - Validation scenarios (bad input)
  - Error handling (system failures)
  - Performance requirements (response times)
- Aim for 5-10 comprehensive scenarios

### Step 5: Specify API

- Define complete OpenAPI schema
- Include all request/response models
- Provide realistic examples
- Create Arazzo workflow showing user journey
- Define success criteria for each workflow step

### Step 6: Plan Backend Implementation

- Analyze current state (what exists)
- Break down into numbered steps
- Provide complete code examples for each step
- Include configuration, models, repositories, services
- Add error handling and logging
- Include OpenTelemetry instrumentation

### Step 7: Design Tests

- Plan unit tests for business logic
- Define integration test scenarios (match Gherkin)
- Create step definitions for BDD tests
- Setup test infrastructure (Testcontainers, etc.)
- Define coverage goals (>80%)

### Step 8: Design Frontend

- Define TypeScript types from API schema
- Plan API service layer
- Design custom hooks for state management
- Design component hierarchy
- Plan styling approach
- Define environment configuration

### Step 9: Create Checklist

- Break down implementation into checkable tasks
- Group by category (backend, frontend, testing, etc.)
- Make tasks specific and actionable
- Include verification steps

### Step 10: Add Subagent Notes

- Define subagent specializations
- Specify responsibilities for each
- Define success criteria
- Document coordination points
- Add guidelines for implementation

---

## Best Practices

### Writing Style

- **Be Specific**: Provide complete code examples, not pseudocode
- **Be Actionable**: Each step should be implementable without guessing
- **Be Comprehensive**: Cover all aspects (happy path, errors, edge cases)
- **Be Realistic**: Include actual file paths, package names, versions
- **Be Testable**: Link everything back to acceptance criteria

### Code Examples

- ✅ Include complete, runnable code
- ✅ Show realistic data and examples
- ✅ Include error handling
- ✅ Add comments for complex logic
- ✅ Follow project coding standards
- ❌ Don't use placeholders like `// implementation here`
- ❌ Don't omit important setup or configuration
- ❌ Don't skip error handling to save space

### Acceptance Criteria

- ✅ Use Given-When-Then format
- ✅ Be specific about data and conditions
- ✅ Include expected outcomes
- ✅ Cover positive and negative scenarios
- ✅ Add performance requirements
- ❌ Don't be vague or ambiguous
- ❌ Don't forget edge cases
- ❌ Don't skip error scenarios

### For Subagent Use

- ✅ Make sections independent when possible
- ✅ Provide complete context within each section
- ✅ Cross-reference related sections
- ✅ Define clear success criteria
- ✅ Include verification steps
- ❌ Don't assume shared context
- ❌ Don't create circular dependencies
- ❌ Don't leave ambiguity

---

## Examples

See existing use case documents for reference:
- [get-schoolrates.md](get-schoolrates.md) - Complete example of this template in use

---

## Template Checklist

Before finalizing a use case document, verify:

- [ ] All placeholder text replaced with actual content
- [ ] Implementation Progress table initialized
- [ ] Summary section complete with overview and requirements
- [ ] At least 5 Gherkin scenarios defined
- [ ] OpenAPI schema complete and valid
- [ ] Arazzo workflow defined
- [ ] Backend implementation steps numbered and complete
- [ ] Unit test examples provided
- [ ] Integration test setup documented
- [ ] Frontend implementation planned
- [ ] Implementation checklist created
- [ ] Subagent notes added with clear responsibilities
- [ ] All code examples are complete and runnable
- [ ] File paths are accurate
- [ ] Document metadata updated (version, date, status)

---

**Template Version:** 1.0  
**Created:** 2026-03-15  
**Based On:** get-schoolrates.md use case document
