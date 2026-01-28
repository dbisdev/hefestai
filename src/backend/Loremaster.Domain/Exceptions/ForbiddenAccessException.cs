namespace Loremaster.Domain.Exceptions;

public class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException() : base("You do not have permission to access this resource.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
