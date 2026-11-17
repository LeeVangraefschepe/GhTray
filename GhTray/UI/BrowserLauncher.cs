using System.Diagnostics;

namespace GhTray.UI;

public static class BrowserLauncher
{
    public static void Open(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
