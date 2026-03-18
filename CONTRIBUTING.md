# Contributing to RateYourSchool

Thank you for your interest in contributing to RateYourSchool! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Coding Standards](#coding-standards)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Release Process](#release-process)

## Code of Conduct

This project follows a professional code of conduct. Please be respectful and constructive in all interactions.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- MongoDB 4.4+
- Node.js 18+ (for frontend)
- Git

### Setup Development Environment

1. **Clone the repository**
   ```bash
   git clone https://github.com/OWNER/RateYourSchool.git
   cd RateYourSchool
   ```

2. **Install backend dependencies**
   ```bash
   cd src/backend
   dotnet restore
   ```

3. **Install frontend dependencies** (when applicable)
   ```bash
   cd src/frontend
   npm install
   ```

4. **Setup MongoDB**
   - Ensure MongoDB is running locally on `mongodb://localhost:27017`
   - Or update `appsettings.Development.json` with your connection string

5. **Run the application**
   ```bash
   cd src/backend/Api
   dotnet run
   ```

## Development Workflow

1. **Create a feature branch**
   ```bash
   git checkout -b feat/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

2. **Make your changes**
   - Write clean, maintainable code
   - Follow the project's architecture patterns (CQRS, Vertical Slices)
   - Add or update tests as needed

3. **Test your changes**
   ```bash
   # Run unit tests
   dotnet test tests/RateYourSchool.Tests.Unit/
   
   # Run integration tests
   dotnet test tests/RateYourSchool.Tests.Integration/
   
   # Check code coverage
   dotnet test --collect:"XPlat Code Coverage"
   ```

4. **Commit your changes**
   - Follow [Conventional Commits](#commit-message-guidelines)
   ```bash
   git add .
   git commit -m "feat(api): add new endpoint for school filtering"
   ```

5. **Push to your fork**
   ```bash
   git push origin feat/your-feature-name
   ```

6. **Open a Pull Request**
   - Provide a clear description of the changes
   - Reference any related issues
   - Ensure all CI checks pass

## Automated Workflows

This project includes several automated workflows to streamline development:

### API Specification Automation

When you create or update a use case document with a TODO API specification, the workflow automatically:

1. **Detects** the TODO status in use case folders (`docs/use-cases/*/`)
2. **Creates** a branch named `icds/<use-case-name>`
3. **Generates** task instructions in the use case folder
4. **Ensures** shared `openapi.yaml`, `arazzo.yaml`, and `postman-collection.json` files exist
5. **Opens** a PR with detailed implementation guidance for GitHub Copilot

**Check TODO API specs:**
```bash
./scripts/api-spec-helper.sh list
```

**Example use case entry:**
```markdown
| **API Specifications (OpenAPI + Arazzo)** | TODO | - |
```

Use case files should be organized in folders: `docs/use-cases/< use-case-name>/use-case.md`

**Important:** All use cases contribute to the same shared API specification files:
- `src/backend/Api/openapi/openapi.yaml` - All API endpoints
- `src/backend/Api/openapi/arazzo.yaml` - All workflows
- `src/backend/Api/openapi/postman-collection.json` - All request examples and tests

See [.github/API_SPEC_AUTOMATION.md](.github/API_SPEC_AUTOMATION.md) and [.github/API_SPEC_QUICK_REFERENCE.md](.github/API_SPEC_QUICK_REFERENCE.md) for details.

### Semantic Release

Releases are fully automated based on conventional commits:
- Push to `main` triggers version calculation
- CHANGELOG.md is automatically updated
- Git tags and GitHub releases are created

See [.github/SEMANTIC_RELEASE_SETUP.md](.github/SEMANTIC_RELEASE_SETUP.md) for details.

### Commit Message Validation

All commits are validated using commitlint:
- Ensures conventional commit format
- Runs on pull requests and pushes to main
- Prevents invalid commit messages from being merged

## Commit Message Guidelines

This project uses [Conventional Commits](https://www.conventionalcommits.org/) for semantic versioning and automated releases.

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: New feature (minor version bump)
- **fix**: Bug fix (patch version bump)
- **docs**: Documentation changes (patch version bump)
- **style**: Code style changes (no version bump)
- **refactor**: Code refactoring (patch version bump)
- **perf**: Performance improvements (patch version bump)
- **test**: Test changes (no version bump)
- **build**: Build system changes (patch version bump)
- **ci**: CI/CD changes (no version bump)
- **chore**: Maintenance tasks (no version bump)
- **revert**: Revert previous commit (patch version bump)

### Breaking Changes

Indicate breaking changes with `!` or `BREAKING CHANGE:` in the footer:

```
feat(api)!: change review rating scale to 1-10

BREAKING CHANGE: Rating scale changed from 1-5 to 1-10.
Migration required for existing reviews.
```

### Examples

```bash
# Feature
git commit -m "feat(reviews): add pagination support"

# Bug fix
git commit -m "fix(scoring): correct null reference in average calculation"

# Documentation
git commit -m "docs(readme): update installation instructions"

# Breaking change
git commit -m "feat(api)!: remove deprecated v1 endpoints"
```

See [.github/COMMIT_CONVENTION.md](.github/COMMIT_CONVENTION.md) for detailed guidelines.

## Coding Standards

### C# / .NET

**Naming Conventions:**
- Classes/Methods: `PascalCase`
- Private fields: `_camelCase` (with underscore prefix)
- Parameters/Local variables: `camelCase`
- Async methods: Suffix with `Async`

**Best Practices:**
- Use `sealed` for non-inheritable classes
- Use `internal` for implementation details
- Always use `async`/`await` for I/O operations
- Include `CancellationToken` in async methods
- Validate input with `ArgumentNullException.ThrowIfNull()`
- Use dependency injection for services

**Example:**
```csharp
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
        
        _logger.LogInformation("Processing request");
        
        var result = await _repository.GetAsync(ct);
        
        return new Response { Data = result };
    }
}
```

### TypeScript / React

**Naming Conventions:**
- Components: `PascalCase`
- Functions/Variables: `camelCase`
- Types/Interfaces: `PascalCase`
- Files: `kebab-case.ts` or `PascalCase.tsx`

**Best Practices:**
- Use functional components with hooks
- Type all props and state
- Use async/await for API calls
- Handle errors appropriately

### Architecture

Follow **Vertical Slice Architecture**:

```
src/Api/Endpoints/[FeatureName]/
├── Endpoint.cs                    # API endpoint
├── Handler.cs                     # Business logic
├── Repository.cs                  # Data access
├── Request.cs                     # Input DTO
├── Response.cs                    # Output DTO
├── [ViewModels].cs                # View models
└── ServiceCollectionExtensions.cs # DI registration
```

Each feature should be self-contained with minimal cross-feature dependencies.

## Testing Requirements

### Code Coverage

- **Minimum**: 80% line coverage for business logic
- **Target**: 90%+ for critical paths

### Unit Tests (xUnit)

**Naming:**
- Test class: `[ClassUnderTest]Tests`
- Test method: `[MethodName]_[Scenario]_[ExpectedBehavior]`

**Example:**
```csharp
public class HandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldReturnSchools()
    {
        // Arrange
        var handler = new Handler(mockRepository.Object, mockLogger.Object);
        var request = new Request { Page = 1, PageSize = 20 };

        // Act
        var result = await handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Schools.Should().HaveCount(20);
    }
}
```

### Integration Tests (ReqnRoll/SpecFlow)

Write BDD-style tests using Gherkin:

```gherkin
Feature: Get SchoolRates List
  As a user
  I want to retrieve schools
  So that I can browse available schools

  Scenario: Retrieve first page
    Given the following schools exist
      | SchoolId | Name        |
      | 1        | Test School |
    When I send a GET request to "/api/v1/schools"
    Then the response status code should be 200
```

### Running Tests

```bash
# Unit tests
dotnet test tests/RateYourSchool.Tests.Unit/

# Integration tests
dotnet test tests/RateYourSchool.Tests.Integration/

# All tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Pull Request Process

1. **Ensure your PR:**
   - Follows commit message conventions
   - Includes tests for new features
   - Maintains or improves code coverage
   - Passes all CI checks
   - Updates documentation if needed

2. **PR Title:**
   - Use conventional commit format
   - Example: `feat(api): add school filtering endpoint`

3. **PR Description:**
   ```markdown
   ## Description
   Brief description of changes
   
   ## Changes
   - Added endpoint X
   - Fixed bug Y
   - Updated documentation Z
   
   ## Related Issues
   Closes #123
   
   ## Testing
   - [ ] Unit tests added/updated
   - [ ] Integration tests added/updated
   - [ ] Manual testing completed
   ```

4. **Code Review:**
   - Address reviewer feedback promptly
   - Use `feat`, `fix`, `refactor` commits for changes
   - Squash commits before merge if requested

5. **Merge:**
   - PRs are merged using "Squash and merge"
   - Ensure final commit message follows conventions

## Release Process

Releases are **fully automated** using semantic-release:

1. **Commits to `main` branch** trigger the release workflow
2. **Semantic-release analyzes** commit messages since last release
3. **Version is calculated** based on commit types:
   - `feat` → minor version (0.X.0)
   - `fix`, `perf`, `refactor`, `docs`, `build` → patch version (0.0.X)
   - `BREAKING CHANGE` or `!` → major version (X.0.0)
4. **CHANGELOG.md is updated** with release notes
5. **Git tag is created** with new version
6. **GitHub Release is published** with auto-generated notes

### Manual Release (if needed)

Releases should be automatic, but you can trigger manually:

```bash
# Ensure you're on main branch
git checkout main
git pull origin main

# Trigger workflow manually from GitHub Actions UI
```

### Version Numbering

We follow [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes and minor improvements

## Questions or Issues?

- **Documentation**: Check [docs/](docs/) folder
- **Architecture**: See [ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **Business Rules**: See [BUSINESS_RULES.md](docs/BUSINESS_RULES.md)
- **Issues**: Open a GitHub issue with details

---

Thank you for contributing to RateYourSchool! 🎓
