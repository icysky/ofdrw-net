namespace Ofd.Core.Signatures.Sig;

/// <summary>创建签名时所用的签章组件提供者信息（18.2.1 图 86 表 67）。</summary>
public class Provider : OFDElement
{
    public Provider(Dom.DomElement proxy) : base(proxy) { }

    public Provider() : base("Provider") { }

    /// <summary>【必选属性】设置 提供者名称（空白抛异常）。</summary>
    public Provider SetProviderName(string providerName)
    {
        if (providerName == null || providerName.Trim().Length == 0)
            throw new ArgumentException("创建签名时所用的签章组件提供者信息（ProviderName）为空");
        return (Provider)AddAttribute("ProviderName", providerName);
    }

    /// <summary>【必选属性】获取 提供者名称，缺失抛异常。</summary>
    public string GetProviderName()
    {
        var str = AttributeValue("ProviderName");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("创建签名时所用的签章组件提供者信息（ProviderName）为空");
        return str;
    }

    /// <summary>【可选属性】设置 组件版本（空白删除属性）。</summary>
    public Provider SetVersion(string? version)
    {
        if (version == null || version.Trim().Length == 0)
        {
            RemoveAttr("Version");
            return this;
        }
        return (Provider)AddAttribute("Version", version);
    }

    /// <summary>【可选属性】获取 组件版本，不存在 null。</summary>
    public string? GetVersion() => AttributeValue("Version");

    /// <summary>【可选属性】设置 组件制造商（空白删除属性）。</summary>
    public Provider SetCompany(string? company)
    {
        if (company == null || company.Trim().Length == 0)
        {
            RemoveAttr("Company");
            return this;
        }
        return (Provider)AddAttribute("Company", company);
    }

    /// <summary>【可选属性】获取 组件制造商，不存在 null。</summary>
    public string? GetCompany() => AttributeValue("Company");

    /// <summary>【可选属性 OFD 2.0】设置 组件接口协议版本（null 删除属性）。</summary>
    public Provider SetProtocolVer(string? protocolVer)
    {
        if (protocolVer == null)
        {
            RemoveAttr("ProtocolVer");
            return this;
        }
        return (Provider)AddAttribute("ProtocolVer", protocolVer);
    }

    /// <summary>【可选属性 OFD 2.0】获取 组件接口协议版本，不存在 null。</summary>
    public string? GetProtocolVer() => AttributeValue("ProtocolVer");

    /// <summary>【可选 OFD 2.0】设置 组件扩展信息（Base64 编码；null 按空数组）。</summary>
    public Provider SetExtendData(byte[]? extendData) =>
        (Provider)SetOFDEntity("ExtendData", Convert.ToBase64String(extendData ?? Array.Empty<byte>()));

    /// <summary>【可选 OFD 2.0】获取 组件扩展信息（Base64 解码），不存在返回空数组。</summary>
    public byte[] GetExtendData()
    {
        var e = GetOFDElement("ExtendData");
        return e == null ? Array.Empty<byte>() : Convert.FromBase64String(ElementText(e).Trim());
    }
}
