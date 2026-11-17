using System.Text.Json;
using GhTray.Models;

namespace GhTray.GitHub;

internal static class WorkItemParser
{
    public static IEnumerable<WorkItem> Parse(string searchResultJson, WorkItemKind kind, bool isAssigned, bool isReviewRequested)
    {
        using var document = JsonDocument.Parse(searchResultJson);
        foreach (var element in document.RootElement.EnumerateArray())
        {
            yield return new WorkItem(
                kind,
                element.GetProperty("repository").GetProperty("nameWithOwner").GetString() ?? "",
                element.GetProperty("number").GetInt32(),
                element.GetProperty("title").GetString() ?? "",
                element.GetProperty("url").GetString() ?? "",
                ReadAuthorLogin(element),
                ReadIsDraft(element),
                element.GetProperty("updatedAt").GetDateTimeOffset(),
                isAssigned,
                isReviewRequested);
        }
    }

    private static string ReadAuthorLogin(JsonElement element) =>
        element.TryGetProperty("author", out var author) && author.TryGetProperty("login", out var login)
            ? login.GetString() ?? ""
            : "";

    private static bool ReadIsDraft(JsonElement element) =>
        element.TryGetProperty("isDraft", out var isDraft) && isDraft.ValueKind == JsonValueKind.True;
}
