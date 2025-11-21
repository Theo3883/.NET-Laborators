using System.Collections.Concurrent;

namespace Lab3.Services;

/// <summary>
/// Singleton service to collect performance metrics across all requests
/// </summary>
public class PerformanceMetricsCollector
{
    private readonly ConcurrentBag<double> _validationTimes = new();
    private readonly ConcurrentBag<double> _databaseOperationTimes = new();

    public void RecordValidationTime(double milliseconds)
    {
        _validationTimes.Add(milliseconds);
    }

    public void RecordDatabaseOperationTime(double milliseconds)
    {
        _databaseOperationTimes.Add(milliseconds);
    }

    public (double[] ValidationTimes, double[] DatabaseTimes) GetMetrics()
    {
        return (_validationTimes.ToArray(), _databaseOperationTimes.ToArray());
    }
}
