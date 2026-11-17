using System.Diagnostics;
using System.Security;

namespace GhTray.Platform;

/// <summary>Sends desktop notifications through <c>notify-send</c> (package <c>libnotify-bin</c>).</summary>
internal sealed class LinuxNotificationSender : INotificationSender
{
    private const int DisplayMilliseconds = 8000;
    private const string DefaultActionKey = "default";
    private static readonly TimeSpan MaxWaitForClick = TimeSpan.FromMinutes(10);

    public event Action? Activated;

    public void Show(NotificationMessage message) => _ = ShowAsync(message);

    private async Task ShowAsync(NotificationMessage message)
    {
        try
        {
            using var timeout = new CancellationTokenSource(MaxWaitForClick);

            var withAction = await RunAsync(BuildArguments(message, withAction: true), timeout.Token);
            if (withAction.ExitCode == 0)
            {
                if (withAction.Output.Trim() == DefaultActionKey) OnClicked(message);
                return;
            }

            // Older libnotify (before 0.8) has no --action; show a plain notification instead.
            await RunAsync(BuildArguments(message, withAction: false), timeout.Token);
        }
        catch
        {
            // Notifications are optional; a missing notify-send must not affect the app.
        }
    }

    private void OnClicked(NotificationMessage message)
    {
        if (message.ClickUrl is { } url) BrowserLauncher.Open(url);
        else Activated?.Invoke();
    }

    private static List<string> BuildArguments(NotificationMessage message, bool withAction)
    {
        var icon = message.Severity == NotificationSeverity.Error ? "dialog-error" : "dialog-information";
        var arguments = new List<string>
        {
            "--app-name=GhTray",
            $"--icon={icon}",
            $"--expire-time={DisplayMilliseconds}",
        };

        if (withAction)
        {
            arguments.Add($"--action={DefaultActionKey}=Open");
            arguments.Add("--wait");
        }

        arguments.Add("--");
        arguments.Add(message.Title);
        arguments.Add(SecurityElement.Escape(message.Body) ?? "");
        return arguments;
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(List<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("notify-send")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start notify-send.");
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await output);
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            throw;
        }
    }
}
