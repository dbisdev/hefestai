using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Loremaster.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    public string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("displayName", user.DisplayName ?? string.Empty)
        };

        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: _dateTimeProvider.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public DateTime GetRefreshTokenExpiryTime()
    {
        var expirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        return _dateTimeProvider.UtcNow.AddDays(expirationDays);
    }
}
