#!/usr/bin/env bash

BASE_URL="http://localhost:5021"

echo "════════════════════════════════════════════════════════════════════════════════"
echo "             ADVANCED ORDER MANAGEMENT API - COMPREHENSIVE DEMO"
echo "════════════════════════════════════════════════════════════════════════════════"
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Random data arrays
AUTHORS=("Elena Rodriguez" "Marcus Chen" "Sophia Patel" "Liam O'Brien" "Yuki Tanaka" "Omar Hassan" "Isabella Costa" "Noah Schmidt" "Amara Okafor" "Lucas Dubois" "Priya Sharma" "Mateo Garcia" "Zara Ali" "Felix Mueller" "Kenji Watanabe")
CATEGORIES=("Fiction" "NonFiction" "Technical" "Children")

FICTION_PREFIXES=("Whispers of" "The Enchanted" "Shadows in" "Echoes from" "Chronicles of" "The Lost" "Secrets of" "Dreams Beyond" "Tales from" "The Hidden")
FICTION_SUFFIXES=("Midnight" "the Forgotten Realm" "Tomorrow" "the Crimson Moon" "Eternity" "the Azure Coast" "the Silver Mountains" "the Golden Age" "Infinity" "the Violet Sky")

NONFICTION_PREFIXES=("Mastering" "The Art of" "Understanding" "The Complete Guide to" "Principles of" "Modern Approaches to" "The Science Behind" "Revolutionary" "Essential" "Advanced Concepts in")
NONFICTION_SUFFIXES=("Leadership" "Innovation" "Strategic Thinking" "Human Psychology" "Digital Transformation" "Sustainable Living" "Cultural Evolution" "Economic Theory" "Social Dynamics" "Creative Problem Solving")

TECHNICAL_PREFIXES=("Advanced" "Modern" "Practical" "Scalable" "Distributed" "Cloud-Native" "Real-Time" "High-Performance" "Secure" "Intelligent")
TECHNICAL_SUFFIXES=("Software Architecture" "Data Engineering" "Microservices" "AI Systems" "DevOps Practices" "API Design" "Database Optimization" "Algorithms" "System Design" "Network Security")
TECHNICAL_KEYWORDS=("programming" "algorithms" "database" "software" "architecture" "design patterns" "cloud" "security" "optimization" "scalability")

CHILDREN_PREFIXES=("Little" "The Adventures of" "Magic" "Super" "Brave" "Happy" "Funny" "The Tale of" "Wonderful" "Amazing")
CHILDREN_SUFFIXES=("Dragon Friends" "Rainbow Journey" "Forest Adventures" "Ocean Explorers" "Sky Pirates" "Garden Mysteries" "Space Travelers" "Mountain Climbers" "River Heroes" "Desert Discoveries")

# Function to generate random ISBN-13
generate_isbn() {
  local prefix="978"
  local group=$(printf "%01d" $((RANDOM % 10)))
  local publisher=$(printf "%04d" $((RANDOM % 10000)))
  local title=$(printf "%04d" $((RANDOM % 10000)))
  
  local digits="${prefix}${group}${publisher}${title}"
  local sum=0
  for ((i=0; i<12; i++)); do
    local digit="${digits:$i:1}"
    if [ $((i % 2)) -eq 0 ]; then
      sum=$((sum + digit))
    else
      sum=$((sum + digit * 3))
    fi
  done
  local check=$((10 - (sum % 10)))
  [ $check -eq 10 ] && check=0
  
  echo "${prefix}-${group}-${publisher:0:4}-${title:0:4}-${check}"
}

# Function to generate random date within last 5 years
generate_random_date() {
  local year=$((2020 + RANDOM % 6))
  local month=$(printf "%02d" $((1 + RANDOM % 12)))
  local day=$(printf "%02d" $((1 + RANDOM % 28)))
  echo "${year}-${month}-${day}"
}

# Function to generate random keywords
generate_keywords() {
  local category=$1
  case $category in
    "Technical")
      local keywords=("${TECHNICAL_KEYWORDS[@]}")
      local k1=${keywords[$((RANDOM % ${#keywords[@]}))]}
      local k2=${keywords[$((RANDOM % ${#keywords[@]}))]}
      echo "[\"$k1\", \"$k2\"]"
      ;;
    "Fiction"|"NonFiction"|"Children")
      echo "[]"
      ;;
  esac
}

# Function to make API call and handle response
api_call() {
  local method=$1
  local endpoint=$2
  local data=$3
  local description=$4
  
  echo -e "${CYAN}➤ $description${NC}"
  
  if [ "$method" == "POST" ] || [ "$method" == "PUT" ]; then
    response=$(curl -s -X $method "${BASE_URL}${endpoint}" \
      -H "Content-Type: application/json" \
      -d "$data" \
      -w "\n%{http_code}")
  else
    response=$(curl -s -X $method "${BASE_URL}${endpoint}" -w "\n%{http_code}")
  fi
  
  http_code=$(echo "$response" | tail -n1)
  body=$(echo "$response" | sed '$d')
  
  if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
    echo -e "${GREEN}✓ Success (HTTP $http_code)${NC}"
    echo "$body" | python3 -m json.tool 2>/dev/null | head -20
  else
    echo -e "${RED}✗ Failed (HTTP $http_code)${NC}"
    echo "$body" | python3 -m json.tool 2>/dev/null
  fi
  echo ""
  
  # Store last created order ID for later use
  if [ "$method" == "POST" ] && [ "$endpoint" == "/orders" ] && [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
    LAST_ORDER_ID=$(echo "$body" | python3 -c "import json, sys; data=json.load(sys.stdin); print(data.get('id', ''))" 2>/dev/null)
  fi
  
  sleep 0.5
}

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 1: CREATE ORDERS WITH ADVANCED VALIDATION${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Create Technical orders (requires keywords, price >= $20, recent publication)
for i in {1..3}; do
  author=${AUTHORS[$((RANDOM % ${#AUTHORS[@]}))]}
  prefix=${TECHNICAL_PREFIXES[$((RANDOM % ${#TECHNICAL_PREFIXES[@]}))]}
  suffix=${TECHNICAL_SUFFIXES[$((RANDOM % ${#TECHNICAL_SUFFIXES[@]}))]}
  
  # Ensure title contains a technical keyword
  keyword=${TECHNICAL_KEYWORDS[$((RANDOM % ${#TECHNICAL_KEYWORDS[@]}))]}
  title="$prefix $keyword $suffix"
  
  isbn=$(generate_isbn)
  price=$((25 + RANDOM % 75))
  stock=$((10 + RANDOM % 50))
  date=$(generate_random_date)
  keywords=$(generate_keywords "Technical")
  
  api_call "POST" "/orders" "{
    \"title\": \"$title\",
    \"author\": \"$author\",
    \"isbn\": \"$isbn\",
    \"category\": \"Technical\",
    \"price\": $price.99,
    \"publishedDate\": \"$date\",
    \"stockQuantity\": $stock,
    \"keywords\": $keywords
  }" "Creating Technical order: $title"
done

# Create Fiction orders (requires author >= 5 chars)
for i in {1..3}; do
  author=${AUTHORS[$((RANDOM % ${#AUTHORS[@]}))]}
  prefix=${FICTION_PREFIXES[$((RANDOM % ${#FICTION_PREFIXES[@]}))]}
  suffix=${FICTION_SUFFIXES[$((RANDOM % ${#FICTION_SUFFIXES[@]}))]}
  title="$prefix $suffix"
  isbn=$(generate_isbn)
  price=$((15 + RANDOM % 35))
  stock=$((20 + RANDOM % 80))
  date=$(generate_random_date)
  
  api_call "POST" "/orders" "{
    \"title\": \"$title\",
    \"author\": \"$author\",
    \"isbn\": \"$isbn\",
    \"category\": \"Fiction\",
    \"price\": $price.49,
    \"publishedDate\": \"$date\",
    \"stockQuantity\": $stock,
    \"keywords\": []
  }" "Creating Fiction order: $title"
done

# Create Children orders (requires price <= $50, appropriate content)
for i in {1..2}; do
  author=${AUTHORS[$((RANDOM % ${#AUTHORS[@]}))]}
  prefix=${CHILDREN_PREFIXES[$((RANDOM % ${#CHILDREN_PREFIXES[@]}))]}
  suffix=${CHILDREN_SUFFIXES[$((RANDOM % ${#CHILDREN_SUFFIXES[@]}))]}
  title="$prefix $suffix"
  isbn=$(generate_isbn)
  price=$((8 + RANDOM % 30))
  stock=$((30 + RANDOM % 70))
  date=$(generate_random_date)
  
  api_call "POST" "/orders" "{
    \"title\": \"$title\",
    \"author\": \"$author\",
    \"isbn\": \"$isbn\",
    \"category\": \"Children\",
    \"price\": $price.95,
    \"publishedDate\": \"$date\",
    \"stockQuantity\": $stock,
    \"keywords\": []
  }" "Creating Children order: $title"
done

# Create NonFiction orders
for i in {1..2}; do
  author=${AUTHORS[$((RANDOM % ${#AUTHORS[@]}))]}
  prefix=${NONFICTION_PREFIXES[$((RANDOM % ${#NONFICTION_PREFIXES[@]}))]}
  suffix=${NONFICTION_SUFFIXES[$((RANDOM % ${#NONFICTION_SUFFIXES[@]}))]}
  title="$prefix $suffix"
  isbn=$(generate_isbn)
  price=$((18 + RANDOM % 42))
  stock=$((15 + RANDOM % 55))
  date=$(generate_random_date)
  
  api_call "POST" "/orders" "{
    \"title\": \"$title\",
    \"author\": \"$author\",
    \"isbn\": \"$isbn\",
    \"category\": \"NonFiction\",
    \"price\": $price.00,
    \"publishedDate\": \"$date\",
    \"stockQuantity\": $stock,
    \"keywords\": []
  }" "Creating NonFiction order: $title"
done

# Ensure LAST_ORDER_ID is set to the last created order ID (robust fallback)
if [ -z "$LAST_ORDER_ID" ]; then
  LAST_ORDER_ID=$(curl -s "${BASE_URL}/orders" | jq -r '.[-1].id' 2>/dev/null)
fi

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 2: DEMONSTRATE VALIDATION FAILURES${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Test: Technical book without keywords (should fail)
api_call "POST" "/orders" "{
  \"title\": \"Generic Tech Example\",
  \"author\": \"Test Author\",
  \"isbn\": \"$(generate_isbn)\",
  \"category\": \"Technical\",
  \"price\": 39.99,
  \"publishedDate\": \"2023-01-01\",
  \"stockQuantity\": 20,
  \"keywords\": []
}" "❌ Testing validation: Technical book without keywords (SHOULD FAIL)"

# Test: Technical book with low price (should fail)
api_call "POST" "/orders" "{
  \"title\": \"Cheap Programming Book\",
  \"author\": \"Test Author\",
  \"isbn\": \"$(generate_isbn)\",
  \"category\": \"Technical\",
  \"price\": 15.00,
  \"publishedDate\": \"2023-01-01\",
  \"stockQuantity\": 20,
  \"keywords\": [\"programming\", \"test\"]
}" "❌ Testing validation: Technical book under $20 (SHOULD FAIL)"

# Test: Children book over $50 (should fail)
api_call "POST" "/orders" "{
  \"title\": \"Expensive Children Book\",
  \"author\": \"Test Author\",
  \"isbn\": \"$(generate_isbn)\",
  \"category\": \"Children\",
  \"price\": 65.00,
  \"publishedDate\": \"2023-01-01\",
  \"stockQuantity\": 20,
  \"keywords\": []
}" "❌ Testing validation: Children book over $50 (SHOULD FAIL)"

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 3: RETRIEVAL OPERATIONS WITH AUTOMAPPER${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Get all orders
api_call "GET" "/orders" "" "Getting all orders (AutoMapper DTOs with formatted prices, initials, etc.)"

# Get orders with pagination
api_call "GET" "/orders/paginated?page=1&pageSize=3" "" "Getting paginated orders (Page 1, Size 3)"

# Get specific order by ID (using last created order)
if [ ! -z "$LAST_ORDER_ID" ]; then
  api_call "GET" "/orders/$LAST_ORDER_ID" "" "Getting specific order by ID: $LAST_ORDER_ID"
fi

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 4: CATEGORY-BASED OPERATIONS${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Get orders by category (with category-specific caching)
for category in "Technical" "Fiction" "Children"; do
  api_call "GET" "/orders/category/$category" "" "Getting $category orders (Category-specific cache)"
done

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 5: MULTI-LANGUAGE SUPPORT${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Test localization in different languages
for culture in "en-US" "fr-FR" "es-ES" "de-DE"; do
  echo -e "${CYAN}➤ Getting localized orders in $culture${NC}"
  response=$(curl -s "${BASE_URL}/orders/localized?culture=$culture")
  echo "$response" | python3 -c "import json, sys; data=json.load(sys.stdin); book=data[0] if data else {}; print(f\"  Culture: {book.get('culture', 'N/A')}\n  Category: {book.get('localizedCategoryName', 'N/A')}\n  Status: {book.get('availabilityStatus', 'N/A')}\")" 2>/dev/null
  echo ""
  sleep 0.3
done

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 6: ORDER METRICS DASHBOARD${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

echo -e "${CYAN}➤ Fetching comprehensive metrics dashboard${NC}"
metrics=$(curl -s "${BASE_URL}/orders/metrics")
echo "$metrics" | python3 -c "
import json, sys
try:
    data = json.load(sys.stdin)
    print('📊 ORDER CREATION METRICS:')
    print(f\"  Total Orders: {data['orderCreation']['totalOrders']}\")
    print(f\"  Today: {data['orderCreation']['ordersToday']}\")
    print(f\"  Total Revenue: {data['orderCreation']['formattedTotalRevenue']}\")
    print(f\"  Avg Order Value: {data['orderCreation']['formattedAverageOrderValue']}\")
    print()
    print('📦 INVENTORY METRICS:')
    print(f\"  Total Stock: {data['inventory']['totalStock']} units\")
    print(f\"  Low Stock Items: {data['inventory']['lowStockItems']}\")
    print(f\"  Inventory Value: {data['inventory']['formattedTotalInventoryValue']}\")
    print()
    print('📈 CATEGORY BREAKDOWN:')
    for cat in data['categoryBreakdown']['categories']:
        print(f\"  {cat['categoryName']}: {cat['orderCount']} orders ({cat['percentageOfTotal']}%)\")
    print()
    print('⚡ PERFORMANCE METRICS:')
    print(f\"  Validation Avg: {data['performance']['validationPerformance']['averageValidationTimeMs']}ms\")
    print(f\"  Database Avg: {data['performance']['databasePerformance']['averageSaveTimeMs']}ms\")
    print(f\"  Total Cache Keys: {data['performance']['cachePerformance']['totalCacheKeys']}\")
except Exception as e:
    print(f'Error parsing metrics: {e}')
" 2>/dev/null
echo ""

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 7: UPDATE OPERATIONS${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Update an order (with category-aware cache invalidation)
if [ ! -z "$LAST_ORDER_ID" ]; then
  # Generate an ISBN and strip hyphens to satisfy strict ISBN validator on update
  NEW_ISBN_FULL=$(generate_isbn)
  NEW_ISBN=${NEW_ISBN_FULL//-/}

  api_call "PUT" "/orders/$LAST_ORDER_ID" "{
    \"title\": \"Updated: Advanced Cloud Architecture Patterns\",
    \"author\": \"Updated Author\",
    \"isbn\": \"$NEW_ISBN\",
    \"category\": \"Technical\",
    \"price\": 89.99,
    \"publishedDate\": \"2024-01-01\",
    \"stockQuantity\": 25,
    \"keywords\": [\"cloud\", \"architecture\", \"patterns\"]
  }" "Updating order $LAST_ORDER_ID (invalidates old & new category caches)"
fi

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  PHASE 8: DELETE OPERATION${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

# Delete an order (with category-specific cache invalidation)
if [ ! -z "$LAST_ORDER_ID" ]; then
  api_call "DELETE" "/orders/$LAST_ORDER_ID" "" "Deleting order $LAST_ORDER_ID (category-specific cache invalidation)"
fi

echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${PURPLE}  DEMO COMPLETE - ADVANCED FEATURES DEMONSTRATED${NC}"
echo -e "${PURPLE}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${GREEN}✅ Features Demonstrated:${NC}"
echo "   • Advanced FluentValidation with business rules"
echo "   • Category-specific conditional validation"
echo "   • AutoMapper with custom resolvers (price formatting, initials, age, etc.)"
echo "   • Category-based caching with selective invalidation"
echo "   • Multi-language support (en-US, fr-FR, es-ES, de-DE)"
echo "   • Real-time metrics dashboard"
echo "   • Performance tracking (validation & database operations)"
echo "   • TraceId correlation throughout requests"
echo "   • Comprehensive logging with EventIds"
echo ""
echo -e "${CYAN}API Documentation: http://localhost:5021/swagger${NC}"
echo ""
