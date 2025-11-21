#!/bin/bash

# Paper Management API - cURL Test Requests
# Make sure the API is running on https://localhost:5001 or http://localhost:5000

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5284}"
HAS_JQ=0

if command -v jq >/dev/null 2>&1; then
  HAS_JQ=1
fi

print_response() {
  local tmp_file status_code
  tmp_file=$(mktemp)

  status_code=$(curl -s -k -o "$tmp_file" -w "%{http_code}" "$@")

  if [[ -s "$tmp_file" ]]; then
    echo "Response:"
    if [[ "$HAS_JQ" -eq 1 ]]; then
      jq '.' "$tmp_file" 2>/dev/null || cat "$tmp_file"
    else
      cat "$tmp_file"
    fi
  else
    echo "Response: (no body returned)"
  fi

  echo "HTTP Status: $status_code"
  rm -f "$tmp_file"
}

run_step() {
  local title="$1"
  shift
  echo "-----------------------------------"
  echo "$title"
  print_response "$@"
  echo ""
}

echo "==================================="
echo "Paper Management API - Test Suite"
echo "==================================="
echo ""

# 1. GET ALL PAPERS
run_step "1. GET /papers - Retrieve all papers" \
  -X GET "$BASE_URL/papers" \
  -H "Accept: application/json"

# 2. GET PAPER BY ID (Success)
run_step "2. GET /papers/1 - Retrieve paper by ID" \
  -X GET "$BASE_URL/papers/1" \
  -H "Accept: application/json"

# 3. GET PAPER BY ID (Not Found - 404)
run_step "3. GET /papers/999 - Retrieve non-existent paper (should return 404)" \
  -X GET "$BASE_URL/papers/999" \
  -H "Accept: application/json"

# 4. GET TOP 3 MOST RECENT PAPERS
run_step "4. GET /papers/top3 - Retrieve top 3 most recent papers" \
  -X GET "$BASE_URL/papers/top3" \
  -H "Accept: application/json"

# 5. CREATE NEW PAPER (Success)
run_step "5. POST /papers - Create a new paper" \
  -X POST "$BASE_URL/papers" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "title": "Advanced Neural Network Architectures",
    "author": "Alice Johnson",
    "publishedOn": "2024-11-15"
  }'

# 6. CREATE NEW PAPER (Validation Error - Empty Title)
run_step "6. POST /papers - Create paper with validation error (empty title)" \
  -X POST "$BASE_URL/papers" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "title": "",
    "author": "Bob Smith",
    "publishedOn": "2024-11-01"
  }'

# 7. CREATE NEW PAPER (Validation Error - Future Date)
run_step "7. POST /papers - Create paper with validation error (future date)" \
  -X POST "$BASE_URL/papers" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "title": "Future Paper",
    "author": "Charlie Brown",
    "publishedOn": "2025-12-31"
  }'

# 8. CREATE NEW PAPER (Validation Error - Title Too Long)
LONG_TITLE=$(printf 'A%.0s' {1..201})
run_step "8. POST /papers - Create paper with validation error (title too long)" \
  -X POST "$BASE_URL/papers" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "{
    \"title\": \"$LONG_TITLE\",
    \"author\": \"David Lee\",
    \"publishedOn\": \"2024-10-01\"
  }"

# 9. CREATE VALID PAPER
run_step "9. POST /papers - Create another valid paper" \
  -X POST "$BASE_URL/papers" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "title": "Quantum Computing in Practice",
    "author": "Emily Davis",
    "publishedOn": "2024-10-20"
  }'

# 10. GET ALL PAPERS AGAIN (should include new papers)
run_step "10. GET /papers - Retrieve all papers (including newly created)" \
  -X GET "$BASE_URL/papers" \
  -H "Accept: application/json"

echo "==================================="
echo "Test Suite Completed!"
echo "==================================="

