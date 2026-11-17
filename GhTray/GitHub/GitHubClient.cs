using GhTray.Models;

namespace GhTray.GitHub;

public sealed class GitHubClient
{
    private const string PullRequestFields = "number,title,url,updatedAt,repository,author,isDraft";
    private const string IssueFields = "number,title,url,updatedAt,repository,author";
    private const int SearchLimit = 100;
    private const int MaxParallelCheckLookups = 6;

    private string? _authenticatedLogin;

    public async Task<IReadOnlyList<WorkItem>> GetAssignedAsync(CancellationToken cancellationToken = default)
    {
        var assignedPullRequestsJson = SearchAsync("prs", "--assignee", PullRequestFields, cancellationToken);
        var reviewRequestedJson = SearchAsync("prs", "--review-requested", PullRequestFields, cancellationToken);
        var assignedIssuesJson = SearchAsync("issues", "--assignee", IssueFields, cancellationToken);
        await Task.WhenAll(assignedPullRequestsJson, reviewRequestedJson, assignedIssuesJson);

        var pullRequests = MergePullRequests(assignedPullRequestsJson.Result, reviewRequestedJson.Result);
        var pullRequestsWithChecks = await AttachChecksAsync(pullRequests, cancellationToken);
        var issues = WorkItemParser.Parse(assignedIssuesJson.Result, WorkItemKind.Issue, isAssigned: true, isReviewRequested: false);

        return pullRequestsWithChecks
            .Concat(issues)
            .OrderByDescending(item => item.UpdatedAt)
            .ToList();
    }

    public async Task<bool> IsLatestActivityMineAsync(WorkItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            var login = await GetAuthenticatedLoginAsync(cancellationToken);
            if (login is null) return false;

            var json = await GhCli.RunAsync(BuildViewArguments(item), cancellationToken);
            return LatestActivityAnalyzer.IsLatestActivityBy(json, login);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static Task<string> SearchAsync(string subject, string relationFlag, string fields, CancellationToken cancellationToken) =>
        GhCli.RunAsync(
            ["search", subject, relationFlag, "@me", "--state", "open", "--limit", SearchLimit.ToString(), "--json", fields],
            cancellationToken);

    private static string[] BuildViewArguments(WorkItem item) =>
        item.IsPullRequest
            ? ["pr", "view", item.Url, "--json", "commits,comments,reviews"]
            : ["issue", "view", item.Url, "--json", "comments"];

    private static List<WorkItem> MergePullRequests(string assignedJson, string reviewRequestedJson)
    {
        var byUrl = new Dictionary<string, WorkItem>();

        foreach (var pullRequest in WorkItemParser.Parse(assignedJson, WorkItemKind.PullRequest, isAssigned: true, isReviewRequested: false))
            byUrl[pullRequest.Url] = pullRequest;

        foreach (var pullRequest in WorkItemParser.Parse(reviewRequestedJson, WorkItemKind.PullRequest, isAssigned: false, isReviewRequested: true))
        {
            byUrl[pullRequest.Url] = byUrl.TryGetValue(pullRequest.Url, out var existing)
                ? existing with { IsReviewRequested = true }
                : pullRequest;
        }

        return byUrl.Values.ToList();
    }

    private static async Task<WorkItem[]> AttachChecksAsync(IEnumerable<WorkItem> pullRequests, CancellationToken cancellationToken)
    {
        using var throttle = new SemaphoreSlim(MaxParallelCheckLookups);

        return await Task.WhenAll(pullRequests.Select(async pullRequest =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                return pullRequest with { Checks = await GetChecksAsync(pullRequest.Url, cancellationToken) };
            }
            finally
            {
                throttle.Release();
            }
        }));
    }

    private static async Task<CheckSummary?> GetChecksAsync(string pullRequestUrl, CancellationToken cancellationToken)
    {
        try
        {
            // gh exits non-zero for failing or pending checks even with --json, so judge by the output instead.
            var result = await GhCli.RunRawAsync(["pr", "checks", pullRequestUrl, "--json", "name,bucket"], cancellationToken);
            return CheckSummaryParser.Parse(result.StandardOutput);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetAuthenticatedLoginAsync(CancellationToken cancellationToken)
    {
        if (_authenticatedLogin is not null) return _authenticatedLogin;

        var login = (await GhCli.RunAsync(["api", "user", "--jq", ".login"], cancellationToken)).Trim();
        if (login.Length > 0) _authenticatedLogin = login;
        return _authenticatedLogin;
    }
}
