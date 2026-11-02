using Ofd.Core.BasicType;

namespace Ofd.Core.Graph.Tight.Method;

/// <summary>线段（图 51 线段结构）：Line 元素，属性 Point1 为结束点。</summary>
public class Line : Command
{
    public Line(Dom.DomElement proxy) : base(proxy) { }

    public Line() : base("Line") { }

    public Line(ST_Pos point1) : this()
    {
        SetPoint1(point1);
    }

    public Line(double x, double y) : this()
    {
        SetPoint1(new ST_Pos(x, y));
    }

    /// <summary>【必选属性】设置 线段的结束点。</summary>
    public Line SetPoint1(ST_Pos point1) => (Line)AddAttribute("Point1", point1.ToString());

    /// <summary>【必选属性】获取 线段的结束点，不存在 null。</summary>
    public ST_Pos? GetPoint1() => ST_Pos.GetInstance(AttributeValue("Point1"));

    /// <summary>ABNF 指令 "L x y"（缺失点时输出 "0 0"，与 Java 一致）。</summary>
    public override string ToString() => "L " + (GetPoint1()?.ToString() ?? "0 0");
}
