using GhTray.Tray;

namespace GhTray;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\GhTray.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
