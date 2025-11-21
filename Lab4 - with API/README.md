
## Contents

- Features overview15148388-149e-42cd-b992-640fb3f33dd9
- Data model
- Request/response contracts (shapes)
- Detailed validation and business rules
- AutoMapper resolvers and why we use them
- Caching: keys, tracking, invalidation strategy
- Localization: XML scheme and fallback algorithm
- Metrics: what we collect and how to extend

---

## Features overview (concise)

The API demonstrates:
- Robust CRUD for orders (books) with category-aware validation
- AutoMapper-driven DTOs with 7+ custom resolvers
- Category-scoped in-memory caching and targeted invalidation
- Multi-language support using XML resource files (no book-specific hardcoding)
- A metrics dashboard exposing operational and performance metrics
- A comprehensive demo script that exercises everything with randomized input

---

## Data model (quick reference)

Primary model: `Book` (File: `Model/Book.cs`)

Fields (not exhaustive):
- `Id` (GUID)
- `Price` (decimal)
- `StockQuantity` (int)
- `CoverImageUrl` (string?)
- `CreatedAt` (DateTime)

DTO: `OrderProfileDto` (includes computed fields and localized fields)

---

## Request / Response Contracts (examples)

Create order (POST /orders):

```json
{
  "title": "Scalable programming System Design",
  "author": "Jane Doe",
  "isbn": "9781234567897",
  "category": "Technical",
  "price": 39.99,
  "publishedDate": "2022-06-01",
  "stockQuantity": 25,
  "keywords": ["programming", "design"]
}
```

Successful response: 201 Created returns `OrderProfileDto` with computed fields.

Error response: 400 with RFC-7807 problem details containing `errors` map and `traceId`.

---

## Validation & Business Rules (detailed)

Validation is implemented primarily with FluentValidation. Important rules:

1. ISBN Format & Uniqueness
- ISBN must match 10 or 13 digits
- Uniqueness checked asynchronously against the DB; violations return 400 with `ISBN` error

2. Technical Books (category == Technical)
- Minimum price: $20.00
- Title must contain at least one token from `TECHNICAL_KEYWORDS`
- `keywords` array must include at least one technical keyword
- Published date must be within the last 5 years

3. Children Books
- Maximum price: $50.00
- AutoMapper applies a 10% discount for display purposes

4. Global rules
- Price must be >= 0
- Stock must be >= 0
- Title required (1-200 chars)
- Author required (2-100 chars)

Async checks are executed using a scoped DbContext. If you add more async checks, keep them minimal to avoid significant latency.

---

## AutoMapper — resolvers & patterns

AutoMapper mappings centralize presentation logic. Main resolvers:
- `CategoryDisplayResolver` — map enum to friendly name (localized when requested)
- `AvailabilityStatusResolver` — compute stock status (In Stock / Low Stock / Out of Stock)
- `ConditionalCoverImageResolver` — children books under threshold may have cover nulled
- `ConditionalPriceResolver` — adjust displayed price for children books by 10% discount

Why resolvers?
- Keep DTO-specific display logic isolated from domain model
- Reuse across multiple endpoints and views

---

## Caching: keys, tracking, invalidation

Cache key format examples:
- `orders_category_Technical_all`
- `orders_category_Fiction_page_1_size_10`

Tracking
- A dictionary (in-memory) maps categories -> set(cacheKeys)
- On read, we add keys to the set for that category
- On write, we iterate keys in the category set, remove them from the cache and clear the set

Invalidation strategy
- Targeted: only keys for affected categories are removed
- Ensures cached responses for other categories remain available

Scaling recommendation
- Use Redis sets for each category to allow multi-process invalidation
- Keep keys deterministic so different app instances can compute and invalidate the same keys

---

## Localization (XML resources) — structure & fallback

XML files: `Resources/OrderMetadata.{culture}.xml`

Schema (high-level):

```xml
<OrderMetadata culture="en-US">
  <CategoryTranslations>
    <Category Key="Technical">Technical & Professional</Category>
    <Category Key="Fiction">Fiction & Literature</Category>
    ...
  </CategoryTranslations>
  <CommonTerms>
    <Term Key="InStock">In Stock</Term>
    <Term Key="OutOfStock">Out of Stock</Term>
  </CommonTerms>
  <Descriptions>
    <CategoryDescription Category="Technical">Books for software professionals...</CategoryDescription>
  </Descriptions>
</OrderMetadata>
```

Loading & fallbacks
- `OrderLocalizationService` scans `Resources/` for `OrderMetadata.*.xml` and loads them into memory on startup.
- Fallback order:
  1. Exact culture (e.g., `fr-FR`)
  2. Neutral culture (e.g., `fr`)
  3. Default `en-US`
  4. Formatted enum as last resort

Endpoint: `GET /orders/localized?culture={culture}` returns localized DTO fields.

---

## Metrics & Performance Collection

- `PerformanceMetricsCollector` records:
  - Validation timings (ms)
  - Database save timings (ms)
  - Cache operations counts

- `GET /orders/metrics` aggregates data per the collector and the database queries to produce a dashboard payload.

Suggested extension
- Forward metrics to Prometheus (via an exporter) or push to Application Insights for long-term retention.

---

