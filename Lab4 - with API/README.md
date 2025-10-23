##  Table of Contents

- [Core Features](#core-features)
- [Bonus Features](#bonus-features)
- [API Endpoints](#api-endpoints)
- [Multi-Language Support](#multi-language-support)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Performance](#performance)
- [API Examples](#api-examples)

---

##  Core Features

### 1. Book Entity

Enhanced book model with comprehensive properties:

```csharp
public class Book
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
    public BookCategory Category { get; set; }
    public decimal Price { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<BookLocalization> Localizations { get; set; }
}
```

**Categories:**
- Fiction (0)
- NonFiction (1)
- Technical (2)
- Children (3)

### 2. AutoMapper Configuration

**7 Custom Value Resolvers:**

1. **CategoryDisplayResolver** - Converts enum to friendly names
   - `Technical` → "Technical & Programming"
   - `Children` → "Children's Books"

2. **PriceFormatterResolver** - Formats prices as currency
   - `45.99` → "$45.99"

3. **PublishedAgeResolver** - Calculates book age
   - "1 years old", "6 months old"

4. **AuthorInitialsResolver** - Extracts author initials
   - "John Smith" → "JS"

5. **AvailabilityStatusResolver** - Determines stock status
   - `>0` → "In Stock"
   - `0` → "Out of Stock"
   - `≤10` → "Low Stock"

6. **ConditionalCoverImageResolver** - Removes images for children's books <$15
   - Protects low-value items

7. **ConditionalPriceResolver** - Applies 10% discount to children's books
   - Automatic pricing adjustment

### 3. Comprehensive Validation

**CreateBookProfileValidator** includes:

#### Basic Validation Rules:
- **Title**: Required, 1-200 characters, no special characters
- **Author**: Required, 2-100 characters, letters/spaces/hyphens only
- **ISBN**: Valid ISBN-10 or ISBN-13 format
- **Price**: $5-$1000 range
- **Stock**: 0-1000 units
- **Published Date**: Not in the future
- **Cover Image**: Valid URL format

#### Advanced Business Rules:
- **Daily Creation Limit**: Max 500 books per day
- **Technical Books**: 
  - Minimum price $20
  - Title must contain keywords: "Programming", "Development", "Software", "Code", "Guide", "Tutorial"
- **Children's Books**:
  - Maximum price $25
  - Maximum 20 pages (if pages field exists)
- **High-Value Books** (>$500):
  - Maximum stock quantity of 10
  - Prevents over-investment

#### Async Database Validation:
- **Unique Title per Author**: Checks database for duplicates
- **Unique ISBN**: Prevents duplicate book entries
- Concurrent validation support with scoped DbContext

### 4. Structured Logging

**LogEvents (2001-2008):**
```csharp
BookCreationRequested = 2001,
BookValidationFailed = 2002,
BookCreatedSuccessfully = 2003,
BookCreationFailed = 2004,
DailyBookLimitCheck = 2005,
DuplicateBookDetected = 2006,
HighValueBookCreated = 2007,
BookCreationMetrics = 2008
```

**Features:**
- Scoped logging with correlation IDs
- Performance tracking with Stopwatch
- Detailed metrics (validation time, DB time, total time)
- Business rule violation logging

### 5. Global Exception Handling

**GlobalExceptionMiddleware** provides:
- Correlation ID generation
- TraceId tracking
- Structured error responses
- Different handling for different exception types:
  - `ValidationException` → 400 Bad Request
  - `InvalidOperationException` → 400 Bad Request
  - `KeyNotFoundException` → 404 Not Found
  - General exceptions → 500 Internal Server Error

**Error Response Format:**
```json
{
  "message": "Error description",
  "errorCode": "ERROR_TYPE",
  "traceId": "request-trace-id",
  "details": "Additional information"
}
```

---

##  Bonus Features

### Bonus 1: Category-Based Caching 

**BookCacheService** implements smart caching:

#### Features:
- **Category-Specific Keys**: `books_{category}_page_{page}_size_{size}`
- **Generic Keys**: `books_all_page_{page}_size_{size}`
- **Targeted Invalidation**: Only affected categories are cleared
- **Cache Statistics**: Tracks hits, misses, hit rate

#### Performance:
- **Cache Hit Rate**: 66.67% in testing
- **Query Reduction**: 2/3 fewer database queries
- **Response Time**: ~40% faster for cached requests

#### Endpoints:
- `GET /books/cache/stats` - View cache performance

#### Example Response:
```json
{
  "cacheHits": 12,
  "cacheMisses": 6,
  "hitRatePercentage": 66.67,
  "totalInvalidations": 3,
  "activeCacheKeys": 8
}
```

### Bonus 2: Metrics Dashboard 

**GetBookMetricsHandler** provides comprehensive analytics:

#### Metrics Categories:

1. **Overview Metrics**:
   - Total books
   - Available books (stock > 0)
   - Out of stock books
   - Total inventory value
   - Average book price
   - Median book price

2. **Time-Based Metrics**:
   - Books created today
   - Books created this week
   - Books created this month
   - Oldest book
   - Newest book
   - Average book age

3. **Stock Metrics**:
   - Low stock books (<10 units)
   - High-value books (>$500)
   - Top 5 books by stock quantity

4. **Category Breakdown**:
   - Books per category
   - Average price per category
   - Total inventory value per category

5. **Performance Snapshot**:
   - Real-time system metrics
   - Books added in last 24 hours
   - Books added in last 7 days
   - Category distribution
   - Inventory turnover rate

#### Endpoints:
- `GET /books/metrics` - Full dashboard
- `GET /books/metrics/performance` - Quick snapshot

#### Example Metrics:
```json
{
  "totalBooks": 144,
  "totalCategories": 4,
  "averageBookPrice": 674.74,
  "totalInventoryValue": 97168.99,
  "booksByCategory": {
    "Technical": {
      "totalBooks": 45,
      "averagePrice": 52.99,
      "totalValue": 23884.55
    }
  }
}
```

### Bonus 3: Batch Operations 

**BatchCreateBooksHandler** enables bulk book creation:

#### Features:

1. **Parallel Validation**:
   - Sequential for <10 books
   - Parallel for ≥10 books
   - Uses `Parallel.ForEachAsync`
   - Configurable parallelism (CPU cores)
   - Thread-safe with scoped DbContext

2. **Transaction Management**:
   - `BeginTransactionAsync` for atomic operations
   - Rollback on any failure
   - Duplicate ISBN detection within transaction
   - Batch insert with `AddRangeAsync`

3. **Performance Optimization**:
   - Parallel DTO mapping for large batches
   - Single database round-trip for validation
   - Efficient batch insert
   - Category-specific cache invalidation

4. **Error Tracking**:
   - Per-book validation errors
   - Index tracking for failed books
   - Title and ISBN in error messages
   - Detailed validation error lists

#### Configuration:
```csharp
MaxBatchSize = 100 books
ParallelValidationThreshold = 10 books
MaxDegreeOfParallelism = Environment.ProcessorCount
```

#### Endpoint:
- `POST /books/batch`

#### Request Format:
```json
{
  "books": [
    {
      "title": "Book Title",
      "author": "Author Name",
      "isbn": "978-X-XXXXX-XXX-X",
      "publishedDate": "2024-01-01",
      "category": "Technical",
      "price": 45.99,
      "stockQuantity": 50,
      "coverImageUrl": "https://example.com/book.jpg"
    }
  ]
}
```

#### Response Format:
```json
{
  "totalRequested": 12,
  "successfullyCreated": 11,
  "failed": 1,
  "createdBooks": [ /* Array of BookProfileDto */ ],
  "errors": [
    {
      "index": 5,
      "title": "Book Title",
      "isbn": "978-X-XXXXX-XXX-X",
      "validationErrors": [
        "Technical book title must contain technical keywords."
      ]
    }
  ],
  "processingTime": "00:00:00.3369285",
  "operationId": "3beaa436"
}
```

#### Performance:
- 12 books: ~336ms
- 15 books: ~400-500ms
- Parallel speedup: 40-60% for large batches

### Bonus 4: Multi-Language Support 

**Comprehensive internationalization system:**

#### Features:

1. **Book Localization**:
   - Store translated titles and descriptions
   - Support for multiple cultures per book
   - Database-backed translations
   - `BookLocalization` entity with foreign key

2. **XML Resource Files**:
   - Category names and descriptions
   - UI labels
   - Availability status messages
   - Loaded at startup and cached

3. **Supported Languages**:
   - English (en-US) - Default
   - Spanish (es)
   - French (fr)
   - German (de)
   - Japanese (ja)

4. **Fallback Mechanism**:
   - Try exact culture match (e.g., "es-MX")
   - Try language match (e.g., "es")
   - Fallback to English (en-US)
   - Never return null/error

#### XML Resource Structure:
```xml
<?xml version="1.0" encoding="utf-8"?>
<resources culture="es">
  <categories>
    <category id="Technical">
      <name>Técnico y Programación</name>
      <description>Libros de programación, manuales técnicos...</description>
    </category>
  </categories>
  <ui>
    <availabilityStatus>
      <inStock>En Stock</inStock>
      <lowStock>Stock Bajo</lowStock>
      <outOfStock>Agotado</outOfStock>
    </availabilityStatus>
    <labels>
      <price>Precio</price>
      <author>Autor</author>
    </labels>
  </ui>
</resources>
```

#### Services:

1. **BookMetadataService** (XML-based):
   - Loads and caches all resource files
   - Provides category translations
   - Provides UI label translations
   - Provides availability status translations

2. **BookLocalizationService** (Database-backed):
   - Manages book-specific translations
   - CRUD operations for localizations
   - Culture normalization
   - Fallback logic

#### Endpoints:

1. **Get Localized Book**:
   ```
   GET /books/{id}/localized?culture={culture}
   ```
   Returns book with all text in specified language

2. **Create/Update Localization**:
   ```
   POST /books/{id}/localizations
   {
     "cultureCode": "es",
     "localizedTitle": "Título en Español",
     "localizedDescription": "Descripción en español"
   }
   ```

3. **Get All Localizations**:
   ```
   GET /books/{id}/localizations
   ```
   Returns all available translations for a book

4. **Delete Localization**:
   ```
   DELETE /books/{id}/localizations/{culture}
   ```

5. **Get Category Metadata**:
   ```
   GET /metadata/categories?culture={culture}
   ```
   Returns all category names in specified language

#### Example Localized Response:
```json
{
  "id": "guid",
  "title": "El Arte de la Programación",
  "description": "Una guía completa...",
  "category": "Técnico y Programación",
  "categoryDescription": "Libros de programación...",
  "availabilityStatus": "En Stock",
  "culture": "es",
  "availableCultures": ["en-US", "es", "fr", "de", "ja"]
}
```

#### Database Schema:
```sql
CREATE TABLE BookLocalizations (
    Id TEXT PRIMARY KEY,
    BookId TEXT NOT NULL,
    CultureCode TEXT NOT NULL,
    LocalizedTitle TEXT NOT NULL,
    LocalizedDescription TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE CASCADE,
    UNIQUE(BookId, CultureCode)
);
```

---

## API Endpoints

### Books

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/books` | Get paginated books with optional category filter |
| GET | `/books/{id}` | Get book by ID |
| GET | `/books/{id}/localized?culture={culture}` | Get localized book |
| POST | `/books` | Create new book |
| POST | `/books/batch` | Batch create books |
| PUT | `/books/{id}` | Update book |
| DELETE | `/books/{id}` | Delete book |

### Localization

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/books/{id}/localizations` | Get all localizations for a book |
| POST | `/books/{id}/localizations` | Create/update book localization |
| DELETE | `/books/{id}/localizations/{culture}` | Delete book localization |
| GET | `/metadata/categories?culture={culture}` | Get localized category metadata |

### Metrics

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/books/metrics` | Get comprehensive metrics dashboard |
| GET | `/books/metrics/performance` | Get performance snapshot |

### Cache

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/books/cache/stats` | Get cache statistics |

---

## Multi-Language Support

### How It Works

#### 1. Category Metadata (XML Resources)

Category names, descriptions, and UI elements are stored in XML files:

**Location**: `/Resources/BookMetadata.{culture}.xml`

**Files**:
- `BookMetadata.en-US.xml` (English - Default)
- `BookMetadata.es.xml` (Spanish)
- `BookMetadata.fr.xml` (French)
- `BookMetadata.de.xml` (German)
- `BookMetadata.ja.xml` (Japanese)

**Loading**:
- Files loaded at application startup
- Cached in memory for fast access
- No database queries needed

**Usage**:
```csharp
var categoryName = _metadataService.GetCategoryName(
    BookCategory.Technical, 
    "es"
); // Returns "Técnico y Programación"
```

#### 2. Book Translations (Database)

Book-specific titles and descriptions are stored in database:

**Entity**: `BookLocalization`
- Linked to book via foreign key
- Unique constraint on (BookId, CultureCode)
- Cascade delete with parent book

**Creation**:
```bash
POST /books/{id}/localizations
{
  "cultureCode": "fr",
  "localizedTitle": "Titre en Français",
  "localizedDescription": "Description en français"
}
```

**Retrieval**:
```bash
GET /books/{id}/localized?culture=fr
```

#### 3. Fallback Mechanism

**Priority Order**:
1. Exact culture match (e.g., "es-MX")
2. Language-only match (e.g., "es")
3. Default culture (en-US)

**Examples**:
- Request for "es-MX" → Try "es-MX" → Try "es" → Use "en-US"
- Request for "fr-CA" → Try "fr-CA" → Try "fr" → Use "en-US"
- Request for "zh-CN" → No match → Use "en-US"

#### 4. Culture Normalization

Handles common variations:
```csharp
"es-ES" → "es"
"es-MX" → "es"
"fr-FR" → "fr"
"fr-CA" → "fr"
"de-DE" → "de"
"ja-JP" → "ja"
```

### Example Usage

#### Creating a Multilingual Book

1. **Create the book** (English by default):
```bash
POST /books
{
  "title": "The Art of Programming",
  "author": "John Smith",
  "isbn": "978-5-55555-001-7",
  "category": "Technical",
  "price": 59.99,
  "stockQuantity": 75
}
```

2. **Add Spanish translation**:
```bash
POST /books/{id}/localizations
{
  "cultureCode": "es",
  "localizedTitle": "El Arte de la Programación",
  "localizedDescription": "Una guía completa para dominar la programación"
}
```

3. **Add French translation**:
```bash
POST /books/{id}/localizations
{
  "cultureCode": "fr",
  "localizedTitle": "L'Art de la Programmation",
  "localizedDescription": "Un guide complet pour maîtriser la programmation"
}
```

4. **Retrieve in Spanish**:
```bash
GET /books/{id}/localized?culture=es
```

**Response**:
```json
{
  "title": "El Arte de la Programación",
  "description": "Una guía completa para dominar la programación",
  "category": "Técnico y Programación",
  "categoryDescription": "Libros de programación, manuales técnicos...",
  "availabilityStatus": "En Stock",
  "culture": "es",
  "availableCultures": ["en-US", "es", "fr"]
}
```

### Benefits

1. **User Experience**: Users see content in their language
2. **SEO**: Better search engine rankings for international markets
3. **Accessibility**: Broader audience reach
4. **Maintainability**: Easy to add new languages
5. **Performance**: XML resources cached in memory
6. **Flexibility**: Mix of static (XML) and dynamic (DB) translations

---

## Testing

### Automated Test Script

Run the comprehensive test script:

```bash
chmod +x populate-and-test.sh
./populate-and-test.sh
```

### Test Coverage

The script tests all 9 phases:

1. **Phase 1**: Seeding (104 books across all categories)
2. **Phase 2**: Retrieval (GET by ID)
3. **Phase 3**: Validation Rules
4. **Phase 4**: Category-Based Caching
5. **Phase 5**: AutoMapper Custom Resolvers
6. **Phase 6**: Update & Delete Operations
7. **Phase 7**: Metrics Dashboard
8. **Phase 8**: Batch Operations
9. **Phase 9**: Multi-Language Support

### Unit Tests

Run unit tests:
```bash
dotnet test
```

**Test Files**:
- `GetBooksWithPaginationHandlerTests.cs`
- `CreateBookHandlerTests.cs`
- `UpdateBookHandlerTests.cs`

**Coverage**: 3 integration tests covering core CRUD operations

### Manual Testing

#### Test Category Localization:
```bash
# English
curl "http://localhost:5021/metadata/categories?culture=en-US"

# Spanish
curl "http://localhost:5021/metadata/categories?culture=es"

# French
curl "http://localhost:5021/metadata/categories?culture=fr"
```

#### Test Book Localization:
```bash
# Create book
BOOK_ID=$(curl -X POST "http://localhost:5021/books" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Book","author":"Test Author",...}' \
  | jq -r '.id')

# Add Spanish translation
curl -X POST "http://localhost:5021/books/$BOOK_ID/localizations" \
  -H "Content-Type: application/json" \
  -d '{"cultureCode":"es","localizedTitle":"Libro de Prueba"}'

# Get in Spanish
curl "http://localhost:5021/books/$BOOK_ID/localized?culture=es"
```

#### Test Batch Operations:
```bash
curl -X POST "http://localhost:5021/books/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "books": [
      {"title":"Book 1 Programming",...},
      {"title":"Book 2",...}
    ]
  }'
```

#### Test Cache Statistics:
```bash
# Initial stats
curl "http://localhost:5021/books/cache/stats"

# Query with caching
curl "http://localhost:5021/books?category=2&page=1&pageSize=10"
curl "http://localhost:5021/books?category=2&page=1&pageSize=10"

# Check improved stats
curl "http://localhost:5021/books/cache/stats"
```

---

## 🏗 Project Structure

```
Lab4 - with API/
├── Model/
│   ├── Book.cs
│   ├── BookCategory.cs
│   ├── BookLocalization.cs
│   └── SupportedCulture.cs
├── DTO/
│   ├── BookProfileDto.cs
│   ├── BookMetricsDto.cs
│   ├── LocalizationDtos.cs
│   └── Request/
│       ├── CreateBookRequest.cs
│       ├── BatchCreateBooksRequest.cs
│       └── CreateBookLocalizationRequest.cs
├── Handlers/
│   ├── CreateBookHandler.cs
│   ├── UpdateBookHandler.cs
│   ├── DeleteBookHandler.cs
│   ├── GetBookByIdHandler.cs
│   ├── GetBooksWithPaginationHandler.cs
│   ├── GetBookMetricsHandler.cs
│   └── BatchCreateBooksHandler.cs
├── Validators/
│   ├── CreateBookProfileValidator.cs
│   ├── UpdateBookValidator.cs
│   └── GetBooksWithPaginationValidator.cs
├── Services/
│   ├── BookCacheService.cs
│   ├── BookMetadataService.cs
│   └── BookLocalizationService.cs
├── Persistence/
│   └── BookContext.cs
├── Middleware/
│   └── GlobalExceptionMiddleware.cs
├── Resources/
│   ├── BookMetadata.en-US.xml
│   ├── BookMetadata.es.xml
│   ├── BookMetadata.fr.xml
│   ├── BookMetadata.de.xml
│   └── BookMetadata.ja.xml
├── Mapping/
│   └── AdvancedBookMappingProfile.cs
├── Program.cs
└── books.db
```

---

## ⚡ Performance

### Optimization Techniques

1. **Caching**:
   - In-memory cache for frequently accessed data
   - Category-specific cache keys
   - 66.67% hit rate achieved

2. **Parallel Processing**:
   - Batch validation in parallel
   - DTO mapping in parallel
   - Configurable degree of parallelism

3. **Database Optimization**:
   - Batch inserts with AddRangeAsync
   - AsNoTracking for read-only queries
   - Indexes on BookId and CultureCode

4. **Resource Management**:
   - XML resources loaded once at startup
   - Cached in memory
   - No repeated file I/O

### Performance Metrics

| Operation | Time | Notes |
|-----------|------|-------|
| Single book creation | ~50ms | Including validation |
| Batch 12 books | ~336ms | Parallel validation |
| Batch 15 books | ~400-500ms | Optimal for CPU cores |
| Cache hit query | ~10ms | Memory access only |
| Cache miss query | ~40ms | Database query |
| Localized query | ~15ms | With cached resources |

### Scalability

1. **Horizontal Scaling**:
   - Stateless API design
   - Shared database (SQLite limitation)
   - Can upgrade to SQL Server/PostgreSQL

2. **Vertical Scaling**:
   - Parallel processing scales with CPU cores
   - Memory cache scales with RAM
   - Connection pooling

3. **Caching Strategy**:
   - Reduces database load
   - Faster response times
   - Targeted cache invalidation

---

##  API Examples

### Create Book
```bash
POST /books
Content-Type: application/json

{
  "title": "Advanced C# Programming",
  "author": "Jane Doe",
  "isbn": "978-1-23456-789-0",
  "publishedDate": "2024-01-15",
  "category": "Technical",
  "price": 49.99,
  "stockQuantity": 100,
  "coverImageUrl": "https://example.com/csharp.jpg"
}
```

### Get Paginated Books with Category Filter
```bash
GET /books?category=2&page=1&pageSize=10
```

### Batch Create Books
```bash
POST /books/batch
Content-Type: application/json

{
  "books": [
    {
      "title": "JavaScript Programming Guide",
      "author": "John Smith",
      "isbn": "978-1-11111-001-0",
      ...
    },
    {
      "title": "Python Data Science",
      "author": "Jane Doe",
      "isbn": "978-1-11111-002-7",
      ...
    }
  ]
}
```

### Add Book Localization
```bash
POST /books/{id}/localizations
Content-Type: application/json

{
  "cultureCode": "es",
  "localizedTitle": "Título en Español",
  "localizedDescription": "Descripción detallada"
}
```

### Get Localized Book
```bash
GET /books/{id}/localized?culture=es
```

### Get Metrics
```bash
GET /books/metrics
```

### Get Cache Statistics
```bash
GET /books/cache/stats
```

---

