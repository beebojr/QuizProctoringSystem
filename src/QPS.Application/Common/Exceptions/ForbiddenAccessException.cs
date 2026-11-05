namespace QPS.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You don't have permission to do that.") : base(message) { }
}
