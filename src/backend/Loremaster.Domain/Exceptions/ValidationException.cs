namespace Loremaster.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : DomainException
{
    /// <summary>
    /// Dictionary of field names to validation error messages.
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Creates a new ValidationException with default message.
    /// </summary>
    public ValidationException() : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Creates a new ValidationException with the specified errors dictionary.
    /// </summary>
    /// <param name="errors">Dictionary of field names to error messages.</param>
    public ValidationException(IDictionary<string, string[]> errors) : this()
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a new ValidationException with a single message.
    /// The message is stored under a general "Validation" key.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { "Validation", new[] { message } }
        };
    }

    /// <summary>
    /// Creates a new ValidationException with a field name and message.
    /// </summary>
    /// <param name="fieldName">The field that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string fieldName, string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { fieldName, new[] { message } }
        };
    }
}
