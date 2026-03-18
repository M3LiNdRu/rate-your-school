# API Specification Workflow - Quick Reference

## 🚀 Quick Start

### For Developers

1. **Create a use case** with TODO API specification:
   ```markdown
   | **API Specifications (OpenAPI + Arazzo)** | TODO | - |
   ```

2. **Push to main** - Workflow triggers automatically

3. **Check for PRs** - Look for PR titled: `feat(api-spec): Add API specifications for <use-case-name>`

4. **Work on the task**:
   ```bash
   git fetch origin
   git checkout icds/<use-case-name>
   ```

5. **Use Copilot** in PR or IDE:
   ```
   @workspace Please implement the API specifications following the task instructions.
   ```

6. **Update status** when done:
   ```markdown
   | **API Specifications (OpenAPI + Arazzo)** | DONE | 2026-03-18 |
   ```

---

## 📋 Branch Naming Convention

All API specification branches follow this pattern:
```
icds/<use-case-name>
```

Examples:
- `icds/createschoolreview`
- `icds/getschoolrate`
- `icds/getschoolsmap`

---

## 🛠️ Helper Commands

### Check for TODO API Specs
```bash
./scripts/api-spec-helper.sh list
```

### View All Status
```bash
./scripts/api-spec-helper.sh status
```

### Check if Workflow Will Trigger
```bash
./scripts/api-spec-helper.sh check
```

### Manual Workflow Trigger
```bash
gh workflow run auto-api-specifications.yml
```

---

## 📁 Generated Files

Each TODO creates or updates 4 files:

| File | Location | Purpose |
|------|----------|---------|
| **Use Case Document** | `docs/use-cases/<name>/use-case.md` | Main use case documentation |
| **Task Instructions** | `docs/use-cases/<name>/api-spec-task.md` | Detailed implementation guide |
| **OpenAPI Spec** | `src/backend/Api/openapi/openapi.yaml` | Shared API specification (updated) |
| **Arazzo Workflow** | `src/backend/Api/openapi/arazzo.yaml` | Shared workflow spec (updated) |
| **Postman Collection** | `src/backend/Api/openapi/postman-collection.json` | Shared test collection (updated) |

---

## ✅ Implementation Checklist

When implementing API specifications:

- [ ] Read the use case document
- [ ] Review task instructions
- [ ] Add endpoints to \`openapi.yaml\`
  - [ ] Define paths and operations
  - [ ] Define request/response schemas
  - [ ] Add models to \`components/schemas\`
  - [ ] Add validation rules
  - [ ] Include examples
  - [ ] Reference business rules
- [ ] Add workflow to \`arazzo.yaml\`
  - [ ] Define workflow steps
  - [ ] Define parameters
  - [ ] Define success criteria
- [ ] Add Postman requests to `postman-collection.json`
  - [ ] Add to appropriate folder (Schools, Reviews, etc.)
  - [ ] Include request examples with realistic data
  - [ ] Add test scripts for validation
  - [ ] Use collection variables ({{baseUrl}})
- [ ] Validate specification files
  - [ ] YAML syntax valid (openapi.yaml, arazzo.yaml)
  - [ ] JSON syntax valid (postman-collection.json)
- [ ] Update use case status to `DONE`
- [ ] Commit with conventional commit message

---

## 💬 Copilot Prompts

### Generate OpenAPI Spec
```
@workspace In src/backend/Api/openapi/openapi.yaml, add the endpoints for 
<use-case-name> from docs/use-cases/<use-case-name>/. Add schemas to 
components/schemas and paths to the paths section.
```

### Generate Arazzo Workflow
```
@workspace In src/backend/Api/openapi/arazzo.yaml, add a workflow for 
<use-case-name> that demonstrates the complete flow.
```

### Generate Postman Collection Requests
```
@workspace In src/backend/Api/openapi/postman-collection.json, add request 
examples for <use-case-name>. Add to the appropriate folder (Schools, Reviews, etc.), 
include test scripts, and use {{baseUrl}} variable for URLs.
```

### Update Use Case Status
```
@workspace Update docs/use-cases/<use-case-name>/use-case.md to mark 
"API Specifications (OpenAPI + Arazzo)" as "DONE" with today's date.
```

---

## 🔄 Workflow Trigger Conditions

The workflow runs when:
- ✅ Code pushed to `main` or `master` branch
- ✅ Manually triggered via GitHub Actions UI
- ✅ At least one use case has `| todo |` status for API Specifications

The workflow will NOT run when:
- ❌ No TODO API specifications exist
- ❌ All use cases are marked `DONE`, `IN-PROGRESS`, or `IN-REVIEW`

---

## 🎯 Status Values

Use these exact status values in use case files:

| Status | Meaning | When to Use |
|--------|---------|-------------|
| `TODO` | Not started | API spec needs to be created |
| `IN-PROGRESS` | Being worked on | Currently implementing |
| `IN-REVIEW` | Under review | PR is open, awaiting review |
| `DONE` | Completed | API spec is merged |

---

## 📊 Workflow Outputs

### Pull Request
- **Title**: `feat(api-spec): Add API specifications for <use-case-name>`
- **Labels**: `api-specification`, `automated`, `copilot-task`
- **Branch**: `icds/<use-case-name>`
- **Description**: Comprehensive task instructions

### GitHub Issue (Optional)
- **Title**: `API Specification needed: <use-case-title>`
- **Labels**: `api-specification`, `automated`
- **Purpose**: Track task completion

---

## 🔧 Troubleshooting

### No PR Created?
1. Check use case file has correct format:
   ```markdown
   | **API Specifications (OpenAPI + Arazzo)** | TODO | - |
   ```
2. Check GitHub Actions logs for errors
3. Verify workflow permissions are set correctly

### Multiple PRs for Same Use Case?
- Workflow checks for existing PRs before creating new ones
- Close duplicate PRs manually if they exist

### Branch Already Exists?
- Workflow will checkout and update existing branch
- Won't fail if branch already exists

### Permissions Error?
1. Go to Settings → Actions → General
2. Set "Workflow permissions" to "Read and write"
3. Enable "Allow GitHub Actions to create and approve pull requests"

---

## 📖 Related Documentation

| Document | Purpose |
|----------|---------|
| [API_SPEC_AUTOMATION.md](.github/API_SPEC_AUTOMATION.md) | Complete workflow documentation |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | Contributing guidelines |
| [COMMIT_CONVENTION.md](.github/COMMIT_CONVENTION.md) | Commit message standards |
| [USE-CASE-TEMPLATE.md](../docs/USE-CASE-TEMPLATE.md) | Use case document template |

---

## 🎓 Example: Complete Flow

```bash
# 1. Create use case (or update existing)
mkdir -p docs/use-cases/createschoolreview
vim docs/use-cases/createschoolreview/use-case.md
# Set API Specifications status to "TODO"

# 2. Commit and push
git add docs/use-cases/createschoolreview/
git commit -m "docs(use-case): add Create School Review use case"
git push origin main

# 3. Workflow runs automatically
# - Creates branch: icds/createschoolreview
# - Opens PR with instructions

# 4. Checkout branch
git fetch origin
git checkout icds/createschoolreview

# 5. Implement with Copilot
# Open task file: docs/use-cases/createschoolreview/api-spec-task.md
# Use Copilot to generate specs

# 6. Commit implementation
git add .
git commit -m "feat(api-spec): add OpenAPI and Arazzo for createschoolreview"
git push origin icds/createschoolreview

# 7. PR is ready for review
# 8. After merge, workflow won't trigger again (status is "DONE")
```

---

**Last Updated:** March 18, 2026  
**Version:** 1.0
