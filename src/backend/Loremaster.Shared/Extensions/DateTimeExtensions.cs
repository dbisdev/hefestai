namespace Loremaster.Shared.Extensions;

public static class DateTimeExtensions
{
    public static bool IsExpired(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    public static bool IsExpired(this DateTime? dateTime)
    {
        return dateTime.HasValue && dateTime.Value < DateTime.UtcNow;
    }

    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        return timeSpan.TotalSeconds switch
        {
            < 60 => "just now",
            < 120 => "a minute ago",
            < 3600 => $"{timeSpan.Minutes} minutes ago",
            < 7200 => "an hour ago",
            < 86400 => $"{timeSpan.Hours} hours ago",
            < 172800 => "yesterday",
            < 2592000 => $"{timeSpan.Days} days ago",
            < 31104000 => $"{timeSpan.Days / 30} months ago",
            _ => $"{timeSpan.Days / 365} years ago"
        };
    }
}
