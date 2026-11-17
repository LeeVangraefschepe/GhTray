using System.Runtime.Versioning;
using Microsoft.Win32;

namespace GhTray.Platform;

[SupportedOSPlatform("windows")]
internal sealed class WindowsPanelTheme : IPanelTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string LightThemeValue = "SystemUsesLightTheme";

    public bool IsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue(LightThemeValue) is 0;
        }
        catch
        {
            return false;
        }
    }
}
