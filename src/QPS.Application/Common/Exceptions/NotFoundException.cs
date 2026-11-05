namespace QPS.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string entity, object key) : base($"{entity} was not found ({key}).") { }
}
