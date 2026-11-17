using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace GhTray.UI;

internal static class Palette
{
    public const double BodyFontSize = 13;
    public const double SmallFontSize = 11;
    public const double PillFontSize = 10;
    public const double TabFontSize = 14;

    public static readonly IBrush Text = Brushes.Black;
    public static readonly IBrush Window = Brushes.White;
    public static readonly IBrush Stripe = Rgb(247, 248, 250);
    public static readonly IBrush Selection = Rgb(221, 233, 250);
    public static readonly IBrush Muted = Rgb(110, 119, 129);
    public static readonly IBrush Fresh = Rgb(26, 127, 55);
    public static readonly IBrush Accent = Rgb(9, 105, 218);
    public static readonly IBrush PillFill = Rgb(234, 238, 242);
    public static readonly IBrush ReviewFill = Rgb(221, 244, 255);
    public static readonly IBrush Failure = Rgb(207, 34, 46);
    public static readonly IBrush Pending = Rgb(154, 103, 0);
    public static readonly IBrush HeaderFill = Rgb(246, 248, 250);
    public static readonly IBrush HeaderBorder = Rgb(216, 222, 228);
    public static readonly IBrush StatusFill = Rgb(240, 240, 240);
    public static readonly IBrush WarningFill = Rgb(255, 241, 184);
    public static readonly IBrush WarningText = Rgb(102, 70, 0);

    private static IBrush Rgb(byte red, byte green, byte blue) =>
        new ImmutableSolidColorBrush(Color.FromRgb(red, green, blue));
}
