namespace Ofd.Core.Signatures;

/// <summary>
/// 签名列表根节点（18.1 图 85 表 66）：可包含多个签名（联合发文等）；
/// 当允许下次继续签名时，该文件不包含在本次签名保护文件列表中。
/// </summary>
public class Signatures : OFDElement
{
    public Signatures(Dom.DomElement proxy) : base(proxy) { }

    public Signatures() : base("Signatures") { }

    /// <summary>
    /// 【可选元素】设置 安全标识最大值（<c>&lt;ofd:MaxSignId&gt;sNNN&lt;/ofd:MaxSignId&gt;</c>，图 85 表 66）。
    /// <para>
    /// 注：此前误实现为 <c>MaxSignID</c> 属性，与规范及 Java 实现（元素）不一致，
    /// 导致产物缺少 MaxSignId。已通过与 Java 产物对拍发现并修正。
    /// </para>
    /// </summary>
    public Signatures SetMaxSignId(string? maxSignId)
    {
        RemoveOFDElemByNames("MaxSignId");
        if (!string.IsNullOrEmpty(maxSignId)) AddOFDEntity("MaxSignId", maxSignId);
        return this;
    }

    /// <summary>【可选元素】获取 安全标识最大值，不存在 null。</summary>
    public string? GetMaxSignId() => GetOFDElementText("MaxSignId");

    /// <summary>【可选】增加 数字签名或安全签章的注册信息（null 忽略）。</summary>
    public Signatures AddSignature(Signature? signature)
    {
        if (signature == null) return this;
        Add(signature);
        return this;
    }

    /// <summary>【可选】获取 签名注册信息序列。</summary>
    public List<Signature> GetSignatures() => GetOFDElements("Signature", e => new Signature(e));
}
