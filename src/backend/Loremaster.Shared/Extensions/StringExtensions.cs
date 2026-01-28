namespace Loremaster.Shared.Extensions;

public static class StringExtensions
{
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .ToLowerInvariant()
            .Trim()
            .Replace(" ", "-")
            .Replace("--", "-");
    }

    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..(maxLength - suffix.Length)] + suffix;
    }

    public static bool IsValidEmail(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }
}
