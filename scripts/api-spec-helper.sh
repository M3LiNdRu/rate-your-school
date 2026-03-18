#!/bin/bash
#
# API Specification Helper Script
# 
# This script helps manage API specification tasks for use cases.
# It can list TODO items, check status, and provide information about
# the automated workflow.
#
# Usage:
#   ./api-spec-helper.sh [command]
#
# Commands:
#   list        - List all use cases with TODO API specifications
#   status      - Show status of all API specifications
#   check       - Check if workflow should trigger
#   help        - Show this help message
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Function to print colored output
print_header() {
    echo -e "${BLUE}===================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}===================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

# Function to list TODO API specifications
list_todos() {
    print_header "API Specifications - TODO Status"
    
    USE_CASES_DIR="$REPO_ROOT/docs/use-cases"
    
    if [ ! -d "$USE_CASES_DIR" ]; then
        print_error "Use cases directory not found: $USE_CASES_DIR"
        exit 1
    fi
    
    TODO_COUNT=0
    echo ""
    
    for dir in "$USE_CASES_DIR"/*/; do
        if [ -d "$dir" ]; then
            # Look for the main use case markdown file
            file="${dir}use-case.md"
            if [ ! -f "$file" ]; then
              # Fallback: look for any .md file in the folder
              file=$(find "$dir" -maxdepth 1 -name "*.md" -type f | head -n 1)
            fi
            
            if [ -f "$file" ]; then
                # Check for TODO API Specifications
                if grep -q "| \*\*API Specifications (OpenAPI + Arazzo)\*\* | TODO |" "$file" || \
                   grep -q "| **API Specifications (OpenAPI + Arazzo)** | TODO |" "$file" || \
                   grep -q "| \*\*API Specifications\*\* | TODO |" "$file" || \
                   grep -q "| **API Specifications** | TODO |" "$file"; then
                
                    BASENAME=$(basename "$dir")
                    TITLE=$(grep -m 1 "^# " "$file" | sed 's/^# //' || echo "$BASENAME")
                
                    echo -e "📋 ${YELLOW}TODO${NC}: $TITLE"
                    echo "   Folder: $dir"
                    echo "   File: $file"
                    echo "   Branch: icds/$BASENAME"
                    echo ""
                    
                    ((TODO_COUNT++))
                fi
            fi
        fi
    done
    
    if [ $TODO_COUNT -eq 0 ]; then
        print_success "No TODO API specifications found! All use cases are up to date."
    else
        print_warning "Found $TODO_COUNT TODO API specification(s)"
        echo ""
        print_info "The automated workflow will create PRs for these when pushed to main branch."
    fi
}

# Function to show status of all API specifications
show_status() {
    print_header "API Specifications - All Statuses"
    
    USE_CASES_DIR="$REPO_ROOT/docs/use-cases"
    
    if [ ! -d "$USE_CASES_DIR" ]; then
        print_error "Use cases directory not found: $USE_CASES_DIR"
        exit 1
    fi
    
    TOTAL=0
    TODO=0
    IN_PROGRESS=0
    IN_REVIEW=0
    DONE=0
    
    echo ""
    printf "%-40s %-15s %-12s\n" "Use Case" "Status" "Completed"
    printf "%-40s %-15s %-12s\n" "--------" "------" "---------"
    
    for dir in "$USE_CASES_DIR"/*/; do
        if [ -d "$dir" ]; then
            BASENAME=$(basename "$dir")
            
            # Look for the main use case markdown file
            file="${dir}use-case.md"
            if [ ! -f "$file" ]; then
              # Fallback: look for any .md file in the folder
              file=$(find "$dir" -maxdepth 1 -name "*.md" -type f | head -n 1)
            fi
            
            if [ -f "$file" ]; then
                # Extract status
                STATUS_LINE=$(grep "| \*\*API Specifications" "$file" || grep "| **API Specifications" "$file" || echo "")
                
                if [ -n "$STATUS_LINE" ]; then
                    ((TOTAL++))
                    
                    # Determine status
                    if echo "$STATUS_LINE" | grep -q "| TODO |"; then
                        STATUS="${YELLOW}TODO${NC}"
                        ((TODO++))
                    elif echo "$STATUS_LINE" | grep -q "| IN-PROGRESS |"; then
                        STATUS="${BLUE}IN-PROGRESS${NC}"
                        ((IN_PROGRESS++))
                    elif echo "$STATUS_LINE" | grep -q "| IN-REVIEW |"; then
                        STATUS="${BLUE}IN-REVIEW${NC}"
                        ((IN_REVIEW++))
                    elif echo "$STATUS_LINE" | grep -q "| DONE |"; then
                        STATUS="${GREEN}DONE${NC}"
                        ((DONE++))
                    else
                        STATUS="unknown"
                    fi
                    
                    # Extract completion date
                    COMPLETED=$(echo "$STATUS_LINE" | awk -F'|' '{print $4}' | xargs)
                    
                    printf "%-40s %-25s %-12s\n" "$BASENAME" "$STATUS" "$COMPLETED"
                fi
            fi
        fi
    done
    
    echo ""
    print_header "Summary"
    echo "Total Use Cases:    $TOTAL"
    echo -e "TODO:               ${YELLOW}$TODO${NC}"
    echo -e "In Progress:        ${BLUE}$IN_PROGRESS${NC}"
    echo -e "In Review:          ${BLUE}$IN_REVIEW${NC}"
    echo -e "Done:               ${GREEN}$DONE${NC}"
    
    if [ $TODO -gt 0 ]; then
        echo ""
        print_warning "$TODO use case(s) need API specifications"
    fi
}

# Function to check if workflow should trigger
check_workflow() {
    print_header "Workflow Trigger Check"
    
    USE_CASES_DIR="$REPO_ROOT/docs/use-cases"
    
    if [ ! -d "$USE_CASES_DIR" ]; then
        print_error "Use cases directory not found: $USE_CASES_DIR"
        exit 1
    fi
    
    TODO_COUNT=0
    echo ""
    
    for dir in "$USE_CASES_DIR"/*/; do
        if [ -d "$dir" ]; then
            # Look for the main use case markdown file
            file="${dir}use-case.md"
            if [ ! -f "$file" ]; then
              # Fallback: look for any .md file in the folder
              file=$(find "$dir" -maxdepth 1 -name "*.md" -type f | head -n 1)
            fi
            
            if [ -f "$file" ]; then
                if grep -q "| \*\*API Specifications (OpenAPI + Arazzo)\*\* | TODO |" "$file" || \
                   grep -q "| **API Specifications (OpenAPI + Arazzo)** | TODO |" "$file"; then
                    ((TODO_COUNT++))
                fi
            fi
        fi
    done
    
    if [ $TODO_COUNT -gt 0 ]; then
        print_success "Workflow WILL trigger"
        echo ""
        echo "Found $TODO_COUNT TODO API specification(s) that will trigger the workflow."
        echo ""
        print_info "When you push to main branch, the workflow will:"
        echo "  1. Create branch: icds/<use-case-name> for each TODO"
        echo "  2. Generate task files and placeholders in docs/use-cases/<use-case-name>/"
        echo "  3. Open pull requests with instructions for Copilot"
        echo ""
        print_info "Run './scripts/api-spec-helper.sh list' to see which use cases will trigger."
    else
        print_info "Workflow will NOT trigger"
        echo ""
        echo "No TODO API specifications found."
        echo "The workflow only triggers when use cases have API Specifications marked as 'TODO'."
    fi
}

# Function to show help
show_help() {
    cat << EOF
API Specification Helper Script

Usage:
  ./api-spec-helper.sh [command]

Commands:
  list        List all use cases with TODO API specifications
  status      Show status of all API specifications
  check       Check if automated workflow will trigger
  help        Show this help message

Examples:
  # List all TODO API specifications
  ./api-spec-helper.sh list

  # Show status of all use cases
  ./api-spec-helper.sh status

  # Check if workflow will trigger on next push
  ./api-spec-helper.sh check

Description:
  This script helps manage API specification tasks for use cases in the
  RateYourSchool project. It works in conjunction with the automated
  GitHub Actions workflow that creates PRs for TODO API specifications.

  The automated workflow runs when:
    - Code is pushed to main/master branch
    - Manually triggered via GitHub Actions UI

Related Files:
  - Workflow: .github/workflows/auto-api-specifications.yml
  - Documentation: .github/API_SPEC_AUTOMATION.md
  - Use Cases: docs/use-cases/

For more information, see:
  .github/API_SPEC_AUTOMATION.md

EOF
}

# Main script logic
main() {
    COMMAND="${1:-help}"
    
    case "$COMMAND" in
        list)
            list_todos
            ;;
        status)
            show_status
            ;;
        check)
            check_workflow
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            print_error "Unknown command: $COMMAND"
            echo ""
            show_help
            exit 1
            ;;
    esac
}

main "$@"
