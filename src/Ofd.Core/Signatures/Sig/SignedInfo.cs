using Ofd.Core.Signatures.Appearance;
using Ofd.Core.Signatures.Range;

namespace Ofd.Core.Signatures.Sig;

/// <summary>
/// 签名要保护的原文及本次签名相关信息（18.2.1 图 86 表 67，OFD 2.0 GMT0099 B.2）。
/// </summary>
public class SignedInfo : OFDElement
{
    public SignedInfo(Dom.DomElement proxy) : base(proxy) { }

    public SignedInfo() : base("SignedInfo") { }

    /// <summary>【必选】设置 签章组件提供者信息（null 抛异常）。</summary>
    public SignedInfo SetProvider(Provider provider)
    {
        if (provider == null) throw new ArgumentException("创建签名时所用的签章组件提供者信息（Provider）为空");
        Set(provider);
        return this;
    }

    /// <summary>【必选】获取 签章组件提供者信息，不存在 null。</summary>
    public Provider? GetProvider()
    {
        var e = GetOFDElement("Provider");
        return e == null ? null : new Provider(e);
    }

    /// <summary>【可选】设置 签名方法（算法代码）。</summary>
    public SignedInfo SetSignatureMethod(string? signatureMethod) => (SignedInfo)SetOFDEntity("SignatureMethod", signatureMethod);

    /// <summary>
    /// 【可选】设置 签名方法（ASN.1 OID 字符串）。
    /// 注：Java 的 ASN1ObjectIdentifier 重载依赖 BouncyCastle，C# Core 保持零依赖，请传入 OID 字符串。
    /// </summary>
    public SignedInfo SetSignatureMethodOid(string oid) => SetSignatureMethod(oid);

    /// <summary>【可选】获取 签名方法，不存在 null。</summary>
    public string? GetSignatureMethod() => GetOFDElementText("SignatureMethod");

    /// <summary>【可选】设置 签名时间（由安全模块返回）。</summary>
    public SignedInfo SetSignatureDateTime(string? signatureDateTime) =>
        (SignedInfo)SetOFDEntity("SignatureDateTime", signatureDateTime);

    /// <summary>【可选】获取 签名时间，不存在 null。</summary>
    public string? GetSignatureDateTime() => GetOFDElementText("SignatureDateTime");

    /// <summary>【可选 OFD 2.0】设置 扩展签名属性集（null 删除元素）。</summary>
    public SignedInfo SetParameters(Parameters? parameters)
    {
        if (parameters == null)
        {
            RemoveOFDElemByNames("Parameters");
            return this;
        }
        Set(parameters);
        return this;
    }

    /// <summary>【可选 OFD 2.0】获取 扩展签名属性集，不存在 null。</summary>
    public Parameters? GetParameters()
    {
        var e = GetOFDElement("Parameters");
        return e == null ? null : new Parameters(e);
    }

    /// <summary>【必选】设置 包内文件摘要记录列表（null 抛异常）。</summary>
    public SignedInfo SetReferences(References references)
    {
        if (references == null) throw new ArgumentException("包内文件计算所得的摘要记录列表（References）为空");
        Set(references);
        return this;
    }

    /// <summary>【必选】获取 包内文件摘要记录列表，缺失抛异常。</summary>
    public References GetReferences()
    {
        var e = GetOFDElement("References");
        if (e == null) throw new InvalidOperationException("包内文件计算所得的摘要记录列表（References）为空");
        return new References(e);
    }

    /// <summary>【可选】增加 本签名关联的外观（可多次；null 忽略）。</summary>
    public SignedInfo AddStampAnnot(StampAnnot? stampAnnot)
    {
        if (stampAnnot == null) return this;
        Add(stampAnnot);
        return this;
    }

    /// <summary>【可选】获取 本签名关联的外观序列。</summary>
    public List<StampAnnot> GetStampAnnots() => GetOFDElements("StampAnnot", e => new StampAnnot(e));

    /// <summary>【可选】设置 电子印章信息（null 删除元素）。</summary>
    public SignedInfo SetSeal(Seal? seal)
    {
        if (seal == null)
        {
            RemoveOFDElemByNames("Seal");
            return this;
        }
        Set(seal);
        return this;
    }

    /// <summary>【可选】获取 电子印章信息，不存在 null。</summary>
    public Seal? GetSeal()
    {
        var e = GetOFDElement("Seal");
        return e == null ? null : new Seal(e);
    }
}
