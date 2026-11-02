using Ofd.Core.BasicType;

namespace Ofd.Core.Graph.Tight.Method;

/// <summary>移动节点（到新的绘制点指令）。</summary>
public class Move : Command
{
    public Move(Dom.DomElement proxy) : base(proxy) { }

    public Move() : base("Move") { }

    public Move(ST_Pos point1) : this()
    {
        SetPoint1(point1);
    }

    public Move(double x, double y) : this()
    {
        SetPoint1(new ST_Pos(x, y));
    }

    /// <summary>【必选属性】设置 移动后新的当前绘制点。</summary>
    public Move SetPoint1(ST_Pos point1) => (Move)AddAttribute("Point1", point1.ToString());

    /// <summary>【必选属性】获取 移动后新的当前绘制点，不存在 null。</summary>
    public ST_Pos? GetPoint1() => ST_Pos.GetInstance(AttributeValue("Point1"));

    /// <summary>ABNF 指令 "M x y"（缺失点时输出 "0 0"，与 Java 一致）。</summary>
    public override string ToString() => "M " + (GetPoint1()?.ToString() ?? "0 0");
}
