using System.Drawing.Drawing2D;

namespace GhTray.Icons;

/// <summary>Supports the commands M C S L H V A Z (absolute and relative). Arcs must be unrotated.</summary>
internal sealed class SvgPathParser
{
    private readonly SvgPathReader _reader;
    private readonly GraphicsPath _path = new();

    private PointF _current;
    private PointF _subpathStart;
    private PointF _lastControlPoint;
    private bool _previousWasCubic;

    private SvgPathParser(string pathData) => _reader = new SvgPathReader(pathData);

    public static GraphicsPath Parse(string pathData) => new SvgPathParser(pathData).Build();

    private GraphicsPath Build()
    {
        var command = ' ';

        while (_reader.HasMore)
        {
            if (_reader.NextIsCommand) command = _reader.ReadCommand();
            else command = ImplicitCommandAfter(command);

            var origin = char.IsLower(command) ? _current : PointF.Empty;
            var isCubic = Execute(command, origin);
            _previousWasCubic = isCubic;
        }

        return _path;
    }

    private static char ImplicitCommandAfter(char command) => command switch
    {
        'M' => 'L',
        'm' => 'l',
        _ => command,
    };

    private bool Execute(char command, PointF origin)
    {
        switch (char.ToUpperInvariant(command))
        {
            case 'M': MoveTo(origin); return false;
            case 'L': LineTo(_reader.ReadPoint(origin)); return false;
            case 'H': LineTo(new PointF(origin.X + _reader.ReadNumber(), _current.Y)); return false;
            case 'V': LineTo(new PointF(_current.X, origin.Y + _reader.ReadNumber())); return false;
            case 'C': CubicTo(origin); return true;
            case 'S': SmoothCubicTo(origin); return true;
            case 'A': ArcTo(origin); return false;
            case 'Z': ClosePath(); return false;
            default: throw new FormatException($"Unsupported SVG path command '{command}'.");
        }
    }

    private void MoveTo(PointF origin)
    {
        _current = _subpathStart = _reader.ReadPoint(origin);
        _path.StartFigure();
    }

    private void LineTo(PointF target)
    {
        _path.AddLine(_current, target);
        _current = target;
    }

    private void CubicTo(PointF origin)
    {
        var firstControl = _reader.ReadPoint(origin);
        AddCubic(firstControl, origin);
    }

    private void SmoothCubicTo(PointF origin)
    {
        var firstControl = _previousWasCubic
            ? new PointF(2 * _current.X - _lastControlPoint.X, 2 * _current.Y - _lastControlPoint.Y)
            : _current;
        AddCubic(firstControl, origin);
    }

    private void AddCubic(PointF firstControl, PointF origin)
    {
        var secondControl = _reader.ReadPoint(origin);
        var target = _reader.ReadPoint(origin);

        _path.AddBezier(_current, firstControl, secondControl, target);
        _current = target;
        _lastControlPoint = secondControl;
    }

    private void ArcTo(PointF origin)
    {
        var radiusX = _reader.ReadNumber();
        var radiusY = _reader.ReadNumber();
        _reader.ReadNumber(); // x-axis rotation: unsupported, assumed 0
        var isLargeArc = _reader.ReadFlag();
        var isSweep = _reader.ReadFlag();
        var target = _reader.ReadPoint(origin);

        SvgArcConverter.AddArc(_path, _current, target, radiusX, radiusY, isLargeArc, isSweep);
        _current = target;
    }

    private void ClosePath()
    {
        _path.CloseFigure();
        _current = _subpathStart;
    }
}
