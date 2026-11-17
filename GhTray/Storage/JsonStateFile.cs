using System.Text.Json;

namespace GhTray.Storage;

internal sealed class JsonStateFile<T> where T : class
{
    private readonly string _path;

    public JsonStateFile(string fileName)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GhTray");
        _path = Path.Combine(directory, fileName);
    }

    public T? TryLoad()
    {
        try
        {
            return File.Exists(_path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(_path)) : null;
        }
        catch
        {
            return null;
        }
    }

    public void TrySave(T state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(state));
        }
        catch
        {
            // Persisting state is best effort; the app keeps working without it.
        }
    }
}
