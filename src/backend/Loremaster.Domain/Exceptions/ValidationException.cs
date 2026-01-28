namespace Loremaster.Domain.Exceptions;

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }
}
