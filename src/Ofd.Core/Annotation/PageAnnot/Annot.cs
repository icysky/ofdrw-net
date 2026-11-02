using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Annotation.PageAnnot;

/// <summary>注释（15.2 图 81 表 61）。</summary>
public class Annot : OFDElement
{
    private const string PARAMETERS = "Parameters";
    private const string PARAMETER = "Parameter";

    public Annot(Dom.DomElement proxy) : base(proxy) { }

    public Annot() : base("Annot") { }

    /// <summary>【必选属性】设置 注释标识（null 抛异常）。</summary>
    public Annot SetId(ST_ID id)
    {
        if (id == null) throw new ArgumentException("注解对象（ID）不能为空");
        SetObjID(id);
        return this;
    }

    /// <summary>【必选属性】获取 注释标识，不存在 null。</summary>
    public ST_ID? GetId() => GetObjID();

    /// <summary>【必选属性】设置 注释类型。</summary>
    public Annot SetType(AnnotType type) => (Annot)AddAttribute("Type", type.ToString());

    /// <summary>【必选属性】获取 注释类型，未知抛异常。</summary>
    public AnnotType GetType() => AnnotTypeExtensions.GetInstance(AttributeValue("Type"));

    /// <summary>【必选属性】设置 注释创建者。</summary>
    public Annot SetCreator(string creator) => (Annot)AddAttribute("Creator", creator);

    /// <summary>【必选属性】获取 注释创建者，缺失抛异常（与 Java 一致）。</summary>
    public string GetCreator()
    {
        var str = AttributeValue("Creator");
        if (str == null || str.Trim().Length == 0) throw new InvalidOperationException("注释创建者（Creator）为空");
        return str;
    }

    /// <summary>【必选属性】设置 最近一次修改时间（ISO yyyy-MM-dd）。</summary>
    public Annot SetLastModDate(DateOnly lastModDate) =>
        (Annot)AddAttribute("LastModDate", lastModDate.ToString(Const.DATE_FORMAT));

    /// <summary>【必选属性】获取 最近一次修改时间，缺失抛异常。</summary>
    public DateOnly GetLastModDate()
    {
        var date = AttributeValue("LastModDate");
        if (date == null || date.Trim().Length == 0) throw new InvalidOperationException("最近一次修改的时间（LastModDate）为空");
        return DateOnly.ParseExact(date.Trim(), Const.DATE_FORMAT);
    }

    /// <summary>【可选属性】设置 注释子类型（null 删除属性）。</summary>
    public Annot SetSubtype(string? subtype)
    {
        if (subtype == null)
        {
            RemoveAttr("Subtype");
            return this;
        }
        return (Annot)AddAttribute("Subtype", subtype);
    }

    /// <summary>【可选属性】获取 注释子类型，不存在 null。</summary>
    public string? GetSubtype() => AttributeValue("Subtype");

    /// <summary>【可选属性】设置 注释是否显示（缺省 true）。</summary>
    public Annot SetVisible(bool visible) => (Annot)AddAttribute("Visible", visible ? "true" : "false");

    /// <summary>【可选属性】获取 注释是否显示，缺省 true。</summary>
    public bool GetVisible() => BoolAttr("Visible", true);

    /// <summary>【可选属性】设置 Remark 是否随页面打印（null 删除属性，缺省 true）。</summary>
    public Annot SetPrint(bool? print) => SetNullableBoolAttr("Print", print);

    /// <summary>【可选属性】获取 Remark 是否随页面打印，缺省 true。</summary>
    public bool? GetPrint() => GetNullableBoolAttr("Print") ?? true;

    /// <summary>【可选属性】设置 Remark 是否不随缩放同步（null 删除属性，缺省 false）。</summary>
    public Annot SetNoZoom(bool? noZoom) => SetNullableBoolAttr("NoZoom", noZoom);

    /// <summary>【可选属性】获取 Remark 是否不随缩放同步，缺省 false。</summary>
    public bool? GetNoZoom() => GetNullableBoolAttr("NoZoom") ?? false;

    /// <summary>【可选属性】设置 Remark 是否不随旋转（null 删除属性，缺省 false）。</summary>
    public Annot SetNoRotate(bool? noRotate) => SetNullableBoolAttr("NoRotate", noRotate);

    /// <summary>【可选属性】获取 Remark 是否不随旋转，缺省 false。</summary>
    public bool? GetNoRotate() => GetNullableBoolAttr("NoRotate") ?? false;

    /// <summary>【可选属性】设置 Remark 是否只读（null 删除属性，缺省 true）。</summary>
    public Annot SetReadOnly(bool? readOnly) => SetNullableBoolAttr("ReadOnly", readOnly);

    /// <summary>【可选属性】获取 Remark 是否只读，缺省 true。</summary>
    public bool? GetReadOnly() => GetNullableBoolAttr("ReadOnly") ?? true;

    /// <summary>【可选】设置 注释说明内容（null 删除元素）。</summary>
    public Annot SetRemark(string? remark)
    {
        if (remark == null)
        {
            RemoveOFDElemByNames("Remark");
            return this;
        }
        return (Annot)AddOFDEntity("Remark", remark);
    }

    /// <summary>【可选】获取 注释说明内容，不存在 null。</summary>
    public string? GetRemark()
    {
        var e = GetOFDElement("Remark");
        return e == null ? null : ElementText(e);
    }

    /// <summary>【可选】增加 注释参数（同名覆盖；空键名忽略）。</summary>
    public Annot AddParameter(string? name, string parameter)
    {
        if (name == null || name.Trim().Length == 0) return this;

        var parameters = GetOFDElement(PARAMETERS);
        if (parameters == null)
        {
            parameters = OFDElement.GetInstance(PARAMETERS).Proxy;
            Proxy.Add(parameters);
        }

        bool foundKey = false;
        foreach (var element in parameters.Kids.OfType<Dom.DomElement>())
        {
            if (name.Equals(element.Attr("Name"), StringComparison.Ordinal))
            {
                SetElementText(element, parameter);
                foundKey = true;
                break;
            }
        }
        if (!foundKey)
        {
            var parameterEl = new OFDSimpleTypeElement(PARAMETER, parameter);
            parameterEl.AddAttribute("Name", name);
            parameters.Add(parameterEl.Proxy);
        }
        return this;
    }

    /// <summary>【可选】获取 注释参数映射表（不存在返回空表）。</summary>
    public Dictionary<string, string> GetParameters()
    {
        var res = new Dictionary<string, string>();
        var parameters = GetOFDElement(PARAMETERS);
        if (parameters == null) return res;
        foreach (var p in MatchElements(parameters, PARAMETER))
        {
            string? name = p.Attr("Name");
            if (name == null) continue;
            res[name] = ElementText(p).Trim();
        }
        return res;
    }

    /// <summary>【必选】设置 注释静态显示效果（null 抛异常）。</summary>
    public Annot SetAppearance(Appearance appearance)
    {
        if (appearance == null) throw new ArgumentException("注释的静态显示效果（Appearance）为空");
        Set(appearance);
        return this;
    }

    /// <summary>【必选】获取 注释静态显示效果，不存在 null。</summary>
    public Appearance? GetAppearance()
    {
        var e = GetOFDElement("Appearance");
        return e == null ? null : new Appearance(e);
    }

    private Annot SetNullableBoolAttr(string name, bool? value)
    {
        if (value == null)
        {
            RemoveAttr(name);
            return this;
        }
        return (Annot)AddAttribute(name, value.Value ? "true" : "false");
    }

    private bool? GetNullableBoolAttr(string name)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return null;
        return bool.Parse(str.Trim());
    }

    private bool BoolAttr(string name, bool defaultValue)
    {
        var str = AttributeValue(name);
        return str == null || str.Trim().Length == 0 ? defaultValue : bool.Parse(str.Trim());
    }
}
