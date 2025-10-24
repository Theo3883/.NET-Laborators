namespace Lab3.DTO.Request;

public record GetOrdersWithPaginationRequest(int Page = 1, int PageSize = 10);
