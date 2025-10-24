#!/usr/bin/env bash

BASE_URL="http://localhost:5021"

echo "=========================================="
echo "Advanced Order API - Comprehensive Testing"
echo "=========================================="
echo ""

# Sample order data with expanded lists
AUTHORS=("John Doe" "Jane Smith" "Robert Johnson" "Emily Davis" "Michael Brown" "Sarah Wilson" "David Lee" "Lisa Anderson" "James Taylor" "Maria Garcia" "Christopher Martinez" "Patricia Rodriguez" "Daniel White" "Jennifer Harris" "Matthew Clark" "Elizabeth Lewis" "Joseph Walker" "Linda Hall" "Ryan Allen" "Barbara Young")

CATEGORIES=("Fiction" "NonFiction" "Technical" "Children")

# Book title prefixes by category
FICTION_TITLES=("The Secret of" "A Tale of" "Journey to" "The Mystery of" "Chronicles of" "The Lost" "Shadows of" "The Hidden" "Echoes from" "The Last")
FICTION_SUFFIXES=("Dreams" "Midnight" "the Forest" "Tomorrow" "Destiny" "the Ocean" "Paradise" "the Mountains" "Eternity" "the Stars")

NONFICTION_TITLES=("Understanding" "The Complete Guide to" "Mastering" "Introduction to" "Advanced" "The Art of" "Principles of" "Modern" "Essential" "The Science of")
NONFICTION_SUFFIXES=("History" "Economics" "Psychology" "Philosophy" "Leadership" "Communication" "Business" "Politics" "Society" "Innovation")

TECHNICAL_TITLES=("Programming" "Data Structures and" "Software Engineering" "Advanced" "Machine Learning" "Web Development" "Database Design" "Cloud Computing" "Cybersecurity" "Artificial Intelligence")
TECHNICAL_SUFFIXES=("Principles" "Patterns" "Best Practices" "in Practice" "Fundamentals" "Architectures" "Algorithms" "Solutions" "Systems" "Techniques")

CHILDREN_TITLES=("The Adventures of" "Little" "The Magic" "Funny" "Brave" "The Tale of" "Super" "Happy" "The Story of" "Wonderful")
CHILDREN_SUFFIXES=("Friends" "Dragon" "Forest" "Puppy" "Princess" "Hero" "Animals" "Rainbow" "Adventures" "Journey")

# Function to generate random ISBN-13
generate_isbn() {
  local prefix="978"
  local group=$(printf "%01d" $((RANDOM % 10)))
  local publisher=$(printf "%03d" $((RANDOM % 1000)))
  local title=$(printf "%05d" $((RANDOM % 100000)))
  
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
  if [ $check -eq 10 ]; then
    check=0
  fi
  
  echo "${prefix}${group}${publisher}${title}${check}"
}

# Function to generate random price based on category
generate_price() {
  local category=$1
  case $category in
    "Technical")
      echo "$((20 + RANDOM % 80)).99"
      ;;
    "Children")
      echo "$((5 + RANDOM % 25)).99"
      ;;
    *)
      echo "$((10 + RANDOM % 40)).99"
      ;;
  esac
}

# Function to generate published date
generate_date() {
  local days_ago=$((RANDOM % 1825))
  date -u -v-${days_ago}d +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "${days_ago} days ago" +"%Y-%m-%dT%H:%M:%SZ"
}

# Function to generate random title based on category
generate_title() {
  local category=$1
  case $category in
    "Fiction")
      local prefix_idx=$((RANDOM % 10))
      local suffix_idx=$((RANDOM % 10))
      echo "${FICTION_TITLES[$prefix_idx]} ${FICTION_SUFFIXES[$suffix_idx]}"
      ;;
    "NonFiction")
      local prefix_idx=$((RANDOM % 10))
      local suffix_idx=$((RANDOM % 10))
      echo "${NONFICTION_TITLES[$prefix_idx]} ${NONFICTION_SUFFIXES[$suffix_idx]}"
      ;;
    "Technical")
      local prefix_idx=$((RANDOM % 10))
      local suffix_idx=$((RANDOM % 10))
      echo "${TECHNICAL_TITLES[$prefix_idx]} ${TECHNICAL_SUFFIXES[$suffix_idx]}"
      ;;
    "Children")
      local prefix_idx=$((RANDOM % 10))
      local suffix_idx=$((RANDOM % 10))
      echo "${CHILDREN_TITLES[$prefix_idx]} ${CHILDREN_SUFFIXES[$suffix_idx]}"
      ;;
  esac
}

echo "Phase 1: Creating sample orders (50 orders across all categories)"
echo "=================================================================="
echo ""

CREATED_IDS=()

echo "Creating 50 orders..."
echo ""

for i in $(seq 1 50)
do
  # Randomly select author and category
  AUTHOR_INDEX=$((RANDOM % 20))
  CATEGORY_INDEX=$((RANDOM % 4))
  
  AUTHOR="${AUTHORS[$AUTHOR_INDEX]}"
  CATEGORY="${CATEGORIES[$CATEGORY_INDEX]}"
  ISBN=$(generate_isbn)
  PRICE=$(generate_price $CATEGORY)
  PUB_DATE=$(generate_date)
  STOCK=$((5 + RANDOM % 50))
  
  # Generate random title based on category
  TITLE=$(generate_title "$CATEGORY")
  
  # Add cover image URL for non-children's orders
  COVER_URL=""
  if [ "$CATEGORY" != "Children" ]; then
    COVER_URL="\"coverImageUrl\":\"https://covers.example.com/order-$i.jpg\","
  fi
  
  RESPONSE=$(curl -X POST "$BASE_URL/orders" \
    -H "Content-Type: application/json" \
    -d "{\"title\":\"$TITLE\",\"author\":\"$AUTHOR\",\"isbn\":\"$ISBN\",\"category\":\"$CATEGORY\",\"price\":$PRICE,\"publishedDate\":\"$PUB_DATE\",$COVER_URL\"stockQuantity\":$STOCK}" \
    -s -w "\n%{http_code}")
  
  HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
  BODY=$(echo "$RESPONSE" | sed '$d')
  
  if [ "$HTTP_CODE" -eq 201 ]; then
    ORDER_ID=$(echo "$BODY" | jq -r '.id')
    CREATED_IDS+=("$ORDER_ID")
    
    if [ $((i % 10)) -eq 0 ]; then
      echo "✓ Created $i orders... (Latest: $CATEGORY)"
    fi
  else
    echo "✗ Failed to create order $i (HTTP $HTTP_CODE)"
  fi
done

echo ""
echo "✓ Successfully created ${#CREATED_IDS[@]} orders!"
echo ""

echo "Phase 2: Testing AutoMapper Computed Fields"
echo "============================================"
echo ""

if [ ${#CREATED_IDS[@]} -gt 0 ]; then
  SAMPLE_ID="${CREATED_IDS[0]}"
  echo "1. Testing computed fields on order: $SAMPLE_ID"
  curl -X GET "$BASE_URL/orders/$SAMPLE_ID" -H "Accept: application/json" -s | jq '{
    id: .id,
    title: .title,
    author: .author,
    authorInitials: .authorInitials,
    categoryDisplayName: .categoryDisplayName,
    formattedPrice: .formattedPrice,
    publishedAge: .publishedAge,
    availabilityStatus: .availabilityStatus,
    isAvailable: .isAvailable,
    stockQuantity: .stockQuantity
  }'
  echo ""
fi

echo "2. Testing Children's order discount (10% off):"
echo "   Creating a Children's order to verify price discount..."
CHILDREN_ISBN=$(generate_isbn)
CHILDREN_RESPONSE=$(curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Fun Kids Story\",\"author\":\"Children Author\",\"isbn\":\"$CHILDREN_ISBN\",\"category\":\"Children\",\"price\":19.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":15}" \
  -s)

CHILDREN_ID=$(echo "$CHILDREN_RESPONSE" | jq -r '.id')
if [ "$CHILDREN_ID" != "null" ]; then
  echo "   ✓ Children's order created: $CHILDREN_ID"
  curl -X GET "$BASE_URL/orders/$CHILDREN_ID" -H "Accept: application/json" -s | jq '{
    title: .title,
    category: .categoryDisplayName,
    originalPrice: "19.99",
    discountedPrice: .price,
    formattedPrice: .formattedPrice,
    coverImageUrl: .coverImageUrl,
    note: "Should be 10% off (17.99) and coverImageUrl should be null"
  }'
  echo ""
fi

echo ""
echo "Phase 3: Testing Validation Rules"
echo "=================================="
echo ""

echo "1. Testing duplicate ISBN validation:"
DUPLICATE_ISBN=$(generate_isbn)
curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"First Order\",\"author\":\"Test Author\",\"isbn\":\"$DUPLICATE_ISBN\",\"category\":\"Fiction\",\"price\":15.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":10}" \
  -s -o /dev/null

echo "   Attempting to create duplicate..."
curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Duplicate Order\",\"author\":\"Test Author\",\"isbn\":\"$DUPLICATE_ISBN\",\"category\":\"Fiction\",\"price\":15.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":10}" \
  -s | jq '{status: .status, title: .title, message: .message}'
echo ""

echo "2. Testing invalid ISBN format:"
curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Invalid ISBN Order\",\"author\":\"Test Author\",\"isbn\":\"123-invalid\",\"category\":\"Fiction\",\"price\":15.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":10}" \
  -s | jq '{status: .status, title: .title, errors: .errors}'
echo ""

echo "3. Testing negative price:"
curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Negative Price Order\",\"author\":\"Test Author\",\"isbn\":\"$(generate_isbn)\",\"category\":\"Fiction\",\"price\":-10.00,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":10}" \
  -s | jq '{status: .status, title: .title, errors: .errors}'
echo ""

echo "4. Testing invalid category:"
curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Invalid Category Order\",\"author\":\"Test Author\",\"isbn\":\"$(generate_isbn)\",\"category\":\"InvalidCategory\",\"price\":15.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":10}" \
  -s | jq '{status: .status, title: .title, errors: .errors}'
echo ""

echo ""
echo "Phase 4: Testing Pagination"
echo "============================"
echo ""

echo "1. GET /orders/paginated?page=1&pageSize=10 (first page):"
curl -X GET "$BASE_URL/orders/paginated?page=1&pageSize=10" -H "Accept: application/json" -s | jq '{
  page: .page,
  pageSize: .pageSize,
  totalCount: .totalCount,
  totalPages: .totalPages,
  itemsCount: (.items | length),
  firstOrder: .items[0].title
}'
echo ""

echo "2. GET /orders/paginated?page=2&pageSize=5:"
curl -X GET "$BASE_URL/orders/paginated?page=2&pageSize=5" -H "Accept: application/json" -s | jq '{
  page: .page,
  pageSize: .pageSize,
  totalCount: .totalCount,
  totalPages: .totalPages,
  itemsCount: (.items | length)
}'
echo ""

echo ""
echo "Phase 5: Testing Update & Delete Operations"
echo "============================================"
echo ""

if [ ${#CREATED_IDS[@]} -gt 5 ]; then
  UPDATE_ID="${CREATED_IDS[5]}"
  
  echo "1. Testing UPDATE order: $UPDATE_ID"
  UPDATE_RESPONSE=$(curl -X PUT "$BASE_URL/orders/$UPDATE_ID" \
    -H "Content-Type: application/json" \
    -d "{\"title\":\"Updated Title\",\"author\":\"Updated Author\",\"isbn\":\"$(generate_isbn)\",\"category\":\"NonFiction\",\"price\":29.99,\"publishedDate\":\"$(generate_date)\",\"coverImageUrl\":\"https://updated.com/cover.jpg\",\"stockQuantity\":25}" \
    -s)
  
  echo "$UPDATE_RESPONSE" | jq '{
    id: .id,
    title: .title,
    author: .author,
    authorInitials: .authorInitials,
    formattedPrice: .formattedPrice,
    categoryDisplayName: .categoryDisplayName
  }'
  echo ""
  
  DELETE_ID="${CREATED_IDS[6]}"
  echo "2. Testing DELETE order: $DELETE_ID"
  curl -X DELETE "$BASE_URL/orders/$DELETE_ID" \
    -s -w "\nHTTP Status: %{http_code}\n"
  echo ""
  
  echo "3. Verifying deleted order returns 404:"
  curl -X GET "$BASE_URL/orders/$DELETE_ID" \
    -H "Accept: application/json" \
    -s -w "\nHTTP Status: %{http_code}\n" | head -1
  echo ""
fi

echo ""
echo "Phase 6: Testing Category-Specific Features"
echo "============================================"
echo ""

echo "Creating orders in each category to test conditional mapping:"
echo ""

echo "1. Fiction order:"
FICTION_ISBN=$(generate_isbn)
FICTION_ORDER=$(curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Amazing Fiction Story\",\"author\":\"Fiction Writer\",\"isbn\":\"$FICTION_ISBN\",\"category\":\"Fiction\",\"price\":24.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":30,\"coverImageUrl\":\"https://example.com/fiction.jpg\"}" \
  -s)
FICTION_ID=$(echo "$FICTION_ORDER" | jq -r '.id')
curl -X GET "$BASE_URL/orders/$FICTION_ID" -H "Accept: application/json" -s | jq '{
  title: .title,
  category: .categoryDisplayName,
  price: .price,
  formattedPrice: .formattedPrice,
  coverImageUrl: .coverImageUrl
}'
echo ""

echo "2. Technical order:"
TECH_ISBN=$(generate_isbn)
TECH_ORDER=$(curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Advanced Programming Guide\",\"author\":\"Tech Expert\",\"isbn\":\"$TECH_ISBN\",\"category\":\"Technical\",\"price\":59.99,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":20,\"coverImageUrl\":\"https://example.com/tech.jpg\"}" \
  -s)
TECH_ID=$(echo "$TECH_ORDER" | jq -r '.id')
curl -X GET "$BASE_URL/orders/$TECH_ID" -H "Accept: application/json" -s | jq '{
  title: .title,
  category: .categoryDisplayName,
  price: .price,
  formattedPrice: .formattedPrice,
  coverImageUrl: .coverImageUrl
}'
echo ""

echo "3. Children order (with discount and no cover):"
CHILD_ISBN=$(generate_isbn)
CHILD_ORDER=$(curl -X POST "$BASE_URL/orders" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Kids Adventure Book\",\"author\":\"Child Author\",\"isbn\":\"$CHILD_ISBN\",\"category\":\"Children\",\"price\":20.00,\"publishedDate\":\"$(generate_date)\",\"stockQuantity\":50,\"coverImageUrl\":\"https://example.com/kids.jpg\"}" \
  -s)
CHILD_ID=$(echo "$CHILD_ORDER" | jq -r '.id')
curl -X GET "$BASE_URL/orders/$CHILD_ID" -H "Accept: application/json" -s | jq '{
  title: .title,
  category: .categoryDisplayName,
  originalInput: "20.00",
  actualPrice: .price,
  formattedPrice: .formattedPrice,
  coverImageUrl: .coverImageUrl,
  note: "Children: 10% discount applied (18.00), cover image null"
}'
echo ""
