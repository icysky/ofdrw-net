using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription;
using Ofd.Core.PageDescription.Clips;
using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Core.Graph.PathObj;

/// <summary>图形对象（9.1 图 46 表 35）：具有一般图元的一切属性和行为特征。</summary>
public class CT_Path : CT_GraphicUnit<CT_Path>, IClipAble
{
    public CT_Path(Dom.DomElement proxy) : base(proxy) { }

    public CT_Path() : base("Path") { }

    protected CT_Path(string name) : base(name) { }

    /// <summary>构造路径对象（改名 PathObject 并设置 ID）。</summary>
    public PathObject ToObj(ST_ID id)
    {
        SetOFDName("PathObject");
        SetObjID(id);
        return new PathObject(Proxy);
    }

    /// <summary>【可选属性】设置 图形是否被勾边（null 删除属性，缺省 true）。</summary>


    /// <summary>【可选属性】获取 图形是否被勾边，缺省 true。</summary>
    public bool GetStroke() => GetBoolAttr("Stroke", true);

    /// <summary>【可选属性】设置 图形是否被填充（null 删除属性，缺省 false）。</summary>


    /// <summary>【可选属性】获取 图形是否被填充，缺省 false。</summary>
    public bool GetFill() => GetBoolAttr("Fill", false);

    /// <summary>【可选属性】设置 填充规则（null 删除属性，缺省 NonZero）。</summary>
    public CT_Path SetRule(Rule? rule)
    {
        if (rule == null)
        {
            RemoveAttr("Rule");
            return this;
        }
        return (CT_Path)AddAttribute("Rule", rule.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 填充规则，缺省 NonZero。</summary>
    public Rule GetRule() => RuleExtensions.GetInstance(AttributeValue("Rule"));

    /// <summary>【可选】设置 勾边颜色（非 StrokeColor 类型时克隆改名，与 Java 一致）。</summary>
    public CT_Path SetStrokeColor(CT_Color? strokeColor)
    {
        if (strokeColor == null) return this;
        Dom.DomElement copy = strokeColor.Proxy.Clone();
        if (strokeColor is not StrokeColor)
            copy.Name = "StrokeColor";
        RemoveOFDElemByNames("StrokeColor");
        Add(new StrokeColor(copy));
        return this;
    }

    /// <summary>【可选】获取 勾边颜色，null 表示黑色。</summary>
    public StrokeColor? GetStrokeColor()
    {
        var e = GetOFDElement("StrokeColor");
        return e == null ? null : new StrokeColor(e);
    }

    /// <summary>【可选】设置 填充颜色（非 FillColor 类型时克隆改名，与 Java 一致）。</summary>
    public CT_Path SetFillColor(CT_Color? fillColor)
    {
        if (fillColor == null) return this;
        Dom.DomElement copy = fillColor.Proxy.Clone();
        if (fillColor is not FillColor)
            copy.Name = "FillColor";
        RemoveOFDElemByNames("FillColor");
        Add(new FillColor(copy));
        return this;
    }

    /// <summary>【可选】获取 填充颜色，null 表示透明色。</summary>
    public FillColor? GetFillColor()
    {
        var e = GetOFDElement("FillColor");
        return e == null ? null : new FillColor(e);
    }

    /// <summary>【必选】设置 图形轮廓数据（null 忽略）。</summary>
    public CT_Path SetAbbreviatedData(AbbreviatedData? abbreviatedData)
    {
        if (abbreviatedData == null) return this;
        RemoveOFDElemByNames("AbbreviatedData");
        Add(abbreviatedData.Flush());
        return this;
    }

    /// <summary>【必选】获取 图形轮廓数据字符串，不存在 null。</summary>
    public string? GetAbbreviatedDataText()
    {
        var e = GetOFDElement("AbbreviatedData");
        return e == null ? null : ElementText(e);
    }

    /// <summary>【必选】获取 图形轮廓数据对象，不存在 null。</summary>
    public AbbreviatedData? GetAbbreviatedDataEle()
    {
        var e = GetOFDElement("AbbreviatedData");
        return e == null ? null : new AbbreviatedData(e);
    }

    private CT_Path SetBoolAttr(string name, bool? value)
    {
        if (value == null)
        {
            RemoveAttr(name);
            return this;
        }
        return (CT_Path)AddAttribute(name, value.Value ? "true" : "false");
    }

    private bool GetBoolAttr(string name, bool defaultValue)
    {
        var str = AttributeValue(name);
        return str == null || str.Trim().Length == 0 ? defaultValue : bool.Parse(str.Trim());
    }
}
