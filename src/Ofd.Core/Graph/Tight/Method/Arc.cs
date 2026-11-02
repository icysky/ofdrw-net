using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Graph.Tight.Method;

/// <summary>圆弧（图 56）：长/短轴、旋转角、大弧标记、顺时针标记、结束点。</summary>
public class Arc : Command
{
    public Arc(Dom.DomElement proxy) : base(proxy) { }

    public Arc() : base("Arc") { }

    public Arc(double rx, double ry, double angle, double large, double sweep, double x, double y) : this()
    {
        SetEllipseSize(rx, ry);
        SetRotationAngle(angle);
        SetLargeArc(large != 0);
        SetSweepDirection(sweep != 0);
        SetEndPoint(x, y);
    }

    /// <summary>【必选属性】设置 弧线方向是否顺时针。</summary>
    public Arc SetSweepDirection(bool sweepDirection) => (Arc)AddAttribute("SweepDirection", sweepDirection ? "true" : "false");

    /// <summary>【必选属性】获取 弧线方向是否顺时针（缺失按 false）。</summary>
    public bool GetSweepDirection() => bool.Parse(AttributeValue("SweepDirection")?.Trim() ?? "false");

    /// <summary>【必选属性】设置 是否是大圆弧（对应角度大于 180°）。</summary>
    public Arc SetLargeArc(bool largeArc) => (Arc)AddAttribute("LargeArc", largeArc ? "true" : "false");

    /// <summary>【必选属性】获取 是否是大圆弧（缺失按 false）。</summary>
    public bool GetLargeArc() => bool.Parse(AttributeValue("LargeArc")?.Trim() ?? "false");

    /// <summary>【必选属性】设置 椭圆旋转角度（>360° 取模）。</summary>
    public Arc SetRotationAngle(double rotationAngle)
    {
        rotationAngle %= 360.0d;
        return (Arc)AddAttribute("RotationAngle", STBase.Fmt(rotationAngle));
    }

    /// <summary>【必选属性】获取 椭圆旋转角度（取模 360），缺失抛异常（与 Java 一致）。</summary>
    public double GetRotationAngle()
    {
        double res = double.Parse(AttributeValue("RotationAngle")?.Trim()
            ?? throw new InvalidOperationException("RotationAngle 不存在"), NumberStyles.Float, CultureInfo.InvariantCulture);
        return res % 360.0d;
    }

    /// <summary>【必选属性】设置 长短轴数组（形如 "200 100"）。</summary>
    public Arc SetEllipseSize(ST_Array ellipseSize) => (Arc)AddAttribute("EllipseSize", ellipseSize.ToString());

    /// <summary>【必选属性】设置 长短轴（数值形式）。</summary>
    public Arc SetEllipseSize(params double[] sizes) => SetEllipseSize(new ST_Array(sizes.Cast<object?>().ToArray()));

    /// <summary>【必选属性】获取 长短轴，不存在 null。</summary>
    public ST_Array? GetEllipseSize() => ST_Array.GetInstance(AttributeValue("EllipseSize"));

    /// <summary>【必选属性】设置 圆弧结束点（下一路径起点）。</summary>
    public Arc SetEndPoint(ST_Pos endPoint) => (Arc)AddAttribute("EndPoint", endPoint.ToString());

    /// <summary>【必选属性】设置 圆弧结束点坐标。</summary>
    public Arc SetEndPoint(double x, double y) => SetEndPoint(new ST_Pos(x, y));

    /// <summary>【必选属性】获取 圆弧结束点，不存在 null。</summary>
    public ST_Pos? GetEndPoint() => ST_Pos.GetInstance(AttributeValue("EndPoint"));

    /// <summary>
    /// ABNF 指令 "A rx ry rot large sweep x y"
    /// （旋转角解析失败时输出 0，与 Java catch NumberFormatException 行为一致）。
    /// </summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder("A ");
        var ellipseSize = GetEllipseSize();
        sb.Append(ellipseSize != null ? ellipseSize.ToString() : "0 0").Append(' ');
        try
        {
            sb.Append(GetRotationAngle().ToString(CultureInfo.InvariantCulture)).Append(' ');
        }
        catch (Exception)
        {
            sb.Append("0 ");
        }
        sb.Append(GetLargeArc() ? "1 " : "0 ");
        sb.Append(GetSweepDirection() ? "1 " : "0 ");
        sb.Append(GetEndPoint()?.ToString() ?? "0 0");
        return sb.ToString();
    }
}
