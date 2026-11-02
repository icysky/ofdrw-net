using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.PageDescription.DrawParam;

/// <summary>
/// 绘制参数（8.2 图 22）：控制绘制渲染效果的修饰参数集合，可被共享与继承。
/// </summary>
public class CT_DrawParam : OFDElement
{
    public CT_DrawParam(Dom.DomElement proxy) : base(proxy) { }

    public CT_DrawParam() : base("DrawParam") { }

    public ST_ID? GetID() => GetObjID();

    public CT_DrawParam SetID(ST_ID id) => (CT_DrawParam)SetObjID(id);

    /// <summary>【可选属性】设置 基础绘制参数引用。</summary>
    public CT_DrawParam SetRelative(ST_RefID relative) => (CT_DrawParam)AddAttribute("Relative", relative.ToString());

    /// <summary>【可选属性】获取 基础绘制参数引用，不存在 null。</summary>
    public ST_RefID? GetRelative() => ST_RefID.GetInstance(AttributeValue("Relative"));

    /// <summary>【可选属性】设置 线宽（非负；null 删属性）。</summary>
    public CT_DrawParam SetLineWidth(double? lineWidth)
    {
        if (lineWidth == null)
        {
            RemoveAttr("LineWidth");
            return this;
        }
        if (lineWidth < 0)
            throw new ArgumentException("线宽必须是非负浮点数");
        return (CT_DrawParam)AddAttribute("LineWidth", STBase.Fmt(lineWidth.Value));
    }

    /// <summary>【可选属性】获取 线宽，缺省 0.353mm。</summary>
    public double GetLineWidth() => GetDouble("LineWidth", 0.353d);

    /// <summary>【可选属性】设置 线条连接样式（null 删属性）。</summary>
    public CT_DrawParam SetJoin(LineJoinType? join)
    {
        if (join == null)
        {
            RemoveAttr("Join");
            return this;
        }
        return (CT_DrawParam)AddAttribute("Join", join.ToString());
    }

    /// <summary>【可选属性】获取 线条连接样式，缺省 Miter。</summary>
    public LineJoinType GetJoin() => LineJoinTypeExtensions.GetInstance(AttributeValue("Join"));

    /// <summary>【可选属性】设置 线端点样式（null 删属性）。</summary>
    public CT_DrawParam SetCap(LineCapType? cap)
    {
        if (cap == null)
        {
            RemoveAttr("Cap");
            return this;
        }
        return (CT_DrawParam)AddAttribute("Cap", cap.ToString());
    }

    /// <summary>【可选属性】获取 线端点样式，缺省 Butt。</summary>
    public LineCapType GetCap() => LineCapTypeExtensions.GetInstance(AttributeValue("Cap"));

    /// <summary>【可选属性】设置 虚线开始位置（null 删属性）。</summary>
    public CT_DrawParam SetDashOffset(double? dashOffset)
    {
        if (dashOffset == null)
        {
            RemoveAttr("DashOffset");
            return this;
        }
        return (CT_DrawParam)AddAttribute("DashOffset", STBase.Fmt(dashOffset.Value));
    }

    /// <summary>【可选属性】获取 虚线开始位置，缺省 0。</summary>
    public double GetDashOffset() => GetDouble("DashOffset", 0d);

    /// <summary>【可选属性】设置 虚线重复样式（null 删属性）。</summary>
    public CT_DrawParam SetDashPattern(ST_Array? dashPattern)
    {
        if (dashPattern == null)
        {
            RemoveAttr("DashPattern");
            return this;
        }
        return (CT_DrawParam)AddAttribute("DashPattern", dashPattern.ToString());
    }

    /// <summary>【可选属性】获取 虚线重复样式，不存在 null。</summary>
    public ST_Array? GetDashPattern() => ST_Array.GetInstance(AttributeValue("DashPattern"));

    /// <summary>【可选属性】设置 Join 截断值（null 删属性）。</summary>
    public CT_DrawParam SetMiterLimit(double? miterLimit)
    {
        if (miterLimit == null)
        {
            RemoveAttr("MiterLimit");
            return this;
        }
        return (CT_DrawParam)AddAttribute("MiterLimit", STBase.Fmt(miterLimit.Value));
    }

    /// <summary>【可选属性】获取 Join 截断值，缺省 3.528。</summary>
    public double GetMiterLimit() => GetDouble("MiterLimit", 3.528d);

    /// <summary>【可选】设置 填充颜色（克隆后改名 FillColor；null 删除元素）。</summary>
    public CT_DrawParam SetFillColor(CT_Color? fillColor)
    {
        if (fillColor == null)
        {
            RemoveOFDElemByNames("FillColor");
            return this;
        }
        var color = new FillColor(fillColor.Proxy.Clone());
        color.SetOFDName("FillColor");
        Set(color);
        return this;
    }

    /// <summary>【可选】获取 填充颜色，不存在 null。</summary>
    public FillColor? GetFillColor()
    {
        var e = GetOFDElement("FillColor");
        return e == null ? null : new FillColor(e);
    }

    /// <summary>【可选】设置 勾边颜色（克隆后改名 StrokeColor；null 删除元素）。</summary>
    public CT_DrawParam SetStrokeColor(CT_Color? strokeColor)
    {
        if (strokeColor == null)
        {
            RemoveOFDElemByNames("StrokeColor");
            return this;
        }
        var color = new StrokeColor(strokeColor.Proxy.Clone());
        color.SetOFDName("StrokeColor");
        Set(color);
        return this;
    }

    /// <summary>【可选】获取 勾边颜色，不存在 null。</summary>
    public StrokeColor? GetStrokeColor()
    {
        var e = GetOFDElement("StrokeColor");
        return e == null ? null : new StrokeColor(e);
    }

    /// <summary>克隆（深拷贝，父引用置空，与 Java clone 一致）。</summary>
    public CT_DrawParam Clone() => new(Proxy.Clone());

    private double GetDouble(string name, double defaultValue)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return defaultValue;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
