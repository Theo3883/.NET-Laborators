namespace Lab3.Exceptions
{
    public class BaseException : Exception
    {
        private int StatusCode { get; set; }
        private string ErrorCode { get; set; }
        protected BaseException(string message, int statusCode, string errorCode) : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        protected BaseException(string message, Exception innerException, int statusCode, string errorCode) : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}