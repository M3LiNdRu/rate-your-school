# Shared OpenAPI & Arazzo Specifications - Final Update

**Date:** March 18, 2026  
**Update:** Changed from per-use-case files to shared specification files

---

## ✅ Changes Made

### 1. File Structure Change

**Before:**
```
src/backend/Api/
├── openapi/
│   ├── createschoolreview.openapi.yaml
│   ├── getschoolrate.openapi.yaml
│   └── getschoolsrates.openapi.yaml
└── arazzo/
    ├── createschoolreview.arazzo.yaml
    ├── getschoolrate.arazzo.yaml
    └── getschoolsrates.arazzo.yaml
```

**After:**
```
src/backend/Api/
└── openapi/
    ├── openapi.yaml                # Single shared file for ALL endpoints
    ├── arazzo.yaml                 # Single shared file for ALL workflows
    └── postman-collection.json     # Single Postman collection for ALL requests
```

### 2. Rationale

**Benefits of shared files:**
- ✅ **Single source of truth** for the entire API
- ✅ **Easier to maintain** - one file to update
- ✅ **Better for API clients** - single spec to import
- ✅ **Consistent structure** - all endpoints follow same patterns
- ✅ **Easier validation** - validate entire API at once
- ✅ **Better documentation** - unified API docs
- ✅ **Industry standard** - most APIs use a single OpenAPI file

### 3. How It Works Now

#### Workflow Behavior

When a use case with `| **API Specifications** | TODO |` is detected:

1. **Creates/updates** the shared files if they don't exist:
   - `src/backend/Api/openapi/openapi.yaml` - Created with base structure
   - `src/backend/Api/openapi/arazzo.yaml` - Created with base structure
   - `src/backend/Api/openapi/postman-collection.json` - Created with base Postman collection

2. **Creates task file** in the use case folder:
   - `docs/use-cases/<use-case-name>/api-spec-task.md`

3. **Opens PR** with instructions to:
   - Add endpoints to the `paths` section in `openapi.yaml`
   - Add schemas to the `components/schemas` section in `openapi.yaml`
   - Add workflows to the `workflows` section in `arazzo.yaml`
   - Add request examples to the appropriate folder in `postman-collection.json`

#### Developer Workflow

```bash
# 1. Checkout the automated PR branch
git checkout icds/createschoolreview

# 2. Add endpoints to the shared openapi.yaml
vim src/backend/Api/openapi/openapi.yaml
# Add to paths section:
# /schools/{schoolId}/reviews:
#   post:
#     ...

# 3. Add schemas to the components section
# Add to components/schemas:
# CreateReviewRequest:
#   type: object
#   ...

# 4. Add workflow to arazzo.yaml
# 4. Add workflows to the shared arazzo.yaml
vim src/backend/Api/openapi/arazzo.yaml
# Add to workflows section:
# - workflowId: create-school-review
#   ...

# 5. Add Postman requests to the collection
vim src/backend/Api/openapi/postman-collection.json
# Add to appropriate folder (Schools, Reviews, etc.):
# {
#   "name": "Create School Review",
#   "request": { ... }
# }

# 6. Commit and push
git add src/backend/Api/openapi/openapi.yaml
git add src/backend/Api/openapi/arazzo.yaml
git add src/backend/Api/openapi/postman-collection.json
git commit -m "feat(api-spec): add Create School Review endpoints"
git push
```

### 4. Updated Files

**GitHub Workflows:**
- `.github/workflows/auto-api-specifications.yml`
  - Creates/updates shared `openapi.yaml` and `arazzo.yaml`
  - No longer creates per-use-case files
  - Initializes shared files with proper structure

**Documentation:**
- `.github/API_SPEC_AUTOMATION.md` - Updated examples and file references
- `.github/API_SPEC_QUICK_REFERENCE.md` - Updated Copilot prompts
- `CONTRIBUTING.md` - Added note about shared files
- `README.md` - Updated feature description

**Created Files:**
- `src/backend/Api/openapi/openapi.yaml` - Shared OpenAPI 3.0.3 specification
- `src/backend/Api/openapi/arazzo.yaml` - Shared Arazzo 1.0.0 specification
- `src/backend/Api/openapi/postman-collection.json` - Shared Postman collection v2.1.0

### 5. Initial File Structure

Both files have been created with:

#### openapi.yaml
- **Info section**: Title, description, version, contact
- **Servers**: Production and development URLs
- **Tags**: Schools, Reviews, Health
- **Paths**: TODO section with examples
- **Components**:
  - Common Error schema
  - Standard response types (BadRequest, NotFound, InternalServerError)
  - Common parameters (Page, PageSize)
- **Comments**: Clear guidance on where to add use case-specific content

#### arazzo.yaml
- **Info section**: Title, description, version
- **Source descriptions**: References to openapi.yaml
- **Workflows**: TODO section with example workflow structure
- **Comments**: Clear guidance on adding use case workflows

#### postman-collection.json
- **Info section**: Collection name, description, schema version
- **Variables**: baseUrl (dev), prodUrl (production)
- **Folders**: Schools, Reviews, Health (organized by feature)
- **Global scripts**:
  - Pre-request: Setup logic (timestamps, IDs)
  - Tests: Common validations (status codes, response time, content-type)
- **Comments**: Clear structure for adding use case-specific requests

### 6. Migration from Previous Approach

If any work was done on per-use-case files, they should be:

1. **Merged into shared files**:
   ```bash
   # Extract paths from individual files
   # Add to paths section in openapi.yaml
   
   # Extract schemas from individual files
   # Add to components/schemas section in openapi.yaml
   
   # Extract workflows from individual files
   # Add to workflows section in arazzo.yaml
   ```

2. **Old files can be deleted** (if they exist):
   ```bash
   rm -rf src/backend/Api/arazzo/
   rm src/backend/Api/openapi/*-*.yaml 2>/dev/null || true
   ```

### 7. Copilot Prompts (Updated)

**Add endpoints:**
```
@workspace In src/backend/Api/openapi/openapi.yaml, add the endpoints for the 
Create School Review use case from docs/use-cases/createschoolreview/. Add:
- POST /schools/{schoolId}/reviews to the paths section
- CreateReviewRequest and CreateReviewResponse schemas to components/schemas
- Include validation rules and examples
```

**Add workflow:**
```
@workspace In src/backend/Api/openapi/arazzo.yaml, add a workflow called 
"create-school-review" that demonstrates the complete flow of submitting a review.
```

### 8. Benefits for Teams

1. **Easier collaboration**: Everyone works on the same files
2. **Better merging**: Less risk of conflicting files
3. **Clearer ownership**: The API specification as a whole is the artifact
4. **API versioning**: Easier to version the entire API together
5. **Tool support**: Better support from OpenAPI tools (Swagger UI, etc.)
6. **Documentation**: Single source for generating API documentation

### 9. File Organization Best Practices

Within the shared files, use comments to organize by feature:

```yaml
paths:
  # ========================================
  # Schools Endpoints
  # ========================================
  /schools:
    get:
      tags: [Schools]
      summary: List schools
      ...
  
  /schools/{schoolId}:
    get:
      tags: [Schools]
      summary: Get school details
      ...
  
  # ========================================
  # Reviews Endpoints
  # ========================================
  /schools/{schoolId}/reviews:
    get:
      tags: [Reviews]
      summary: Get reviews for a school
      ...
    
    post:
      tags: [Reviews]
      summary: Create a review
      ...
```

### 10. Validation

Both files can be validated using standard tools:

```bash
# Validate OpenAPI
npx @stoplight/spectral-cli lint src/backend/Api/openapi/openapi.yaml

# Validate Arazzo
# Use arazzo-cli when available
```

---

## Summary

✅ **Changed from**: Per-use-case OpenAPI and Arazzo files  
✅ **Changed to**: Single shared `openapi.yaml` and `arazzo.yaml`  
✅ **Files created**: Both shared files with proper structure  
✅ **Documentation updated**: All references updated  
✅ **Workflow updated**: Creates/maintains shared files  

**All use cases now contribute to the same unified API specification!** 🎉

This approach follows industry best practices and makes the API easier to maintain, document, and consume.
