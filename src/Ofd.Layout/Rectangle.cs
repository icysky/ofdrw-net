using System.Globalization;

namespace Ofd.Layout;

/// <summary>矩形区域（Java Rectangle）。</summary>
public class Rectangle
{
    private double _x;
    private double _y;
    private double _width;
    private double _height;

    /// <summary>空矩形。</summary>
    public static readonly Rectangle Empty = new(0, 0);

    public Rectangle(double x, double y, double width, double height)
    {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    public Rectangle(double width, double height) : this(0, 0, width, height) { }

    /// <summary>增加宽度（正增负减）。</summary>
    public void AddToWidth(double delta) => _width += delta;

    /// <summary>增加高度（正增负减）。</summary>
    public void AddToHeight(double delta) => _height += delta;

    public bool IsEmpty() => _width == 0 && _height == 0;

    public double GetWidth() => _width;

    public void SetWidth(double width) => _width = width;

    public double GetHeight() => _height;

    public void SetHeight(double height) => _height = height;

    public double GetX() => _x;

    public void SetX(double x) => _x = x;

    public double GetY() => _y;

    public void SetY(double y) => _y = y;

    /// <summary>从顶部缩减高度，返回被缩减出的区域（原对象高度减少、y 下移）。</summary>
    public Rectangle Reduce(double height)
    {
        var res = new Rectangle(_x, _y, _width, height);
        _height -= height;
        _y += height;
        return res;
    }

    public Rectangle Clone() => new(_x, _y, _width, _height);

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture,
            "Rectangle{{x={0}, y={1}, width={2}, height={3}}}", _x, _y, _width, _height);
}
