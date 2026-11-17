using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.Imaging;
using Avalonia.Controls;

namespace GhTray.Icons;

public static class IconFactory
{
    // Octicons "mark-github", 16x16 viewbox.
    private const string MarkPathData =
        "M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z";

    private const double MarkViewBoxSize = 16;
    private const double MarkInsetRatio = 0.03;
    private const double BadgeDiameter = MarkViewBoxSize * 0.40;
    private const double BadgeGap = MarkViewBoxSize * 0.04;
    private const double Dpi = 96;

    private static readonly IBrush MarkOnLightPanel = new ImmutableSolidColorBrush(Color.FromRgb(36, 41, 47));
    private static readonly IBrush MarkOnDarkPanel = Brushes.White;
    private static readonly IBrush UnseenBadgeBrush = new ImmutableSolidColorBrush(Color.FromRgb(229, 35, 31));
    private static readonly IBrush WarningBadgeBrush = new ImmutableSolidColorBrush(Color.FromRgb(245, 190, 0));

    public static WindowIcon Create(Badge badge, int size, bool isDarkPanel)
    {
        using var bitmap = Render(badge, size, isDarkPanel);
        return new WindowIcon(bitmap);
    }

    public static RenderTargetBitmap Render(Badge badge, int size, bool isDarkPanel)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(Dpi, Dpi));
        using var context = bitmap.CreateDrawingContext();

        var scale = size / MarkViewBoxSize;
        using (context.PushTransform(Matrix.CreateScale(scale, scale)))
        {
            var mark = CreateMarkGeometry();
            if (badge != Badge.None) mark = CutOutBadgeRing(mark);

            context.DrawGeometry(isDarkPanel ? MarkOnDarkPanel : MarkOnLightPanel, null, mark);
            if (badge != Badge.None) context.DrawGeometry(BadgeBrushFor(badge), null, CreateBadgeGeometry(BadgeDiameter / 2));
        }

        return bitmap;
    }

    private static Geometry CreateMarkGeometry()
    {
        var inset = MarkViewBoxSize * MarkInsetRatio;
        var insetScale = (MarkViewBoxSize - 2 * inset) / MarkViewBoxSize;

        var mark = StreamGeometry.Parse(MarkPathData);
        mark.Transform = new MatrixTransform(Matrix.CreateScale(insetScale, insetScale) * Matrix.CreateTranslation(inset, inset));
        return mark;
    }

    private static Geometry CutOutBadgeRing(Geometry mark) =>
        new CombinedGeometry(GeometryCombineMode.Exclude, mark, CreateBadgeGeometry(BadgeDiameter / 2 + BadgeGap));

    private static Geometry CreateBadgeGeometry(double radius)
    {
        var center = new Point(MarkViewBoxSize - BadgeDiameter / 2, BadgeDiameter / 2);
        return new EllipseGeometry(new Rect(center.X - radius, center.Y - radius, 2 * radius, 2 * radius));
    }

    private static IBrush BadgeBrushFor(Badge badge) =>
        badge == Badge.Warning ? WarningBadgeBrush : UnseenBadgeBrush;
}
