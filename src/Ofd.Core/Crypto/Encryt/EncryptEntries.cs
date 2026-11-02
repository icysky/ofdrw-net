namespace Ofd.Core.Crypto.Encryt;

/// <summary>未加密的明密文映射表文件结构（GMT0099 附录 C 图 C.3）。</summary>
public class EncryptEntries : OFDElement
{
    public EncryptEntries(Dom.DomElement proxy) : base(proxy) { }

    public EncryptEntries() : base("EncryptEntries") { }

    /// <summary>【必选属性】设置 加密操作标识（空串抛异常）。</summary>
    public EncryptEntries SetId(string id)
    {
        if (id == null || id.Length == 0) throw new ArgumentException("加密操作标识(id)为空");
        return (EncryptEntries)AddAttribute("ID", id);
    }

    /// <summary>【必选属性】获取 加密操作标识，不存在 null。</summary>
    public string? GetId() => AttributeValue("ID");

    /// <summary>【必选】增加 明密文对应关系（null 忽略）。</summary>
    public EncryptEntries AddEncryptEntry(EncryptEntry? encryptEntry)
    {
        if (encryptEntry == null) return this;
        Add(encryptEntry);
        return this;
    }

    /// <summary>【必选】增加 明密文对应关系（路径+专用密钥）。</summary>
    public EncryptEntries AddEncryptEntry(string path, string ePath, string decryptSeedLoc) =>
        AddEncryptEntry(new EncryptEntry(path, ePath, decryptSeedLoc));

    /// <summary>【必选】增加 明密文对应关系（仅路径）。</summary>
    public EncryptEntries AddEncryptEntry(string path, string ePath) =>
        AddEncryptEntry(new EncryptEntry(path, ePath));

    /// <summary>【必选】获取 明密文对应关系列表。</summary>
    public List<EncryptEntry> GetEncryptEntries() => GetOFDElements("EncryptEntry", e => new EncryptEntry(e));
}
