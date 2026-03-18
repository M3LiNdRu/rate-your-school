# Postman Collection Guide

**Date:** March 18, 2026  
**File:** `src/backend/Api/openapi/postman-collection.json`

---

## Overview

The RateYourSchool API Postman collection provides a complete set of request examples, test scripts, and documentation for testing and exploring the API. The collection is automatically maintained alongside the OpenAPI specification.

## Features

✅ **Request Examples** - Ready-to-use API requests with realistic data  
✅ **Automated Tests** - Pre-configured test scripts for validation  
✅ **Environment Variables** - Easy switching between development and production  
✅ **Organized by Feature** - Requests grouped into logical folders  
✅ **Synchronized with OpenAPI** - Stays in sync with API specification  
✅ **Global Test Scripts** - Common validations applied to all requests

---

## Getting Started

### Import the Collection

**Option 1: Import from File**
1. Open Postman
2. Click **Import** button
3. Select file: `src/backend/Api/openapi/postman-collection.json`
4. Click **Import**

**Option 2: Import from URL** (if hosted)
```
https://raw.githubusercontent.com/OWNER/RateYourSchool/main/src/backend/Api/openapi/postman-collection.json
```

### Set Up Environment

The collection uses variables for flexibility:

| Variable | Default Value | Description |
|----------|---------------|-------------|
| `baseUrl` | `http://localhost:5001/v1` | Development server |
| `prodUrl` | `https://api.rateyourschool.com/v1` | Production server |

**To use production:**
1. In Postman, edit the variable
2. Change request URLs from `{{baseUrl}}` to `{{prodUrl}}`

**Or create environments:**
```json
// Development Environment
{
  "name": "Development",
  "values": [
    { "key": "apiUrl", "value": "http://localhost:5001/v1", "enabled": true }
  ]
}

// Production Environment
{
  "name": "Production",
  "values": [
    { "key": "apiUrl", "value": "https://api.rateyourschool.com/v1", "enabled": true }
  ]
}
```

---

## Collection Structure

```
RateYourSchool API/
├── Schools/              # School-related endpoints
│   ├── Get All Schools
│   ├── Get School by ID
│   └── ...
├── Reviews/              # Review-related endpoints
│   ├── Get School Reviews
│   ├── Create Review
│   └── ...
└── Health/               # Health check endpoints
    └── Health Check
```

---

## Adding New Requests

When implementing a use case, add requests to the appropriate folder:

### Example: GET Request

```json
{
  "name": "Get All Schools",
  "request": {
    "method": "GET",
    "header": [],
    "url": {
      "raw": "{{baseUrl}}/schools?page=1&pageSize=20",
      "host": ["{{baseUrl}}"],
      "path": ["schools"],
      "query": [
        {
          "key": "page",
          "value": "1",
          "description": "Page number (1-based)"
        },
        {
          "key": "pageSize",
          "value": "20",
          "description": "Number of items per page (1-1000)"
        }
      ]
    }
  },
  "response": [],
  "event": [
    {
      "listen": "test",
      "script": {
        "exec": [
          "pm.test('Status code is 200', function () {",
          "    pm.response.to.have.status(200);",
          "});",
          "",
          "pm.test('Returns school list', function () {",
          "    const response = pm.response.json();",
          "    pm.expect(response).to.have.property('schools');",
          "    pm.expect(response.schools).to.be.an('array');",
          "});",
          "",
          "pm.test('Pagination metadata exists', function () {",
          "    const response = pm.response.json();",
          "    pm.expect(response).to.have.property('page');",
          "    pm.expect(response).to.have.property('pageSize');",
          "    pm.expect(response).to.have.property('totalCount');",
          "});"
        ],
        "type": "text/javascript"
      }
    }
  ]
}
```

### Example: POST Request

```json
{
  "name": "Create School Review",
  "request": {
    "method": "POST",
    "header": [
      {
        "key": "Content-Type",
        "value": "application/json"
      }
    ],
    "body": {
      "mode": "raw",
      "raw": "{\n  \"rating\": 4.5,\n  \"comment\": \"Great school with excellent teachers\",\n  \"teachingQuality\": 5,\n  \"facilities\": 4,\n  \"administration\": 4\n}"
    },
    "url": {
      "raw": "{{baseUrl}}/schools/{{schoolId}}/reviews",
      "host": ["{{baseUrl}}"],
      "path": ["schools", "{{schoolId}}", "reviews"]
    }
  },
  "response": [],
  "event": [
    {
      "listen": "test",
      "script": {
        "exec": [
          "pm.test('Status code is 201', function () {",
          "    pm.response.to.have.status(201);",
          "});",
          "",
          "pm.test('Review created successfully', function () {",
          "    const response = pm.response.json();",
          "    pm.expect(response).to.have.property('reviewId');",
          "    pm.expect(response.reviewId).to.be.a('string');",
          "});",
          "",
          "// Save reviewId for subsequent requests",
          "if (pm.response.code === 201) {",
          "    const response = pm.response.json();",
          "    pm.collectionVariables.set('lastReviewId', response.reviewId);",
          "}"
        ],
        "type": "text/javascript"
      }
    }
  ]
}
```

---

## Test Scripts

### Global Tests

The collection includes global test scripts that run for all requests:

```javascript
// Global test script (runs after every request)
pm.test("Status code should be successful", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 201, 204]);
});

pm.test("Response time is acceptable", function () {
    pm.expect(pm.response.responseTime).to.be.below(1000);
});

pm.test("Content-Type is application/json", function () {
    pm.expect(pm.response.headers.get('Content-Type')).to.include('application/json');
});
```

### Request-Specific Tests

Add tests for specific validations:

```javascript
// Validate response schema
pm.test("Response has correct schema", function () {
    const schema = {
        type: "object",
        required: ["schools", "page", "pageSize", "totalCount"],
        properties: {
            schools: { type: "array" },
            page: { type: "number" },
            pageSize: { type: "number" },
            totalCount: { type: "number" }
        }
    };
    pm.response.to.have.jsonSchema(schema);
});

// Validate business rules
pm.test("Page size is within allowed range (BR-027)", function () {
    const response = pm.response.json();
    pm.expect(response.pageSize).to.be.at.least(1);
    pm.expect(response.pageSize).to.be.at.most(1000);
});

// Save data for chained requests
pm.test("Save school ID for later use", function () {
    const response = pm.response.json();
    if (response.schools && response.schools.length > 0) {
        pm.collectionVariables.set('schoolId', response.schools[0].id);
    }
});
```

---

## Pre-Request Scripts

Use pre-request scripts to set up data before requests:

```javascript
// Generate dynamic data
pm.collectionVariables.set('timestamp', new Date().toISOString());
pm.collectionVariables.set('uuid', pm.variables.replaceIn('{{$guid}}'));

// Set authentication (if needed in future)
// pm.request.headers.add({
//     key: 'Authorization',
//     value: 'Bearer ' + pm.collectionVariables.get('authToken')
// });
```

---

## Workflow Testing

Test complete workflows using Postman's Collection Runner:

### Example: School Review Workflow

1. **Get All Schools** → Save first school ID
2. **Get School Details** → Using saved ID
3. **Create Review** → For saved school
4. **Get School Reviews** → Verify review appears
5. **Get Updated School** → Check rating updated

**Setup:**
```javascript
// In "Get All Schools" test:
pm.collectionVariables.set('schoolId', response.schools[0].id);

// In "Create Review" test:
pm.collectionVariables.set('reviewId', response.reviewId);
```

---

## Best Practices

### 1. Use Collection Variables

```javascript
// Set variables for reuse
pm.collectionVariables.set('schoolId', 'school-123');
pm.collectionVariables.set('reviewId', 'review-456');

// Use in URLs
{{baseUrl}}/schools/{{schoolId}}/reviews/{{reviewId}}
```

### 2. Add Descriptions

```json
{
  "name": "Get School by ID",
  "request": {
    "description": "Retrieves detailed information for a specific school including rating and recent reviews. Implements use case: getschoolrate (BR-009 to BR-015).",
    ...
  }
}
```

### 3. Provide Examples

Add example responses to document expected behavior:

```json
{
  "response": [
    {
      "name": "Success - School Found",
      "originalRequest": { ... },
      "status": "OK",
      "code": 200,
      "_postman_previewlanguage": "json",
      "body": "{\n  \"id\": \"school-123\",\n  \"name\": \"Example School\",\n  ...  \n}"
    },
    {
      "name": "Not Found - School ID Invalid",
      "status": "Not Found",
      "code": 404,
      "body": "{\n  \"error\": \"NOT_FOUND\",\n  \"message\": \"School not found\"\n}"
    }
  ]
}
```

### 4. Reference Business Rules

```javascript
pm.test("Page size respects BR-027 (max 1000)", function () {
    const response = pm.response.json();
    pm.expect(response.pageSize).to.be.at.most(1000);
});

pm.test("Rating within valid range (BR-018)", function () {
    const response = pm.response.json();
    pm.expect(response.rating).to.be.at.least(0);
    pm.expect(response.rating).to.be.at.most(5);
});
```

---

## Generating from OpenAPI

The Postman collection can be generated from the OpenAPI specification:

### Using openapi-to-postman CLI

```bash
# Install the tool
npm install -g openapi-to-postman

# Generate collection
openapi2postmanv2 \
  -s src/backend/Api/openapi/openapi.yaml \
  -o src/backend/Api/openapi/postman-collection-generated.json \
  -p

# Review and merge with existing collection
```

### Using Postman's Import

1. In Postman, click **Import**
2. Select **Link** tab
3. Paste OpenAPI file URL or upload file
4. Postman will generate collection automatically
5. Review and customize as needed

**Note:** Auto-generated collections need manual enhancement:
- Add test scripts
- Add pre-request scripts
- Add descriptions
- Add example responses
- Organize into folders

---

## Validation

Validate the collection structure:

### Using Newman (Postman CLI)

```bash
# Install Newman
npm install -g newman

# Run collection
newman run src/backend/Api/openapi/postman-collection.json \
  --environment development.postman_environment.json

# Run with detailed output
newman run src/backend/Api/openapi/postman-collection.json \
  --reporters cli,json \
  --reporter-json-export results.json
```

### JSON Schema Validation

```bash
# Validate JSON structure
jq empty src/backend/Api/openapi/postman-collection.json

# Check for required properties
jq '.info, .item, .variable' src/backend/Api/openapi/postman-collection.json
```

---

## Continuous Integration

Run Postman tests in CI/CD pipelines:

```yaml
# .github/workflows/api-tests.yml
name: API Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Start API server
        run: docker-compose up -d
      
      - name: Install Newman
        run: npm install -g newman
      
      - name: Run Postman tests
        run: |
          newman run src/backend/Api/openapi/postman-collection.json \
            --environment ci.postman_environment.json \
            --reporters cli,junit \
            --reporter-junit-export results.xml
      
      - name: Publish test results
        uses: EnricoMi/publish-unit-test-result-action@v2
        if: always()
        with:
          files: results.xml
```

---

## Troubleshooting

### Collection Not Importing

**Issue:** Postman fails to import collection

**Solutions:**
1. Validate JSON syntax: `jq empty postman-collection.json`
2. Check schema version is v2.1.0
3. Ensure all required fields are present

### Variables Not Working

**Issue:** `{{baseUrl}}` not resolving

**Solutions:**
1. Check variable is defined in collection
2. Verify variable name spelling
3. Check scope (collection vs. environment)

### Tests Failing

**Issue:** Tests fail unexpectedly

**Solutions:**
1. Check API server is running
2. Verify baseUrl points to correct server
3. Check request data is valid
4. Review test assertions

---

## Contributing

When adding new API endpoints:

1. ✅ Add request to appropriate folder (Schools, Reviews, Health)
2. ✅ Include realistic example data
3. ✅ Add test scripts for validation
4. ✅ Use collection variables for dynamic data
5. ✅ Add response examples (success and error cases)
6. ✅ Reference business rules in tests
7. ✅ Add descriptions explaining the endpoint
8. ✅ Validate JSON syntax before committing

---

## Resources

- **Postman Documentation**: https://learning.postman.com/
- **Postman Collection v2.1.0 Schema**: https://schema.getpostman.com/
- **Newman CLI**: https://learning.postman.com/docs/running-collections/using-newman-cli/
- **OpenAPI to Postman**: https://github.com/postmanlabs/openapi-to-postman

---

**Last Updated:** March 18, 2026  
**Maintained by:** RateYourSchool Team
