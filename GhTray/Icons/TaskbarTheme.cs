using Microsoft.Win32;

namespace GhTray.Icons;

internal static class TaskbarTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string LightThemeValue = "SystemUsesLightTheme";

    public static bool IsDark()
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
