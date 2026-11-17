namespace GhTray.Formatting;

internal static class TextTruncation
{
    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "...";
}
