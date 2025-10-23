using Lab3.Model;

namespace Lab3.DTO.Request;

/// Request for paginated book retrieval with optional category filtering
public record GetBooksWithPaginationRequest(
    int Page, 
    int PageSize, 
    BookCategory? Category = null);
