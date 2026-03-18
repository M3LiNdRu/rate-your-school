# Semantic Release Setup Guide

This repository uses [semantic-release](https://github.com/semantic-release/semantic-release) for automated version management and release generation.

## How It Works

1. **Commits** to the `main` branch trigger the semantic release workflow
2. **Commit messages** following [Conventional Commits](https://www.conventionalcommits.org/) determine version bumps
3. **Versions** are calculated automatically based on commit types
4. **CHANGELOG.md** is updated with release notes
5. **Git tags** are created for each version
6. **GitHub Releases** are published automatically

## Initial Setup

### 1. Update Repository URL

Edit [.releaserc.json](.releaserc.json) and replace the `repositoryUrl`:

```json
{
  "repositoryUrl": "https://github.com/YOUR-ORG/RateYourSchool",
  ...
}
```

### 2. Configure GitHub Token

The workflow uses `GITHUB_TOKEN` which is automatically provided by GitHub Actions. No additional configuration needed.

### 3. Branch Protection (Recommended)

Configure branch protection for `main`:

1. Go to **Settings** → **Branches** → **Add rule**
2. Set branch name pattern: `main`
3. Enable:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
   - ✅ Require conversation resolution before merging
   - ✅ Include administrators

### 4. Initial Release

Create an initial release to establish the baseline version:

```bash
# Option 1: Create a tag manually
git tag v0.1.0
git push origin v0.1.0

# Option 2: Let semantic-release create the first release
# Just push a commit with a conventional message to main
git commit --allow-empty -m "chore: initial release setup"
git push origin main
```

## Version Calculation

Versions follow [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH):

### MAJOR (X.0.0) - Breaking Changes

Triggered by:
- Commit with `BREAKING CHANGE:` in footer
- Commit with `!` after type/scope: `feat!: new API`

Example:
```bash
git commit -m "feat(api)!: change review rating scale

BREAKING CHANGE: Rating scale changed from 1-5 to 1-10"
```

Result: `1.0.0` → `2.0.0`

### MINOR (0.X.0) - New Features

Triggered by:
- `feat` commits

Example:
```bash
git commit -m "feat(reviews): add filtering by rating"
```

Result: `1.0.0` → `1.1.0`

### PATCH (0.0.X) - Bug Fixes & Improvements

Triggered by:
- `fix` commits (bug fixes)
- `perf` commits (performance)
- `refactor` commits (code refactoring)
- `docs` commits (documentation)
- `build` commits (build system)
- `revert` commits (reverts)

Examples:
```bash
git commit -m "fix(scoring): correct null reference error"
git commit -m "perf(api): optimize database queries"
git commit -m "docs(readme): update installation guide"
```

Result: `1.0.0` → `1.0.1`

### No Version Bump

These commits do NOT trigger a release:
- `style` (formatting)
- `test` (tests)
- `ci` (CI/CD)
- `chore` (maintenance)

## Commit Message Format

### Structure

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Examples

#### Feature
```
feat(api): add pagination to school search endpoint

Implement page and pageSize query parameters.
Default page size is 20, maximum is 1000.

Closes #45
```

#### Bug Fix
```
fix(reviews): prevent duplicate review submission

Added validation to check if user already reviewed the school.

Fixes #67
```

#### Breaking Change
```
feat(api)!: migrate to new authentication system

BREAKING CHANGE: API now requires JWT tokens instead of API keys.
Update client applications to use OAuth 2.0 flow.

Refs #89
```

#### Documentation
```
docs(contributing): add semantic release documentation

Explain how semantic versioning works in the project.
```

#### Multiple Changes
```
feat(api): add multiple improvements

- Add search by city parameter
- Improve error messages
- Add request validation

Closes #23, #24, #25
```

## Workflow Files

### Semantic Release Workflow
[.github/workflows/semantic-release.yml](.github/workflows/semantic-release.yml)

**Triggers:**
- Push to `main` branch
- Manual workflow dispatch

**Actions:**
1. Checkout code
2. Setup Node.js and .NET
3. Install semantic-release tools
4. Analyze commits and determine version
5. Update CHANGELOG.md
6. Create git tag
7. Publish GitHub release
8. Commit changes

### Commit Linter Workflow
[.github/workflows/commitlint.yml](.github/workflows/commitlint.yml)

**Triggers:**
- Pull requests
- Push to main

**Actions:**
1. Validate commit messages
2. Ensure conventional commit format
3. Fail if invalid messages found

## Configuration Files

### .releaserc.json
Main semantic-release configuration:
- Defines release branches
- Configures plugins
- Sets changelog format
- Defines GitHub release assets

### .commitlintrc.json
Commitlint configuration:
- Defines allowed commit types
- Sets validation rules
- Enforces message format

## Troubleshooting

### Release Not Triggered

**Issue**: Commits pushed but no release created

**Possible causes:**
1. **No releasable commits** - Only `style`, `test`, `ci`, `chore` commits
2. **Commit format invalid** - Messages don't follow conventional commits
3. **Already released** - Version already exists for these commits

**Solution:**
- Check commit messages: `git log --oneline`
- Verify with commitlint: `npx commitlint --from HEAD~1 --to HEAD`
- Check GitHub Actions logs for errors

### Version Incorrect

**Issue**: Wrong version number generated

**Possible cause:**
- Commit type incorrect
- Expected `feat` but used `fix`
- Missing `BREAKING CHANGE:` indicator

**Solution:**
- Review commit messages
- Amend commit if not yet pushed: `git commit --amend`
- If already released, create a new commit to trigger correct version

### Workflow Permission Error

**Issue**: GitHub Actions fails with permission error

**Solution:**
1. Go to **Settings** → **Actions** → **General**
2. Under "Workflow permissions":
   - Select "Read and write permissions"
   - Enable "Allow GitHub Actions to create and approve pull requests"

### Changelog Not Updated

**Issue**: CHANGELOG.md not updating

**Possible cause:**
- File conflicts
- Git configuration issues

**Solution:**
1. Ensure CHANGELOG.md exists and is tracked
2. Check workflow logs for errors
3. Verify `.releaserc.json` changelog plugin configuration

## Best Practices

### 1. Atomic Commits
Make small, focused commits that do one thing:
```bash
# Good
git commit -m "feat(api): add search parameter"
git commit -m "feat(frontend): add search UI component"

# Avoid
git commit -m "feat: add search feature with UI, API, and tests"
```

### 2. Descriptive Scopes
Use clear, consistent scopes:
- `api` - Backend API changes
- `frontend` - Frontend changes
- `docs` - Documentation
- `tests` - Test changes
- `ci` - CI/CD changes

### 3. Clear Subjects
- Use imperative mood: "add" not "added"
- Be specific: "add pagination to schools endpoint"
- Limit to 72 characters

### 4. Include References
Link to issues and PRs:
```
Closes #123
Fixes #456
Refs #789
```

### 5. Document Breaking Changes
Always explain breaking changes:
```
BREAKING CHANGE: Removed legacy API v1 endpoints.
Migrate to API v2. See migration guide in docs/MIGRATION.md
```

## Manual Release (Emergency)

If automated release fails, you can create a release manually:

```bash
# 1. Determine next version manually
npm install -g semantic-release
npx semantic-release --dry-run

# 2. Create tag
git tag v1.2.3
git push origin v1.2.3

# 3. Create GitHub Release
# Go to GitHub → Releases → Create new release
# Select the tag and publish
```

## Resources

- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [Semantic Release Documentation](https://semantic-release.gitbook.io/)
- [Commitlint](https://commitlint.js.org/)
- [Project Commit Convention](.github/COMMIT_CONVENTION.md)
- [Contributing Guidelines](CONTRIBUTING.md)

---

**Questions?** Open an issue or check [CONTRIBUTING.md](CONTRIBUTING.md)
