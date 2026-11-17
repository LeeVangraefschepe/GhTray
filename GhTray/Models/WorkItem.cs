namespace GhTray.Models;

public sealed record WorkItem(
    WorkItemKind Kind,
    string Repository,
    int Number,
    string Title,
    string Url,
    string Author,
    bool IsDraft,
    DateTimeOffset UpdatedAt,
    bool IsAssigned = true,
    bool IsReviewRequested = false,
    CheckSummary? Checks = null)
{
    public string Key => Url;

    public bool IsPullRequest => Kind == WorkItemKind.PullRequest;

    public string Reference => $"{Repository}#{Number}";
}
