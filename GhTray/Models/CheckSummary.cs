namespace GhTray.Models;

public sealed record CheckSummary(int Passed, int Failed, int Pending, IReadOnlyList<string> FailedNames)
{
    public static CheckSummary Empty { get; } = new(0, 0, 0, []);

    public int Total => Passed + Failed + Pending;
}
