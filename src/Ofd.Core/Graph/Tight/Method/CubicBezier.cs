using Ofd.Core.BasicType;

namespace Ofd.Core.Graph.Tight.Method;

/// <summary>三次贝塞尔曲线（图 53）：Point1/Point2 控制点、Point3 结束点。</summary>
public class CubicBezier : Command
{
    public CubicBezier(Dom.DomElement proxy) : base(proxy) { }

    public CubicBezier() : base("CubicBezier") { }

    public CubicBezier(double x1, double y1, double x2, double y2, double x3, double y3)
        : this(new ST_Pos(x1, y1), new ST_Pos(x2, y2), new ST_Pos(x3, y3))
    {
    }

    public CubicBezier(ST_Pos point1, ST_Pos point2, ST_Pos point3) : this()
    {
        SetPoint1(point1).SetPoint2(point2).SetPoint3(point3);
    }

    /// <summary>【必选属性】设置 第一个控制点。</summary>
    public CubicBezier SetPoint1(ST_Pos point1) => (CubicBezier)AddAttribute("Point1", point1.ToString());

    /// <summary>【必选属性】获取 第一个控制点，不存在 null。</summary>
    public ST_Pos? GetPoint1() => ST_Pos.GetInstance(AttributeValue("Point1"));

    /// <summary>【必选属性】设置 第二个控制点。</summary>
    public CubicBezier SetPoint2(ST_Pos point2) => (CubicBezier)AddAttribute("Point2", point2.ToString());

    /// <summary>【必选属性】获取 第二个控制点，不存在 null。</summary>
    public ST_Pos? GetPoint2() => ST_Pos.GetInstance(AttributeValue("Point2"));

    /// <summary>【必选属性】设置 结束点（下一路径起始点）。</summary>
    public CubicBezier SetPoint3(ST_Pos point3) => (CubicBezier)AddAttribute("Point3", point3.ToString());

    /// <summary>【必选属性】获取 结束点，不存在 null。</summary>
    public ST_Pos? GetPoint3() => ST_Pos.GetInstance(AttributeValue("Point3"));

    /// <summary>ABNF 指令 "B x1 y1 x2 y2 x3 y3"（缺失点以 "0 0" 兜底）。</summary>
    public override string ToString() =>
        "B " + (GetPoint1()?.ToString() ?? "0 0") + " " +
        (GetPoint2()?.ToString() ?? "0 0") + " " +
        (GetPoint3()?.ToString() ?? "0 0");
}
