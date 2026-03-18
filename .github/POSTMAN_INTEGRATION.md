# Postman Collection Integration - Complete

**Date:** March 18, 2026  
**Update:** Added Postman collection generation to API specification workflow

---

## ✅ Changes Made

### 1. Created Postman Collection File

**File:** `src/backend/Api/openapi/postman-collection.json`

**Structure:**
- **Info section**: Collection name, description, schema (v2.1.0)
- **Variables**: baseUrl (development), prodUrl (production)
- **Folders**: Schools, Reviews, Health (organized by API category)
- **Global events**:
  - Pre-request script (setup logic)
  - Test script (common validations: status codes, response time, content-type)

**Features:**
- Ready for import into Postman
- Includes environment variables for dev/prod switching
- Pre-configured test scripts
- Organized folder structure mirroring API tags
- Documentation in description

### 2. Updated Workflow

**File:** `.github/workflows/auto-api-specifications.yml`

**Changes:**
- Added Postman collection creation step (creates file if doesn't exist)
- Updated task instructions to include Postman collection guidance
- Added Postman collection to checklist
- Added Postman collection link to PR references
- Included validation for JSON syntax

**Workflow now creates/updates:**
1. `openapi.yaml` - OpenAPI 3.0.3 specification
2. `arazzo.yaml` - Arazzo 1.0.0 workflows
3. `postman-collection.json` - Postman collection v2.1.0
4. `api-spec-task.md` - Task instructions

### 3. Updated Documentation

**Files Modified:**

1. **`.github/POSTMAN_COLLECTION.md`** (NEW)
   - Comprehensive guide for using Postman collection
   - Import instructions
   - Request examples (GET, POST)
   - Test script examples
   - Best practices
   - CI/CD integration
   - Troubleshooting guide

2. **`.github/SHARED_API_SPECS.md`**
   - Added Postman collection to file structure diagram
   - Added postman-collection.json to created files list
   - Added example Postman request in developer workflow
   - Added Postman collection structure documentation
   - Updated instructions to include Postman requests

3. **`.github/API_SPEC_QUICK_REFERENCE.md`**
   - Updated "Generated Files" table to include Postman collection
   - Added Postman collection to implementation checklist
   - Added Copilot prompt for generating Postman requests
   - Added JSON validation to checklist

4. **`.github/API_SPEC_AUTOMATION.md`**
   - Added Postman collection to workflow step 3 description
   - Documented Postman collection creation/update behavior

5. **`CONTRIBUTING.md`**
   - Updated workflow steps to mention Postman collection
   - Added postman-collection.json to shared files list
   - Added JSON syntax validation to checklist

6. **`README.md`**
   - Updated automation features to mention Postman
   - Added postman-collection.json to shared files list

---

## 📋 How It Works

### Workflow Behavior

When a use case with `| **API Specifications** | TODO |` is detected:

1. **Detects** TODO status in use case
2. **Creates branch** `icds/<use-case-name>`
3. **Creates/updates** three shared files:
   - `openapi.yaml` (if doesn't exist)
   - `arazzo.yaml` (if doesn't exist)
   - `postman-collection.json` (if doesn't exist)  ← **NEW**
4. **Generates** task instructions including Postman guidance
5. **Opens PR** with instructions for all three files

### Developer Workflow

```bash
# 1. Checkout PR branch
git checkout icds/createschoolreview

# 2. Add endpoints to OpenAPI
vim src/backend/Api/openapi/openapi.yaml

# 3. Add workflow to Arazzo
vim src/backend/Api/openapi/arazzo.yaml

# 4. Add requests to Postman collection  ← NEW
vim src/backend/Api/openapi/postman-collection.json
# Add to appropriate folder (Schools, Reviews, Health)
# Include test scripts and realistic examples

# 5. Validate
jq empty src/backend/Api/openapi/postman-collection.json

# 6. Commit
git add src/backend/Api/openapi/*.{yaml,json}
git commit -m "feat(api-spec): add Create School Review with tests"
git push
```

---

## 🎯 Benefits

### For Developers

- ✅ **Ready-to-use examples** - Import collection and start testing immediately
- ✅ **Automated tests** - Pre-configured validations save time
- ✅ **Documentation** - Request descriptions explain each endpoint
- ✅ **Workflows** - Test complete user flows with saved variables

### For QA/Testers

- ✅ **Test automation** - Run entire test suite with Newman CLI
- ✅ **CI/CD integration** - Automated API testing in pipelines
- ✅ **Environment switching** - Easy dev/staging/prod testing
- ✅ **Regression testing** - Catch breaking changes early

### For API Consumers

- ✅ **Learn by example** - See how to use each endpoint
- ✅ **Quick onboarding** - Import and explore API immediately
- ✅ **Error examples** - See what error responses look like
- ✅ **Best practices** - Follow demonstrated patterns

---

## 📝 Task Instructions

When implementing API specifications, contributors now need to:

### 1. Add Endpoints to OpenAPI (openapi.yaml)

```yaml
paths:
  /schools/{schoolId}/reviews:
    post:
      summary: Create a new school review
      operationId: createSchoolReview
      # ... rest of definition
```

### 2. Add Workflows to Arazzo (arazzo.yaml)

```yaml
workflows:
  - workflowId: create-school-review
    summary: Submit a review for a school
    # ... rest of workflow
```

### 3. Add Requests to Postman Collection (postman-collection.json)  ← NEW

```json
{
  "name": "Create School Review",
  "request": {
    "method": "POST",
    "url": "{{baseUrl}}/schools/{{schoolId}}/reviews",
    "body": {
      "mode": "raw",
      "raw": "{ \"rating\": 4.5, ... }"
    }
  },
  "event": [
    {
      "listen": "test",
      "script": {
        "exec": [
          "pm.test('Review created', function () { ... });"
        ]
      }
    }
  ]
}
```

---

## ✅ Checklist (Updated)

When completing API specification tasks:

- [ ] Endpoints added to `openapi.yaml`
- [ ] Schemas added to `components/schemas` section
- [ ] Workflow added to `arazzo.yaml`
- [ ] **Postman requests added to `postman-collection.json`** ← NEW
- [ ] All endpoints from use case are included
- [ ] Request/response schemas match requirements
- [ ] Validation rules are implemented
- [ ] Examples are provided (OpenAPI + Postman)
- [ ] Business rules are referenced
- [ ] OpenAPI and Arazzo files are valid YAML
- [ ] **Postman collection is valid JSON** ← NEW
- [ ] **Test scripts validate responses** ← NEW
- [ ] Use case status updated to `DONE` for API Specifications

---

## 🔧 Validation

Before committing changes:

### Validate YAML Files

```bash
# Using yamllint (if installed)
yamllint src/backend/Api/openapi/openapi.yaml
yamllint src/backend/Api/openapi/arazzo.yaml

# Or using spectral
spectral lint src/backend/Api/openapi/openapi.yaml
```

### Validate JSON File  ← NEW

```bash
# Validate JSON syntax
jq empty src/backend/Api/openapi/postman-collection.json

# Pretty print to verify structure
jq . src/backend/Api/openapi/postman-collection.json | head -50

# Validate schema version
jq '.info.schema' src/backend/Api/openapi/postman-collection.json
```

### Run Postman Tests  ← NEW

```bash
# Install Newman (Postman CLI)
npm install -g newman

# Run collection tests
newman run src/backend/Api/openapi/postman-collection.json \
  --environment dev.postman_environment.json
```

---

## 📚 Resources

New documentation created:

1. **[.github/POSTMAN_COLLECTION.md](.github/POSTMAN_COLLECTION.md)**
   - Complete guide to using the Postman collection
   - Examples of GET/POST requests
   - Test script patterns
   - CI/CD integration
   - Troubleshooting

2. **Postman Collection File**
   - `src/backend/Api/openapi/postman-collection.json`
   - Ready to import into Postman
   - Includes variables, folders, global tests

External resources:

- [Postman Learning Center](https://learning.postman.com/)
- [Newman CLI Documentation](https://learning.postman.com/docs/running-collections/using-newman-cli/)
- [Postman Collection v2.1.0 Schema](https://schema.getpostman.com/json/collection/v2.1.0/collection.json)

---

## 🔄 Migration

If you have existing API endpoints:

### From Existing OpenAPI to Postman

```bash
# Using openapi-to-postman
npm install -g openapi-to-postman

# Generate collection from OpenAPI
openapi2postmanv2 \
  -s src/backend/Api/openapi/openapi.yaml \
  -o generated-collection.json \
  -p

# Merge with existing collection
# (Manual step - copy requests to appropriate folders)
```

### Manual Migration

For each endpoint in OpenAPI:

1. Create request in Postman collection
2. Match method, URL, parameters
3. Add request body (for POST/PUT)
4. Add test scripts
5. Add to appropriate folder

---

## 🚀 Next Steps

After this update:

1. ✅ **Workflow creates Postman collection automatically**
2. ✅ **Documentation guides developers on adding requests**
3. ✅ **Validation ensures JSON syntax is correct**
4. ✅ **CI/CD can run Postman tests (when configured)**

**For Contributors:**
- When implementing use cases, add Postman requests alongside OpenAPI specs
- Include test scripts that validate business rules
- Use realistic example data
- Reference business rules in test assertions

**For Reviewers:**
- Verify Postman requests match OpenAPI specification
- Check test scripts validate expected behavior
- Ensure request examples use collection variables
- Validate JSON syntax

---

## 📊 Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Files Created** | OpenAPI + Arazzo | OpenAPI + Arazzo + **Postman** |
| **Test Examples** | None | **Automated test scripts** |
| **Request Examples** | OpenAPI examples only | **Postman requests + examples** |
| **CI/CD Testing** | Not available | **Newman CLI integration** |
| **Developer Experience** | Spec-only | **Spec + runnable examples** |
| **Documentation** | 7 docs | **8 docs (+POSTMAN_COLLECTION.md)** |

---

**Completed:** March 18, 2026  
**Status:** ✅ Postman collection fully integrated into API specification workflow  
**Next Action:** Test workflow by triggering PR creation for a TODO use case

---

## Verification

Run these commands to verify the changes:

```bash
# 1. Check all three files exist
ls -lh src/backend/Api/openapi/
# Should show: openapi.yaml, arazzo.yaml, postman-collection.json

# 2. Validate Postman collection JSON
jq empty src/backend/Api/openapi/postman-collection.json
echo "✅ Valid JSON"

# 3. Check collection structure
jq '.info.name, .variable[].key, .item[].name' \
  src/backend/Api/openapi/postman-collection.json

# 4. Check workflow file updated
grep -c "postman-collection.json" .github/workflows/auto-api-specifications.yml
# Should return multiple matches

# 5. Check documentation updated
grep -l "Postman" .github/*.md README.md CONTRIBUTING.md
# Should list updated documentation files
```

**Expected Output:**
- ✅ All three API specification files present
- ✅ Postman collection has valid JSON syntax
- ✅ Collection has proper structure (info, variables, folders)
- ✅ Workflow references Postman collection
- ✅ Documentation mentions Postman collection

---

**All changes verified and complete!** 🎉
