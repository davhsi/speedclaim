namespace SpeedClaim.Api.Exceptions;

public class UnprocessableException : AppException
{
    public UnprocessableException(string message) : base(message, 422) { }
}
