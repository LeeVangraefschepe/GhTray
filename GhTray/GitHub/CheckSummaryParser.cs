using System.Text.Json;
using GhTray.Models;

namespace GhTray.GitHub;

internal static class CheckSummaryParser
{
    public static CheckSummary Parse(string checksJson)
    {
        if (string.IsNullOrWhiteSpace(checksJson)) return CheckSummary.Empty;

        var passed = 0;
        var pending = 0;
        var failedNames = new List<string>();

        using var document = JsonDocument.Parse(checksJson);
        foreach (var check in document.RootElement.EnumerateArray())
        {
            var bucket = check.TryGetProperty("bucket", out var bucketElement) ? bucketElement.GetString() : null;
            switch (bucket)
            {
                case "pass": passed++; break;
                case "fail": failedNames.Add(check.GetProperty("name").GetString() ?? "?"); break;
                case "pending": pending++; break;
            }
        }

        failedNames.Sort(StringComparer.OrdinalIgnoreCase);
        return new CheckSummary(passed, failedNames.Count, pending, failedNames);
    }
}
