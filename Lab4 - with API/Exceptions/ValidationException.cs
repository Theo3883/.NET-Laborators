namespace Lab3.Exceptions;

public class ValidatorsException : BaseException
{

    public List<string> Errors { get; } = new();

    public ValidatorsException(IEnumerable<string> errors)
        : base($"Validation failed: {string.Join("; ", errors)}", 400, "VALIDATION_FAILED")
    {
        Errors = errors.ToList();
    }

    protected ValidatorsException(string error) : base(error, 400, "VALIDATION_FAILED")
    {
        Errors = new List<string> { error };
    }
}