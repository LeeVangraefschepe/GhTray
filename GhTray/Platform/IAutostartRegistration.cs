namespace GhTray.Platform;

public interface IAutostartRegistration
{
    bool IsSupported { get; }

    bool IsEnabled();

    void SetEnabled(bool enabled);
}
