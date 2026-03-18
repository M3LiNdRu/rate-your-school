# Use Case Migration Summary

**Date:** March 18, 2026  
**Migration:** Flat files to folder structure + Status values to UPPERCASE

---

## Changes Made

### 1. Folder Structure

**Before:**
```
docs/use-cases/
├── createschoolreview.md
├── getschoolrate.md
├── getschoolreviews.md
├── getschoolsmap.md
└── getschoolsrates.md
```

**After:**
```
docs/use-cases/
├── createschoolreview/
│   └── use-case.md
├── getschoolrate/
│   └── use-case.md
├── getschoolreviews/
│   └── use-case.md
├── getschoolsmap/
│   └── use-case.md
└── getschoolsrates/
    └── use-case.md
```

Each use case now has its own folder where:
- Main document: `use-case.md`
- Task instructions (auto-generated): `api-spec-task.md`

### 2. Status Values

All status values changed from lowercase to UPPERCASE:

| Old Value | New Value |
|-----------|-----------|
| `todo` | `TODO` |
| `in-progress` | `IN-PROGRESS` |
| `in-review` | `IN-REVIEW` |
| `done` | `DONE` |

### 3. Updated Files

The following files were updated to reflect the new structure and status values:

#### GitHub Workflows
- `.github/workflows/auto-api-specifications.yml`
  - Searches use case folders instead of flat files
  - Looks for `use-case.md` in each folder
  - Generates `api-spec-task.md` in the use case folder
  - Uses UPPERCASE status patterns

#### Documentation
- `.github/API_SPEC_AUTOMATION.md`
  - Updated file paths and examples
  - Changed status values to UPPERCASE
  - Updated folder structure diagrams

- `.github/API_SPEC_QUICK_REFERENCE.md`
  - Updated all examples with new folder structure
  - Changed status values to UPPERCASE
  - Updated file locations

#### Scripts
- `scripts/api-spec-helper.sh`
  - Searches for use case folders
  - Looks for `use-case.md` files
  - Uses UPPERCASE status patterns

#### Templates & Contributing
- `docs/USE-CASE-TEMPLATE.md`
  - Updated status legend to UPPERCASE
  - Updated status placeholders to UPPERCASE

- `CONTRIBUTING.md`
  - Updated examples with folder structure
  - Changed status values to UPPERCASE

- `README.md`
  - Updated references to use folder structure

### 4. Migrated Use Cases

All existing use case files were migrated:

✅ **createschoolreview**
- Location: `docs/use-cases/createschoolreview/use-case.md`
- API Spec Status: `TODO`
- Will trigger workflow ✓

✅ **getschoolrate**
- Location: `docs/use-cases/getschoolrate/use-case.md`
- API Spec Status: `TODO`
- Will trigger workflow ✓

✅ **getschoolreviews**
- Location: `docs/use-cases/getschoolreviews/use-case.md`
- API Spec Status: (check file for status)

✅ **getschoolsmap**
- Location: `docs/use-cases/getschoolsmap/use-case.md`
- API Spec Status: (check file for status)

✅ **getschoolsrates**
- Location: `docs/use-cases/getschoolsrates/use-case.md`
- API Spec Status: `TODO`
- Will trigger workflow ✓

---

## Workflow Behavior

### Before Migration
- Searched for `*.md` files in `docs/use-cases/`
- Pattern: `| **API Specifications** | todo |`
- Created task file in: `src/backend/Api/openapi/tasks/<name>-api-spec.md`

### After Migration
- Searches for folders in `docs/use-cases/*/`
- Looks for `use-case.md` in each folder
- Pattern: `| **API Specifications** | TODO |`
- Creates task file in: `docs/use-cases/<name>/api-spec-task.md`

---

## Testing the Migration

### 1. Check TODO Items
```bash
./scripts/api-spec-helper.sh list
```

Expected output:
- createschoolreview (TODO)
- getschoolrate (TODO)
- getschoolsrates (TODO)

### 2. Check All Statuses
```bash
./scripts/api-spec-helper.sh status
```

### 3. Verify Workflow Will Trigger
```bash
./scripts/api-spec-helper.sh check
```

### 4. Test Workflow (Recommended)
1. Commit the changes to a test branch
2. Push to main/master
3. Check GitHub Actions for workflow run
4. Verify PRs are created with correct structure

---

## Creating New Use Cases

### New Folder Structure

```bash
# Create new use case
mkdir -p docs/use-cases/my-new-use-case
vim docs/use-cases/my-new-use-case/use-case.md
```

### Use Case Template

```markdown
# Use Case: My New Use Case

**Status:** Not Started  
**Priority:** Medium  

---

## Implementation Progress

| Task | Status | Completed |
|------|--------|-----------|
| **Functional Requirements Document** | TODO | - |
| **API Specifications (OpenAPI + Arazzo)** | TODO | - |
| **Backend Implementation** | TODO | - |
| **Frontend Implementation** | TODO | - |
| **E2E Tests** | TODO | - |
| **Penetration Tests** | TODO | - |

**Status Legend:**
- `TODO` - Not started
- `IN-PROGRESS` - Currently being worked on
- `IN-REVIEW` - Completed and under review
- `DONE` - Completed and verified
```

---

## Rollback (If Needed)

If you need to rollback the migration:

```bash
cd /home/rcalaf/apps/RateYourSchool/docs/use-cases

# Move files back
for dir in */; do
  if [ -f "$dir/use-case.md" ]; then
    basename="${dir%/}"
    mv "$dir/use-case.md" "$basename.md"
    rmdir "$dir"
  fi
done

# Convert statuses back to lowercase
find . -name "*.md" -type f -exec sed -i 's/| TODO |/| todo |/g' {} \;
find . -name "*.md" -type f -exec sed -i 's/| DONE |/| done |/g' {} \;
find . -name "*.md" -type f -exec sed -i 's/| IN-PROGRESS |/| in-progress |/g' {} \;
find . -name "*.md" -type f -exec sed -i 's/| IN-REVIEW |/| in-review |/g' {} \;
```

**Note:** You would also need to revert all the updated files listed above.

---

## Benefits of New Structure

1. **Organization**: Each use case has its own folder for related files
2. **Scalability**: Easy to add more files per use case (diagrams, examples, etc.)
3. **Clarity**: UPPERCASE statuses are more visible and consistent
4. **Automation**: Task instructions live with the use case
5. **Maintainability**: Easier to manage and navigate

---

## Next Steps

1. ✅ Migration complete
2. ⏳ Test the workflow by pushing to main
3. ⏳ Verify PRs are created correctly
4. ⏳ Review generated task files in use case folders
5. ⏳ Update any external documentation that references old paths

---

**Migration completed successfully!** ✅

For questions or issues, see:
- [API_SPEC_AUTOMATION.md](.github/API_SPEC_AUTOMATION.md)
- [API_SPEC_QUICK_REFERENCE.md](.github/API_SPEC_QUICK_REFERENCE.md)
- [USE-CASE-TEMPLATE.md](docs/USE-CASE-TEMPLATE.md)
