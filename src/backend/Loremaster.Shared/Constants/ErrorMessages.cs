namespace Loremaster.Shared.Constants;

public static class ErrorMessages
{
    public const string NotFound = "The requested resource was not found.";
    public const string Unauthorized = "You are not authorized to perform this action.";
    public const string Forbidden = "Access to this resource is forbidden.";
    public const string ValidationFailed = "One or more validation errors occurred.";
    public const string InternalError = "An internal server error occurred.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string EmailAlreadyExists = "A user with this email already exists.";
    public const string InvalidRefreshToken = "Invalid or expired refresh token.";
    public const string AccountDeactivated = "This account has been deactivated.";
}
