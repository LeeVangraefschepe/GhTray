namespace GhTray.Platform;

internal sealed class UnsupportedAutostartRegistration : IAutostartRegistration
{
    public bool IsSupported => false;

    public bool IsEnabled() => false;

    public void SetEnabled(bool enabled)
    {
    }
}
