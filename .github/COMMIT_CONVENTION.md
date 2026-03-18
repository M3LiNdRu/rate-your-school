# Commit Message Convention

This repository follows the [Conventional Commits](https://www.conventionalcommits.org/) specification for commit messages.

## Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type

Must be one of the following:

- **feat**: A new feature (triggers minor version bump)
- **fix**: A bug fix (triggers patch version bump)
- **docs**: Documentation only changes (triggers patch version bump)
- **style**: Changes that do not affect the meaning of the code (formatting, etc.)
- **refactor**: A code change that neither fixes a bug nor adds a feature (triggers patch version bump)
- **perf**: A code change that improves performance (triggers patch version bump)
- **test**: Adding missing tests or correcting existing tests
- **build**: Changes that affect the build system or external dependencies (triggers patch version bump)
- **ci**: Changes to CI configuration files and scripts
- **chore**: Other changes that don't modify src or test files
- **revert**: Reverts a previous commit (triggers patch version bump)

### Scope (Optional)

The scope should specify the place of the commit change. For example:

- `feat(api)`: New API endpoint
- `fix(frontend)`: Bug fix in frontend
- `docs(readme)`: Update README
- `refactor(handler)`: Refactor handler logic

### Subject

- Use imperative, present tense: "change" not "changed" nor "changes"
- Don't capitalize the first letter
- No period (.) at the end
- Limit to 72 characters

### Body (Optional)

- Provide more detailed explanatory text if needed
- Wrap at 72 characters
- Explain what and why, not how

### Footer (Optional)

- Reference issues: `Closes #123`, `Fixes #456`
- Breaking changes: Start with `BREAKING CHANGE:` followed by description

## Breaking Changes

Breaking changes must be indicated in the commit message footer or with `!` after the type/scope:

```
feat!: remove support for legacy API

BREAKING CHANGE: The legacy v1 API has been removed. Please migrate to v2.
```

Breaking changes trigger a **major** version bump.

## Examples

### Feature

```
feat(reviews): add pagination support for user reviews

Implement pagination parameters (page, pageSize) for the GetUserReviews endpoint.
Supports up to 1000 items per page with a default of 20.

Closes #45
```

### Bug Fix

```
fix(scoring): correct average calculation for schools with no reviews

Previously, schools without reviews showed incorrect scores.
Now returns null when no reviews exist.

Fixes #67
```

### Breaking Change

```
feat(api)!: change review rating scale from 1-5 to 1-10

BREAKING CHANGE: The rating scale for reviews has changed from 1-5 to 1-10.
Existing reviews will need to be migrated. API consumers must update their
rating input validation.

Refs #89
```

### Documentation

```
docs(architecture): add CQRS pattern explanation

Add detailed explanation of CQRS implementation and read/write model separation.
```

### Refactor

```
refactor(handler): extract validation logic to separate method

Improve code readability by extracting validation into a dedicated method.
No functional changes.
```

### Chore

```
chore(deps): update MongoDB driver to 2.25.0
```

## Semantic Version Bumps

Based on commit types:

| Commit Type | Version Bump | Example |
|-------------|--------------|---------|
| `feat` | minor (0.X.0) | 1.0.0 → 1.1.0 |
| `fix`, `perf`, `revert` | patch (0.0.X) | 1.0.0 → 1.0.1 |
| `docs`, `refactor`, `build` | patch (0.0.X) | 1.0.0 → 1.0.1 |
| `BREAKING CHANGE` or `!` | major (X.0.0) | 1.0.0 → 2.0.0 |
| `style`, `test`, `ci`, `chore` | none | no release |

## Tooling

- **Commitizen**: Use `git cz` for interactive commit message creation
- **Commitlint**: Automatically validates commit messages in CI/CD
- **Semantic Release**: Automatically generates releases based on commits

## References

- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [Angular Commit Guidelines](https://github.com/angular/angular/blob/main/CONTRIBUTING.md#commit)
