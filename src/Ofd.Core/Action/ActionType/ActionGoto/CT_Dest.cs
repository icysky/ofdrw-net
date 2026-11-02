using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Action.ActionType.ActionGoto;

/// <summary>目标区域（图 75 目标区域结构）。</summary>
public class CT_Dest : OFDElement, IOFDGotoTarget
{
    public CT_Dest(Dom.DomElement proxy) : base(proxy) { }

    public CT_Dest() : base("Dest") { }

    /// <summary>【必选属性】设置 目标区域的描述方法。</summary>
    public CT_Dest SetType(DestType type) => (CT_Dest)AddAttribute("Type", type.ToString());

    /// <summary>【必选属性】获取 目标区域的描述方法，未知抛异常。</summary>
    public DestType GetType() => DestTypeExtensions.GetInstance(AttributeValue("Type"));

    /// <summary>【必选属性】设置 引用跳转目标页面的标识。</summary>
    public CT_Dest SetPageID(ST_RefID pageId) => (CT_Dest)AddAttribute("PageID", pageId.ToString());

    /// <summary>【必选属性】获取 引用跳转目标页面的标识，不存在 null。</summary>
    public ST_RefID? GetPageID() => ST_RefID.GetInstance(AttributeValue("PageID"));

    /// <summary>【可选】设置 目标区域左上角 x 坐标（默认 0）。</summary>
    public CT_Dest SetLeft(double left) => (CT_Dest)SetOFDEntity("Left", STBase.Fmt(left));

    /// <summary>【可选】获取 目标区域左上角 x 坐标，缺省 0。</summary>
    public double GetLeft() => GetDouble("Left");

    /// <summary>【可选】设置 目标区域右上角 x 坐标（默认 0）。</summary>
    public CT_Dest SetRight(double right) => (CT_Dest)SetOFDEntity("Right", STBase.Fmt(right));

    /// <summary>【可选】获取 目标区域右上角 x 坐标，缺省 0。</summary>
    public double GetRight() => GetDouble("Right");

    /// <summary>【可选】设置 目标区域左上角 y 坐标（默认 0）。</summary>
    public CT_Dest SetTop(double top) => (CT_Dest)SetOFDEntity("Top", STBase.Fmt(top));

    /// <summary>【可选】获取 目标区域左上角 y 坐标，缺省 0。</summary>
    public double GetTop() => GetDouble("Top");

    /// <summary>【可选】设置 目标区域右下角 y 坐标（默认 0）。</summary>
    public CT_Dest SetBottom(double bottom) => (CT_Dest)SetOFDEntity("Bottom", STBase.Fmt(bottom));

    /// <summary>【可选】获取 目标区域右下角 y 坐标，缺省 0。</summary>
    public double GetBottom() => GetDouble("Bottom");

    /// <summary>【可选】设置 目标区域页面缩放比例（0 或不出现按当前缩放，取值 [0.1, 64.0]）。</summary>
    public CT_Dest SetZoom(double zoom) => (CT_Dest)SetOFDEntity("Zoom", STBase.Fmt(zoom));

    /// <summary>【可选】获取 目标区域页面缩放比例，缺省 0。</summary>
    public double GetZoom() => GetDouble("Zoom");

    private double GetDouble(string name)
    {
        var str = GetOFDElementText(name);
        if (str == null || str.Trim().Length == 0) return 0d;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
