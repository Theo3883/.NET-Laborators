namespace Lab3.Exceptions;

public class BookNotFoundException(Guid bookId)
    : BaseException($"Book with ID {bookId} was not found.", 404, "BOOK_NOT_FOUND");