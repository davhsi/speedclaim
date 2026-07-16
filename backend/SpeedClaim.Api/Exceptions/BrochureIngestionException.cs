namespace SpeedClaim.Api.Exceptions;

public sealed class BrochureIngestionException : Exception
{
    public BrochureIngestionException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
