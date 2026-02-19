using System.Net;
using System.Text.RegularExpressions;

namespace Loremaster.Shared.Helpers;

public static class UrlValidationHelper
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http",
        "https"
    };

    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1"
    };

    private static readonly Regex PrivateIpRegex = new(
        @"^(127\.|10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.|192\.168\.)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LocalhostRegex = new(
        @"^(localhost|127\.0\.0\.1|::1|::ffff:127\.0\.0\.1|0\.0\.0\.0)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (bool IsValid, string? ErrorMessage) ValidateUrl(string? url, bool allowHttp = false)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "URL is required");
        }

        if (url.Length > 2048)
        {
            return (false, "URL exceeds maximum length of 2048 characters");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return (false, "Invalid URL format");
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            return (false, $"URL scheme '{uri.Scheme}' is not allowed. Use http or https.");
        }

        if (!allowHttp && uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "HTTP URLs are not allowed. Use HTTPS.");
        }

        var host = uri.Host;

        if (LocalhostRegex.IsMatch(host))
        {
            return (false, "Localhost URLs are not allowed");
        }

        if (PrivateIpRegex.IsMatch(host))
        {
            return (false, "Private IP addresses are not allowed");
        }

        if (IPAddress.TryParse(host, out var ipAddress))
        {
            if (IPAddress.IsLoopback(ipAddress))
            {
                return (false, "Loopback addresses are not allowed");
            }

            if (IsPrivateIP(ipAddress))
            {
                return (false, "Private IP addresses are not allowed");
            }
        }

        if (host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Cloud metadata endpoints are not allowed");
        }

        return (true, null);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateUrlAllowLocal(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "URL is required");
        }

        if (url.Length > 2048)
        {
            return (false, "URL exceeds maximum length of 2048 characters");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return (false, "Invalid URL format");
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            return (false, $"URL scheme '{uri.Scheme}' is not allowed. Use http or https.");
        }

        var host = uri.Host;

        if (host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Cloud metadata endpoints are not allowed");
        }

        return (true, null);
    }

    private static bool IsPrivateIP(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        
        if (bytes[0] == 10) return true;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        if (bytes[0] == 169 && bytes[1] == 254) return true;
        
        return false;
    }
}
