using System.ComponentModel;
using System.Diagnostics;

namespace GhTray.GitHub;

internal readonly record struct GhResult(int ExitCode, string StandardOutput, string StandardError);

internal static class GhCli
{
    public static async Task<string> RunAsync(string[] arguments, CancellationToken cancellationToken)
    {
        var result = await RunRawAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            var command = string.Join(' ', arguments.Take(2));
            throw new InvalidOperationException($"gh {command} failed: {result.StandardError.Trim()}");
        }

        return result.StandardOutput;
    }

    public static async Task<GhResult> RunRawAsync(string[] arguments, CancellationToken cancellationToken)
    {
        using var process = StartProcess(arguments);

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return new GhResult(process.ExitCode, await standardOutput, await standardError);
    }

    private static Process StartProcess(string[] arguments)
    {
        var startInfo = new ProcessStartInfo("gh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        try
        {
            return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start gh.");
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException(
                "The GitHub CLI ('gh') was not found on PATH. Install it from https://cli.github.com and run 'gh auth login'.");
        }
    }
}
