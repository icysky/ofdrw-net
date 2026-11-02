using Ofd.Core.BasicType;

namespace Ofd.Core.Signatures.Sig;

/// <summary>
/// 签名描述文件根节点（18.2.1 图 86 表 67）：通过对签名描述文件的保护间接保护 OFD 原文。
/// 注：与签名列表项 Signatures.Signature 同名不同包（Java 同样如此）。
/// </summary>
public class Signature : OFDElement
{
    public Signature(Dom.DomElement proxy) : base(proxy) { }

    public Signature() : base("Signature") { }

    /// <summary>【必选】设置 签名保护信息（null 抛异常）。</summary>
    public Signature SetSignedInfo(SignedInfo signedInfo)
    {
        if (signedInfo == null) throw new ArgumentException("签名要保护的原文及本次签名的相关信息（SignedInfo）为空");
        Set(signedInfo);
        return this;
    }

    /// <summary>【必选】获取 签名保护信息，缺失抛异常。</summary>
    public SignedInfo GetSignedInfo()
    {
        var e = GetOFDElement("SignedInfo");
        if (e == null) throw new InvalidOperationException("签名要保护的原文及本次签名的相关信息（SignedInfo）为空");
        return new SignedInfo(e);
    }

    /// <summary>【必选】设置 指向签名值文件（null 抛异常）。</summary>
    public Signature SetSignedValue(ST_Loc signedValue)
    {
        if (signedValue == null) throw new ArgumentException("签名值文件（SignedValue）为空");
        Set(new OFDSimpleTypeElement("SignedValue", signedValue));
        return this;
    }

    /// <summary>
    /// 【必选】获取 指向签名值文件，不存在 null。
    /// 注：Java 上游误用 ST_Loc.getInstance(Element)，C# 按语义取元素文本。
    /// </summary>
    public ST_Loc? GetSignedValue()
    {
        var e = GetOFDElement("SignedValue");
        return e == null ? null : ST_Loc.GetInstance(ElementText(e).Trim());
    }
}
