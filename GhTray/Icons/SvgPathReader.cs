using System.Globalization;

namespace GhTray.Icons;

internal sealed class SvgPathReader(string pathData)
{
    private int _position;

    public bool HasMore
    {
        get
        {
            SkipSeparators();
            return _position < pathData.Length;
        }
    }

    public bool NextIsCommand => char.IsLetter(pathData[_position]);

    public char ReadCommand() => pathData[_position++];

    public bool ReadFlag()
    {
        SkipSeparators();
        return pathData[_position++] == '1';
    }

    public float ReadNumber()
    {
        SkipSeparators();
        var start = _position;

        if (_position < pathData.Length && pathData[_position] is '-' or '+') _position++;

        var seenDecimalPoint = false;
        while (_position < pathData.Length)
        {
            var current = pathData[_position];
            if (char.IsDigit(current)) _position++;
            else if (current == '.' && !seenDecimalPoint) { seenDecimalPoint = true; _position++; }
            else break;
        }

        return float.Parse(pathData.AsSpan(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public PointF ReadPoint(PointF origin) => new(origin.X + ReadNumber(), origin.Y + ReadNumber());

    private void SkipSeparators()
    {
        while (_position < pathData.Length && (char.IsWhiteSpace(pathData[_position]) || pathData[_position] == ','))
            _position++;
    }
}
