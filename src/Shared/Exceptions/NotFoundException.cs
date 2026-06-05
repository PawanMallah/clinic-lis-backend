namespace Shared.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' not found") { }

    public NotFoundException(string message) : base(message) { }
}
