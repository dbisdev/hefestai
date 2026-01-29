using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Loremaster.Infrastructure.Identity;

/// <summary>
/// Generates JWT tokens for service-to-service authentication
/// </summary>
public class ServiceTokenGenerator : IServiceTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ServiceTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    public string GenerateServiceToken(params string[] scopes)
    {
        var secret = _configuration["ServiceJwt:Secret"] 
            ?? throw new InvalidOperationException("ServiceJwt:Secret is not configured");
        
        var issuer = _configuration["ServiceJwt:Issuer"] ?? "Loremaster.Api";
        var audience = _configuration["ServiceJwt:Audience"] ?? "Loremaster.Genkit";
        var expirationMinutes = int.Parse(_configuration["ServiceJwt:ExpirationMinutes"] ?? "5");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var now = _dateTimeProvider.UtcNow;
        var unixNow = new DateTimeOffset(now).ToUnixTimeSeconds();
        var unixExp = new DateTimeOffset(now.AddMinutes(expirationMinutes)).ToUnixTimeSeconds();
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "loremaster-backend"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, unixNow.ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, unixExp.ToString(), ClaimValueTypes.Integer64),
            new Claim("scope", string.Join(" ", scopes)),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
