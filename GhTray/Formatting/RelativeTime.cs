namespace GhTray.Formatting;

internal static class RelativeTime
{
    public static string Describe(DateTimeOffset timestamp)
    {
        var age = DateTimeOffset.UtcNow - timestamp;
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalHours < 1) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalDays < 1) return $"{(int)age.TotalHours}h ago";
        if (age.TotalDays < 30) return $"{(int)age.TotalDays}d ago";
        return timestamp.LocalDateTime.ToString("yyyy-MM-dd");
    }
}
