namespace Loremaster.Application.Common.Interfaces;

/// <summary>
/// Interface for generating service-to-service JWT tokens
/// </summary>
public interface IServiceTokenGenerator
{
    /// <summary>
    /// Generate a JWT token for service-to-service communication
    /// </summary>
    /// <param name="scopes">The scopes to include in the token</param>
    /// <returns>JWT token string</returns>
    string GenerateServiceToken(params string[] scopes);
}

/// <summary>
/// Available scopes for service-to-service communication
/// </summary>
public static class ServiceScopes
{
    public const string GenkitExecute = "genkit.execute";
    public const string GenkitAdmin = "genkit.admin";
}
