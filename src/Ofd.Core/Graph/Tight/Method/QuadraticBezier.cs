using Ofd.Core.BasicType;

namespace Ofd.Core.Graph.Tight.Method;

/// <summary>二次贝塞尔曲线（图 52）：Point1 控制点、Point2 结束点。</summary>
public class QuadraticBezier : Command
{
    public QuadraticBezier(Dom.DomElement proxy) : base(proxy) { }

    public QuadraticBezier() : base("QuadraticBezier") { }

    public QuadraticBezier(double x1, double y1, double x2, double y2)
        : this(new ST_Pos(x1, y1), new ST_Pos(x2, y2))
    {
    }

    public QuadraticBezier(ST_Pos point1, ST_Pos point2) : this()
    {
        SetPoint1(point1).SetPoint2(point2);
    }

    /// <summary>【必选属性】设置 控制点。</summary>
    public QuadraticBezier SetPoint1(ST_Pos point1) => (QuadraticBezier)AddAttribute("Point1", point1.ToString());

    /// <summary>【必选属性】获取 控制点，不存在 null。</summary>
    public ST_Pos? GetPoint1() => ST_Pos.GetInstance(AttributeValue("Point1"));

    /// <summary>【必选属性】设置 结束点。</summary>
    public QuadraticBezier SetPoint2(ST_Pos point2) => (QuadraticBezier)AddAttribute("Point2", point2.ToString());

    /// <summary>【必选属性】获取 结束点，不存在 null。</summary>
    public ST_Pos? GetPoint2() => ST_Pos.GetInstance(AttributeValue("Point2"));

    /// <summary>ABNF 指令 "Q x1 y1 x2 y2"（缺失点以 "0 0" 兜底）。</summary>
    public override string ToString() =>
        "Q " + (GetPoint1()?.ToString() ?? "0 0") + " " + (GetPoint2()?.ToString() ?? "0 0");
}
