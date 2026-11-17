using System.Diagnostics;

namespace GhTray.Platform;

internal sealed class LinuxPanelTheme : IPanelTheme
{
    private static readonly string[] AlwaysDarkPanelDesktops = ["GNOME", "Unity", "Ubuntu"];

    public bool IsDark()
    {
        // The GNOME and Ubuntu top bar stays dark whichever application theme is active.
        if (RunsOnAlwaysDarkPanelDesktop()) return true;

        var colorScheme = ReadGSetting("org.gnome.desktop.interface", "color-scheme");
        return colorScheme is null || !colorScheme.Contains("light", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RunsOnAlwaysDarkPanelDesktop()
    {
        var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
        return AlwaysDarkPanelDesktops.Any(name => desktop.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadGSetting(string schema, string key)
    {
        try
        {
            var startInfo = new ProcessStartInfo("gsettings")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("get");
            startInfo.ArgumentList.Add(schema);
            startInfo.ArgumentList.Add(key);

            using var process = Process.Start(startInfo);
            if (process is null) return null;

            var output = process.StandardOutput.ReadToEnd();
            return process.WaitForExit(2000) && process.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
