using System.Diagnostics;

namespace GhTray.Platform;

public static class BrowserLauncher
{
    public static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // No default browser or opener is available; there is nothing sensible to fall back to.
        }
    }
}
