using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription;
using Ofd.Core.PageDescription.Clips;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.Text.Text;

namespace Ofd.Core.Text.Text;

/// <summary>文字对象（11.2 图 59 表 45）。</summary>
public class CT_Text : CT_GraphicUnit<CT_Text>, IClipAble
{
    public CT_Text(Dom.DomElement proxy) : base(proxy) { }

    public CT_Text() : base("Text") { }

    protected CT_Text(string name) : base(name) { }

    /// <summary>获取指定 ID 的文字对象（元素名 TextObject）。</summary>
    public static CT_Text TextObject(ST_ID id)
    {
        var res = new CT_Text("TextObject");
        res.SetObjID(id);
        return res;
    }

    /// <summary>构造文字对象（改名 TextObject 并设置 ID）。</summary>
    public BasicStructure.PageObj.Layer.Block.TextObject ToObj(ST_ID id)
    {
        SetOFDName("TextObject");
        SetObjID(id);
        return new BasicStructure.PageObj.Layer.Block.TextObject(Proxy);
    }

    /// <summary>【必选属性】设置 引用资源文件中定义的字形标识。</summary>
    public CT_Text SetFont(ST_RefID font) => (CT_Text)AddAttribute("Font", font.ToString());

    /// <summary>【必选属性】设置 引用字形标识。</summary>
    public CT_Text SetFont(long refId) => SetFont(new ST_RefID(refId));

    /// <summary>【必选属性】获取 引用字形资源标识，不存在 null。</summary>
    public ST_RefID? GetFont() => ST_RefID.GetInstance(AttributeValue("Font"));

    /// <summary>【必选属性】设置 字号（单位毫米）。</summary>
    public CT_Text SetSize(double size) => (CT_Text)AddAttribute("Size", STBase.Fmt(size));

    /// <summary>【必选属性】获取 字号（单位毫米），缺失抛异常（与 Java 一致）。</summary>
    public double GetSize()
    {
        var str = AttributeValue("Size");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("字号（Size）不能为空");
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 是否勾边（null 删除属性，缺省 false）。</summary>
    public CT_Text SetStroke(bool? stroke) => SetBoolAttr("Stroke", stroke);

    /// <summary>【可选属性】获取 是否勾边，缺省 false。</summary>
    public bool GetStroke() => GetBoolAttr("Stroke", false);

    /// <summary>【可选属性】设置 是否填充（null 删除属性，缺省 true）。</summary>
    public CT_Text SetFill(bool? fill) => SetBoolAttr("Fill", fill);

    /// <summary>【可选属性】获取 是否填充，缺省 true。</summary>
    public bool GetFill() => GetBoolAttr("Fill", true);

    /// <summary>【可选属性】设置 字形水平缩放比（null 删除属性，缺省 1.0）。</summary>
    public CT_Text SetHScale(double? hScale)
    {
        if (hScale == null)
        {
            RemoveAttr("HScale");
            return this;
        }
        return (CT_Text)AddAttribute("HScale", STBase.Fmt(hScale.Value));
    }

    /// <summary>【可选属性】获取 字形水平缩放比，缺省 1.0。</summary>
    public double GetHScale() => GetDouble("HScale", 1.0d);

    /// <summary>【可选属性】设置 阅读方向（null 删除属性，缺省 0）。</summary>
    public CT_Text SetReadDirection(Direction? readDirection)
    {
        if (readDirection == null)
        {
            RemoveAttr("ReadDirection");
            return this;
        }
        return (CT_Text)AddAttribute("ReadDirection", readDirection.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 阅读方向，缺省 0。</summary>
    public Direction GetReadDirection() => DirectionExtensions.GetInstance(AttributeValue("ReadDirection"));

    /// <summary>【可选属性】设置 字符方向（null 删除属性，缺省 0）。</summary>
    public CT_Text SetCharDirection(Direction? charDirection)
    {
        if (charDirection == null)
        {
            RemoveAttr("CharDirection");
            return this;
        }
        return (CT_Text)AddAttribute("CharDirection", charDirection.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 字符方向，缺省 0。</summary>
    public Direction GetCharDirection() => DirectionExtensions.GetInstance(AttributeValue("CharDirection"));

    /// <summary>【可选属性】设置 文字粗细值（null 删除属性，缺省 400）。</summary>
    public CT_Text SetWeight(Weight? weight)
    {
        if (weight == null)
        {
            RemoveAttr("Weight");
            return this;
        }
        return (CT_Text)AddAttribute("Weight", weight.Value.ToXmlValue());
    }

    /// <summary>【可选属性】获取 文字粗细值，缺省 400。</summary>
    public Weight GetWeight() => WeightExtensions.GetInstance(AttributeValue("Weight"));

    /// <summary>【可选属性】设置 是否斜体（null 删除属性，缺省 false）。</summary>
    public CT_Text SetItalic(bool? italic) => SetBoolAttr("Italic", italic);

    /// <summary>【可选属性】获取 是否斜体，缺省 false。</summary>
    public bool GetItalic() => GetBoolAttr("Italic", false);

    /// <summary>【可选】设置 填充颜色（直接改传入对象名为 FillColor 后加入，与 Java 一致）。</summary>
    public CT_Text SetFillColor(CT_Color? fillColor)
    {
        if (fillColor == null) return this;
        fillColor.SetOFDName("FillColor");
        Add(fillColor);
        return this;
    }

    /// <summary>【可选】获取 填充颜色，null 表示黑色。</summary>
    public FillColor? GetFillColor()
    {
        var e = GetOFDElement("FillColor");
        return e == null ? null : new FillColor(e);
    }

    /// <summary>【可选】设置 勾边颜色（直接改传入对象名为 StrokeColor 后加入，与 Java 一致）。</summary>
    public CT_Text SetStrokeColor(CT_Color? strokeColor)
    {
        if (strokeColor == null) return this;
        strokeColor.SetOFDName("StrokeColor");
        Add(strokeColor);
        return this;
    }

    /// <summary>【可选】获取 勾边颜色，null 表示透明色。</summary>
    public StrokeColor? GetStrokeColor()
    {
        var e = GetOFDElement("StrokeColor");
        return e == null ? null : new StrokeColor(e);
    }

    /// <summary>【可选】增加 字符编码到字符索引的变换关系（null 忽略）。</summary>
    public CT_Text AddCGTransform(CT_CGTransform? cgTransform)
    {
        if (cgTransform == null) return this;
        Add(cgTransform);
        return this;
    }

    /// <summary>【可选】获取 字符变换关系序列。</summary>
    public List<CT_CGTransform> GetCGTransforms() => GetOFDElements("CGTransform", e => new CT_CGTransform(e));

    /// <summary>【必选】增加 文字内容（字符编码串，null 忽略）。</summary>
    public CT_Text AddTextCode(TextCode? textCode)
    {
        if (textCode == null) return this;
        Add(textCode);
        return this;
    }

    /// <summary>【必选】获取 文字内容序列。</summary>
    public List<TextCode> GetTextCodes() => GetOFDElements("TextCode", e => new TextCode(e));

    private CT_Text SetBoolAttr(string name, bool? value)
    {
        if (value == null)
        {
            RemoveAttr(name);
            return this;
        }
        return (CT_Text)AddAttribute(name, value.Value ? "true" : "false");
    }

    private bool GetBoolAttr(string name, bool defaultValue)
    {
        var str = AttributeValue(name);
        return str == null || str.Trim().Length == 0 ? defaultValue : bool.Parse(str.Trim());
    }

    private double GetDouble(string name, double defaultValue)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return defaultValue;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
