using System.Drawing.Drawing2D;

namespace GhTray.Icons;

internal static class SvgArcConverter
{
    private const double RadiansToDegrees = 180 / Math.PI;

    // Endpoint-to-centre parameterisation from SVG spec F.6.5; GDI+ and SVG are both y-down.
    public static void AddArc(GraphicsPath path, PointF from, PointF to, float radiusX, float radiusY, bool isLargeArc, bool isSweep)
    {
        if (radiusX == 0 || radiusY == 0)
        {
            path.AddLine(from, to);
            return;
        }

        double halfDeltaX = (from.X - to.X) / 2.0;
        double halfDeltaY = (from.Y - to.Y) / 2.0;

        (radiusX, radiusY) = ScaleRadiiToFit(radiusX, radiusY, halfDeltaX, halfDeltaY);

        double rxSquared = radiusX * (double)radiusX;
        double rySquared = radiusY * (double)radiusY;
        var numerator = rxSquared * rySquared - rxSquared * halfDeltaY * halfDeltaY - rySquared * halfDeltaX * halfDeltaX;
        var denominator = rxSquared * halfDeltaY * halfDeltaY + rySquared * halfDeltaX * halfDeltaX;
        var coefficient = (isLargeArc == isSweep ? -1 : 1) * Math.Sqrt(Math.Max(0, numerator / denominator));

        var centerOffsetX = coefficient * (radiusX * halfDeltaY / radiusY);
        var centerOffsetY = coefficient * -(radiusY * halfDeltaX / radiusX);
        var centerX = centerOffsetX + (from.X + to.X) / 2.0;
        var centerY = centerOffsetY + (from.Y + to.Y) / 2.0;

        var startAngle = Math.Atan2((halfDeltaY - centerOffsetY) / radiusY, (halfDeltaX - centerOffsetX) / radiusX);
        var endAngle = Math.Atan2((-halfDeltaY - centerOffsetY) / radiusY, (-halfDeltaX - centerOffsetX) / radiusX);
        var sweepAngle = NormalizeSweep(endAngle - startAngle, isSweep);

        path.AddArc(
            (float)(centerX - radiusX), (float)(centerY - radiusY),
            2 * radiusX, 2 * radiusY,
            (float)(startAngle * RadiansToDegrees), (float)(sweepAngle * RadiansToDegrees));
    }

    private static (float RadiusX, float RadiusY) ScaleRadiiToFit(float radiusX, float radiusY, double halfDeltaX, double halfDeltaY)
    {
        var overshoot = halfDeltaX * halfDeltaX / (radiusX * (double)radiusX)
                        + halfDeltaY * halfDeltaY / (radiusY * (double)radiusY);
        if (overshoot <= 1) return (radiusX, radiusY);

        var scale = Math.Sqrt(overshoot);
        return ((float)(radiusX * scale), (float)(radiusY * scale));
    }

    private static double NormalizeSweep(double sweepAngle, bool isSweep) => isSweep switch
    {
        false when sweepAngle > 0 => sweepAngle - 2 * Math.PI,
        true when sweepAngle < 0 => sweepAngle + 2 * Math.PI,
        _ => sweepAngle,
    };
}
