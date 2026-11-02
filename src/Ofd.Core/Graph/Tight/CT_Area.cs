using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.Graph.Tight.Method;

namespace Ofd.Core.Graph.Tight;

/// <summary>区域中的分路径（图 49）：起始点 + 绘制指令序列，路径闭合。</summary>
public class CT_Area : OFDElement
{
    public CT_Area(Dom.DomElement proxy) : base(proxy) { }

    public CT_Area() : base("Area") { }

    /// <summary>【必选属性】设置 起始点坐标。</summary>
    public CT_Area SetStart(ST_Pos start) => (CT_Area)AddAttribute("Start", start.ToString());

    /// <summary>【必选属性】获取 起始点坐标，不存在 null。</summary>
    public ST_Pos? GetStart() => ST_Pos.GetInstance(AttributeValue("Start"));

    /// <summary>【必选】增加 绘制指令（移动点、画线、画圆弧等）。</summary>
    public CT_Area AddCommand(Command command)
    {
        Add(command);
        return this;
    }

    /// <summary>连续绘制（等价 AddCommand）。</summary>
    public CT_Area Next(Command command) => AddCommand(command);

    /// <summary>【必选】获取 绘制指令序列（顺序决定绘制图形）。</summary>
    public List<Command> GetCommands() => Elements().Select(CommandFactory.GetInstance).ToList();
}

/// <summary>路径绘制指令工厂（Java CT_Area.getCommands 内的 switch 外置）。</summary>
public static class CommandFactory
{
    public static Command GetInstance(DomElement element)
    {
        string qName = element.Prefix.Length > 0 ? element.Prefix + ":" + element.Name : element.Name;
        return qName switch
        {
            "ofd:Move" or "Move" => new Move(element),
            "ofd:Line" or "Line" => new Line(element),
            "ofd:QuadraticBezier" or "QuadraticBezier" => new QuadraticBezier(element),
            "ofd:CubicBezier" or "CubicBezier" => new CubicBezier(element),
            "ofd:Arc" or "Arc" => new Arc(element),
            "ofd:Close" or "Close" => new Close(element),
            _ => throw new ArgumentException("未知类型：" + qName),
        };
    }
}
