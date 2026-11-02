namespace Ofd.Core.Crypto.Encryt;

/// <summary>
/// 加密组件的相关信息（GMT0099 C.3 表 C.2）：继承签名 Provider，
/// 但组件名属性为 "Name"（签名侧为 "ProviderName"）。
/// </summary>
public class Provider : global::Ofd.Core.Signatures.Sig.Provider
{
    public Provider(Dom.DomElement proxy) : base(proxy) { }

    public Provider() : base() { }

    /// <summary>【必选属性】设置 加密组件名称（写入 "Name" 属性；null 抛异常）。</summary>
    public new Provider SetProviderName(string name)
    {
        if (name == null) throw new ArgumentException("加密组件名称（name）为空");
        AddAttribute("Name", name);
        return this;
    }

    /// <summary>【必选属性】获取 加密组件名称（读 "Name" 属性），缺失抛异常。</summary>
    public new string GetProviderName()
    {
        var str = AttributeValue("Name");
        if (str == null || str.Trim().Length == 0)
            throw new InvalidOperationException("加密组件名称（Name）为空");
        return str;
    }
}
