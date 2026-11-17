using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace GhTray.Icons;

public static class IconFactory
{
    // Octicons "mark-github", 16x16 viewbox.
    private const string MarkPathData =
        "M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z";

    private const float MarkViewBoxSize = 16f;
    private const int SupersampleFactor = 4;
    private const float MarkInsetRatio = 0.03f;
    private const float BadgeDiameterRatio = 0.40f;
    private const float BadgeGapRatio = 0.04f;

    private static readonly Lazy<GraphicsPath> MarkPath = new(() => SvgPathParser.Parse(MarkPathData));

    private static readonly Color MarkOnLightTaskbar = Color.FromArgb(36, 41, 47);
    private static readonly Color MarkOnDarkTaskbar = Color.White;
    private static readonly Color UnseenBadgeColor = Color.FromArgb(229, 35, 31);
    private static readonly Color WarningBadgeColor = Color.FromArgb(245, 190, 0);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(Badge badge, int size, bool isDarkTaskbar)
    {
        using var bitmap = RenderBitmap(badge, size, isDarkTaskbar);
        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    public static Bitmap RenderBitmap(Badge badge, int size, bool isDarkTaskbar)
    {
        var supersampledSize = size * SupersampleFactor;
        using var supersampled = new Bitmap(supersampledSize, supersampledSize);

        using (var graphics = Graphics.FromImage(supersampled))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            DrawMark(graphics, supersampledSize, isDarkTaskbar);
            if (badge != Badge.None) DrawBadge(graphics, supersampledSize, badge);
        }

        return Downsample(supersampled, size);
    }

    private static void DrawMark(Graphics graphics, int canvasSize, bool isDarkTaskbar)
    {
        var inset = canvasSize * MarkInsetRatio;
        var scale = (canvasSize - 2 * inset) / MarkViewBoxSize;

        using var path = (GraphicsPath)MarkPath.Value.Clone();
        using var transform = new Matrix();
        transform.Translate(inset, inset);
        transform.Scale(scale, scale);
        path.Transform(transform);

        using var brush = new SolidBrush(isDarkTaskbar ? MarkOnDarkTaskbar : MarkOnLightTaskbar);
        graphics.FillPath(brush, path);
    }

    private static void DrawBadge(Graphics graphics, int canvasSize, Badge badge)
    {
        var diameter = canvasSize * BadgeDiameterRatio;
        var gap = canvasSize * BadgeGapRatio;
        var centerX = canvasSize - diameter / 2f;
        var centerY = diameter / 2f;

        // Punch a transparent ring around the dot so it separates from the mark.
        graphics.CompositingMode = CompositingMode.SourceCopy;
        using (var eraser = new SolidBrush(Color.Transparent))
        {
            graphics.FillEllipse(eraser,
                centerX - diameter / 2 - gap, centerY - diameter / 2 - gap,
                diameter + 2 * gap, diameter + 2 * gap);
        }

        graphics.CompositingMode = CompositingMode.SourceOver;
        using var dotBrush = new SolidBrush(badge == Badge.Warning ? WarningBadgeColor : UnseenBadgeColor);
        graphics.FillEllipse(dotBrush, centerX - diameter / 2, centerY - diameter / 2, diameter, diameter);
    }

    private static Bitmap Downsample(Bitmap source, int size)
    {
        var result = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(result);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, size, size));
        return result;
    }
}
