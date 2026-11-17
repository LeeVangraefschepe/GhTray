using System.Text.Json;

namespace GhTray.GitHub;

internal static class LatestActivityAnalyzer
{
    public static bool IsLatestActivityBy(string itemJson, string login)
    {
        var latest = new LatestActivity(login);

        using var document = JsonDocument.Parse(itemJson);
        var root = document.RootElement;

        foreach (var comment in EnumerateArray(root, "comments"))
            latest.Consider(comment, "createdAt", "author");

        foreach (var review in EnumerateArray(root, "reviews"))
            latest.Consider(review, "submittedAt", "author");

        foreach (var commit in EnumerateArray(root, "commits"))
            latest.ConsiderCommit(commit);

        return latest.IsByLogin;
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var array) ? array.EnumerateArray() : [];

    private sealed class LatestActivity(string login)
    {
        private DateTimeOffset _latestAt = DateTimeOffset.MinValue;
        private bool _latestIsByLogin;

        public bool IsByLogin => _latestAt != DateTimeOffset.MinValue && _latestIsByLogin;

        public void Consider(JsonElement entry, string timestampProperty, string authorProperty)
        {
            if (!TryReadTimestamp(entry, timestampProperty, out var at)) return;

            var byLogin = entry.TryGetProperty(authorProperty, out var author) && IsAuthoredByLogin(author);
            Record(at, byLogin);
        }

        public void ConsiderCommit(JsonElement commit)
        {
            if (!TryReadTimestamp(commit, "committedDate", out var at)) return;

            var byLogin = commit.TryGetProperty("authors", out var authors)
                          && authors.EnumerateArray().Any(IsAuthoredByLogin);
            Record(at, byLogin);
        }

        private void Record(DateTimeOffset at, bool byLogin)
        {
            if (at < _latestAt) return;

            _latestAt = at;
            _latestIsByLogin = byLogin;
        }

        private bool IsAuthoredByLogin(JsonElement author) =>
            author.ValueKind == JsonValueKind.Object
            && author.TryGetProperty("login", out var authorLogin)
            && string.Equals(authorLogin.GetString(), login, StringComparison.OrdinalIgnoreCase);

        private static bool TryReadTimestamp(JsonElement entry, string propertyName, out DateTimeOffset at)
        {
            at = default;
            return entry.TryGetProperty(propertyName, out var value)
                   && value.ValueKind == JsonValueKind.String
                   && value.TryGetDateTimeOffset(out at);
        }
    }
}
