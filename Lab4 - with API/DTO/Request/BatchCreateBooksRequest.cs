using Lab3.Model;

namespace Lab3.DTO.Request;

/// Request for batch book creation with inventory management
public record BatchCreateBooksRequest(
    List<CreateBookProfileRequest> Books
);

/// Response for batch book creation operation
public record BatchCreateBooksResponse(
    int TotalRequested,
    int SuccessfullyCreated,
    int Failed,
    List<BookProfileDto> CreatedBooks,
    List<BatchBookError> Errors,
    TimeSpan ProcessingTime,
    string OperationId
);

/// Error details for failed book creation in batch
public record BatchBookError(
    int Index,
    string Title,
    string ISBN,
    List<string> ValidationErrors
);
