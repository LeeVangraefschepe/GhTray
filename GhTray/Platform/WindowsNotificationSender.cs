using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace GhTray.Platform;

/// <summary>Shows Windows toasts by driving the WinRT toast API from a short-lived hidden PowerShell process.</summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsNotificationSender : INotificationSender
{
    private const string AppId = "GhTray";
    private const string AppRegistrationKey = @"Software\Classes\AppUserModelId\" + AppId;
    private const string ToastXmlVariable = "GHTRAY_TOAST_XML";
    private const string AppIdVariable = "GHTRAY_APP_ID";

    private const string ShowToastScript = """
        $ErrorActionPreference = 'Stop'
        [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
        [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
        $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
        $xml.LoadXml($env:GHTRAY_TOAST_XML)
        $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($env:GHTRAY_APP_ID).Show($toast)
        """;

    private static readonly string EncodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(ShowToastScript));

    // Windows hands clicks on a toast to the OS (protocol activation), so this never fires.
    public event Action? Activated
    {
        add { }
        remove { }
    }

    public void Show(NotificationMessage message) => _ = ShowAsync(message);

    private static async Task ShowAsync(NotificationMessage message)
    {
        try
        {
            RegisterAppDisplayName();

            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-EncodedCommand");
            startInfo.ArgumentList.Add(EncodedScript);
            startInfo.Environment[ToastXmlVariable] = BuildToastXml(message);
            startInfo.Environment[AppIdVariable] = AppId;

            using var process = Process.Start(startInfo);
            if (process is not null) await process.WaitForExitAsync();
        }
        catch
        {
            // Notifications are optional; a failing toast must not affect the app.
        }
    }

    private static void RegisterAppDisplayName()
    {
        using var key = Registry.CurrentUser.CreateSubKey(AppRegistrationKey);
        key.SetValue("DisplayName", "GhTray");
    }

    private static string BuildToastXml(NotificationMessage message)
    {
        var launch = message.ClickUrl is { } url
            ? $" activationType=\"protocol\" launch=\"{SecurityElement.Escape(url)}\""
            : "";

        return $"<toast{launch}><visual><binding template=\"ToastGeneric\">" +
               $"<text>{SecurityElement.Escape(message.Title)}</text>" +
               $"<text>{SecurityElement.Escape(message.Body)}</text>" +
               "</binding></visual></toast>";
    }
}
