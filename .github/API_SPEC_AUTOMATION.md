# Automated API Specification Workflow

This document explains the automated API specification workflow that helps streamline the creation of OpenAPI and Arazzo specifications for use cases.

## Overview

The **Auto-Generate API Specifications** workflow automatically:

1. **Monitors** pushes to the `main`/`master` branch
2. **Scans** all use-case files in `docs/use-cases/` for TODO API specifications
3. **Creates** a new branch named `icds/<use-case-name>` for each TODO found
4. **Generates** placeholder files and task instructions
5. **Opens** a pull request with detailed instructions for GitHub Copilot

## Workflow Trigger

The workflow runs automatically when:
- Code is pushed to `main` or `master` branch
- Manually triggered via GitHub Actions UI (workflow_dispatch)

## How It Works

### Step 1: Scan Use Cases

The workflow searches all use case folders in `docs/use-cases/*/` for this pattern:

```markdown
| **API Specifications (OpenAPI + Arazzo)** | TODO | - |
```

Each use case should be in its own folder: `docs/use-cases/<use-case-name>/`

### Step 2: Create Branch

For each TODO found, the workflow:
- Creates a branch named `icds/<use-case-name>`
- Example: For `createschoolreview.md` → branch `icds/createschoolreview`

### Step 3: Generate Files

The workflow creates or updates files in the new branch:

1. **Task Instructions** (`docs/use-cases/<use-case-name>/api-spec-task.md`)
   - Detailed instructions for what needs to be implemented
   - Checklist of requirements
   - References to the use case document

2. **Shared OpenAPI File** (`src/backend/Api/openapi/openapi.yaml`)
   - Creates the file if it doesn't exist
   - All use cases add their endpoints to this single file
   - Maintains a unified API specification

3. **Shared Arazzo File** (`src/backend/Api/openapi/arazzo.yaml`)
   - Creates the file if it doesn't exist
   - All use cases add their workflows to this single file
   - Maintains unified workflow specifications

4. **Shared Postman Collection** (`src/backend/Api/openapi/postman-collection.json`)
   - Creates the file if it doesn't exist
   - All use cases add their request examples to this single collection
   - Organized by folders (Schools, Reviews, Health)
   - Includes test scripts and variables

### Step 4: Create Pull Request

A PR is created with:
- **Title**: `feat(api-spec): Add API specifications for <use-case-name>`
- **Description**: Comprehensive instructions and task overview
- **Labels**: `api-specification`, `automated`, `copilot-task`
- **References**: Links to use case document, architecture, business rules

### Step 5: Optional Issue

An optional GitHub issue is created to track the task.

## Using GitHub Copilot with Generated PRs

### In the Pull Request

1. Open the generated PR
2. Use Copilot Chat:
   ```
   @workspace Please help me implement the API specifications for this use case 
   following the instructions in the task file.
   ```

### In Your IDE

1. Checkout the branch:
   ```bash
   git fetch origin
   git checkout icds/<use-case-name>
   ```

2. Open the task file:
   ```
   docs/use-cases/<use-case-name>/api-spec-task.md
   ```

3. Use Copilot:
   ```
   @workspace Read the use case folder and create the OpenAPI 
   specification following the task instructions.
   ```

### Example Copilot Prompts

**For OpenAPI Specification:**
```
@workspace In src/backend/Api/openapi/openapi.yaml, add the endpoints for the 
Create School Review use case from docs/use-cases/createschoolreview/. Include:
- POST /schools/{schoolId}/reviews endpoint
- Request schema with all required fields in components/schemas
- Response schemas (201, 400, 404, 500)
- Validation rules and examples
```

**For Arazzo Workflow:**
```
@workspace In src/backend/Api/openapi/arazzo.yaml, add a workflow for creating 
a school review that demonstrates the complete flow described in the use case.
```

**For Updating Use Case Status:**
```
@workspace Update the use case file docs/use-cases/createschoolreview/use-case.md 
to mark "API Specifications (OpenAPI + Arazzo)" as "DONE" with today's date.
```

## Manual Triggering

### Via GitHub UI

1. Go to **Actions** tab
2. Select **Auto-Generate API Specifications from Use Cases**
3. Click **Run workflow**
4. Select branch (main/master)
5. Click **Run workflow** button

### Via GitHub CLI

```bash
gh workflow run auto-api-specifications.yml
```

## File Structure

After the workflow runs, you'll have:

```
RateYourSchool/
├── docs/
│   └── use-cases/
│       └── <use-case-name>/                        # Use case folder
│           ├── use-case.md                         # Use case document
│           └── api-spec-task.md                    # Task instructions
├── src/
│   └── backend/
│       └── Api/
│           └── openapi/
│               ├── openapi.yaml                    # Shared OpenAPI spec (all endpoints)
│               └── arazzo.yaml                     # Shared Arazzo spec (all workflows)
```

## Workflow Configuration

### Permissions Required

The workflow requires these GitHub permissions:
```yaml
permissions:
  contents: write        # Create branches and commits
  pull-requests: write   # Create pull requests
  issues: write          # Create tracking issues
```

### Matrix Strategy

The workflow processes multiple TODOs using a matrix strategy:
- **max-parallel: 1** ensures one PR is created at a time to avoid conflicts
- Each use case gets its own branch and PR

## Customization

### Modify Search Pattern

To change what patterns trigger the workflow, edit the grep patterns in `.github/workflows/auto-api-specifications.yml`:

```bash
# Current patterns checked:
grep -q "| \*\*API Specifications (OpenAPI + Arazzo)\*\* | TODO |" "$file"
grep -q "| **API Specifications (OpenAPI + Arazzo)** | TODO |" "$file"
```

### Change Branch Naming

Modify the branch name pattern:

```bash
BRANCH_NAME="icds/${USE_CASE_NAME}"
# Could change to:
# BRANCH_NAME="api-spec/${USE_CASE_NAME}"
# BRANCH_NAME="feature/api-${USE_CASE_NAME}"
```

### Disable Issue Creation

Comment out or remove the "Create GitHub Issue" step if you don't want tracking issues.

## Best Practices

### 1. Keep Use Cases Updated

Ensure use case files have the Implementation Progress table with clear status indicators:

```markdown
| Task | Status | Completed |
|------|--------|-----------|
| **API Specifications (OpenAPI + Arazzo)** | TODO | - |
```

Valid statuses: `TODO`, `IN-PROGRESS`, `IN-REVIEW`, `DONE`
- Use Copilot to accelerate implementation
- Close PRs that are no longer needed

### 3. Update Use Case After Completion

After implementing the API specs, update the use case file:

```markdown
| **API Specifications (OpenAPI + Arazzo)** | DONE | 2026-03-18 |
```

This prevents the workflow from creating duplicate PRs.

### 4. Use Conventional Commits

When committing the completed specs, use conventional commit format:

```bash
git commit -m "feat(api-spec): add OpenAPI and Arazzo for createschoolreview

Implements comprehensive API specification for the Create School Review use case.
Includes request/response schemas, validation rules, and workflow definition.

Closes #<issue-number>"
```

## Troubleshooting

### No PRs Created

**Check:**
1. Are there use case folders with `TODO` status for API Specifications?
2. Is the pattern matching correctly? (check workflow logs)
3. Do you have the required permissions?

**Solution:**
- Review use case folders in `docs/use-cases/*/`
- Check GitHub Actions logs for error messages
- Verify workflow permissions in repository settings

### Branch Already Exists Error

**Issue:** The branch `icds/<use-case-name>` already exists

**Solution:**
The workflow handles this automatically by checking out existing branches. If there's already a PR, it won't create a duplicate.

### Permission Denied

**Issue:** Workflow fails with permission errors

**Solution:**
1. Go to **Settings** → **Actions** → **General**
2. Under "Workflow permissions", select "Read and write permissions"
3. Enable "Allow GitHub Actions to create and approve pull requests"

### Multiple PRs for Same Use Case

**Issue:** Multiple PRs created for the same use case

**Solution:**
- The workflow checks for existing PRs before creating new ones
- Ensure only one workflow run processes a use case at a time
- Close duplicate PRs manually if they exist

## Integration with Development Workflow

### Recommended Process

1. **Create Use Case**
   - Document the feature in `docs/use-cases/<name>.md`
   - Mark API Specifications as `todo`

2. **Push to Main**
   - Commit and push the use case document
   - Workflow automatically triggers

3. **Review PR**
   - Check the generated PR for accuracy
   - Read the task instructions

4. **Implement with Copilot**
   - Checkout the branch
   - Use Copilot to implement specifications
   - Follow the checklist in the task file

5. **Update and Merge**
   - Update use case status to `done`
   - Request code review
   - Merge the PR

6. **Continue Implementation**
   - Move to next phase (Backend, Frontend, Tests)
   - The use case file tracks overall progress

## Example: Complete Workflow Run

```bash
# 1. Developer creates use case
git checkout -b feature/add-review-use-case
mkdir -p docs/use-cases/createschoolreview
# ... create docs/use-cases/createschoolreview/use-case.md ...
git add docs/use-cases/createschoolreview/
git commit -m "docs(use-case): add Create School Review use case"
git push origin feature/add-review-use-case

# 2. Create PR and merge to main
# ... merge PR ...

# 3. Workflow automatically triggers
# - Detects TODO API specification
# - Creates branch: icds/createschoolreview
# - Creates PR with task instructions

# 4. Developer works on API spec
git fetch origin
git checkout icds/createschoolreview

# Use Copilot to add endpoints to shared files
# @workspace help me add the Create School Review endpoints to openapi.yaml

# 5. Commit completed work
git add src/backend/Api/openapi/openapi.yaml
git add src/backend/Api/openapi/arazzo.yaml
git add docs/use-cases/createschoolreview/api-spec-task.md
git add docs/use-cases/createschoolreview/use-case.md  # Update status to 'DONE'
git commit -m "feat(api-spec): add Create School Review endpoints to OpenAPI spec"
git push origin icds/createschoolreview

# 6. Merge PR
# ... review and merge ...
```

## Future Enhancements

Potential improvements to the workflow:

- **Validation**: Add OpenAPI/Arazzo schema validation
- **Linting**: Integrate spectral or similar linting tools
- **Auto-merge**: Automatically merge if validation passes
- **Notifications**: Send Slack/email notifications
- **Templates**: Use more sophisticated templates based on use case type
- **AI Integration**: Direct integration with GitHub Copilot API when available

## Related Documentation

- [Use Case Template](../../docs/USE-CASE-TEMPLATE.md)
- [Contributing Guide](../../CONTRIBUTING.md)
- [Commit Conventions](.github/COMMIT_CONVENTION.md)
- [Architecture Documentation](../../docs/ARCHITECTURE.md)

---

**Last Updated:** March 18, 2026  
**Workflow Version:** 1.0
