using Lab3.DTO.Response;

namespace Lab3.Services;

public interface IOrderMetricsService
{
    Task<OrderMetricsDto> GetOrderMetricsAsync();
    void RecordValidationTime(double milliseconds);
    void RecordDatabaseOperationTime(double milliseconds);
}
