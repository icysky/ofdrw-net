using Ofd.Core.BasicType;

namespace Ofd.Core.Extensions;

/// <summary>扩展信息节点（第 17 章 图 83 表 64）。</summary>
public class CT_Extension : OFDElement
{
    public CT_Extension(Dom.DomElement proxy) : base(proxy) { }

    public CT_Extension() : base("Extension") { }

    /// <summary>【必选属性】设置 扩展应用程序名称（空白抛异常）。</summary>
    public CT_Extension SetAppName(string appName)
    {
        if (appName == null || appName.Trim().Length == 0)
            throw new ArgumentException("用于生成或解释该自定义对象数据的扩展应用程序名称（AppName）不能为空");
        return (CT_Extension)AddAttribute("AppName", appName);
    }

    /// <summary>【必选属性】获取 扩展应用程序名称，缺失抛异常。</summary>
    public string GetAppName()
    {
        var str = AttributeValue("AppName");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("用于生成或解释该自定义对象数据的扩展应用程序名称（AppName）不能为空");
        return str;
    }

    /// <summary>【可选属性】设置 软件厂商标识（空白删除属性）。</summary>
    public CT_Extension SetCompany(string? company)
    {
        if (company == null || company.Trim().Length == 0)
        {
            RemoveAttr("Company");
            return this;
        }
        return (CT_Extension)AddAttribute("Company", company);
    }

    /// <summary>【可选属性】获取 软件厂商标识，不存在 null。</summary>
    public string? GetCompany() => AttributeValue("Company");

    /// <summary>【可选属性】设置 软件版本（空白删除属性）。</summary>
    public CT_Extension SetAppVersion(string? appVersion)
    {
        if (appVersion == null || appVersion.Trim().Length == 0)
        {
            RemoveAttr("AppVersion");
            return this;
        }
        return (CT_Extension)AddAttribute("AppVersion", appVersion);
    }

    /// <summary>【可选属性】获取 软件版本，不存在 null。</summary>
    public string? GetAppVersion() => AttributeValue("AppVersion");

    /// <summary>【可选属性】设置 形成日期（ISO yyyy-MM-dd；null 删除属性）。</summary>
    public CT_Extension SetDate(DateOnly? date)
    {
        if (date == null)
        {
            RemoveAttr("Date");
            return this;
        }
        return (CT_Extension)AddAttribute("Date", date.Value.ToString(Const.DATE_FORMAT));
    }

    /// <summary>【可选属性】获取 形成日期，不存在 null。</summary>
    public DateOnly? GetDate()
    {
        var str = AttributeValue("Date");
        if (str == null || str.Trim().Length == 0) return null;
        return DateOnly.ParseExact(str.Trim(), Const.DATE_FORMAT);
    }

    /// <summary>【可选属性】设置 引用扩展项针对的文档项目标识（null 删除属性）。</summary>
    public CT_Extension SetRefId(ST_RefID? refId)
    {
        if (refId == null)
        {
            RemoveAttr("RefId");
            return this;
        }
        return (CT_Extension)AddAttribute("RefId", refId.ToString());
    }

    /// <summary>【可选属性】获取 引用文档项目标识，不存在 null。</summary>
    public ST_RefID? GetRefId() => ST_RefID.GetInstance(AttributeValue("RefId"));

    /// <summary>【必选】增加 属性（null 忽略）。</summary>
    public CT_Extension AddProperty(Property? property)
    {
        if (property == null) return this;
        Add(property);
        return this;
    }

    /// <summary>【必选】获取 属性列表。</summary>
    public List<Property> GetPropertys() => GetOFDElements("Property", e => new Property(e));

    /// <summary>【必选】增加 扩展数据文件位置（null 忽略）。</summary>
    public CT_Extension AddExtendData(ST_Loc? extendData)
    {
        if (extendData == null) return this;
        AddOFDEntity("ExtendData", extendData);
        return this;
    }

    /// <summary>【必选】获取 扩展数据文件位置序列。</summary>
    public List<ST_Loc?> GetExtendDatas() => GetOFDElements("ExtendData", ElementText).Select(ST_Loc.GetInstance).ToList();

    /// <summary>【必选】增加 扩展复杂属性（xs:anyType；null 忽略）。</summary>
    public CT_Extension AddData(Dom.DomElement? data)
    {
        if (data == null) return this;
        Proxy.Add(data);
        return this;
    }

    /// <summary>【必选】获取 扩展复杂属性序列。</summary>
    public List<Dom.DomElement> GetDatas() => MatchElements(Proxy, "Data");
}
