using Ofd.Core.BasicType;

namespace Ofd.Core.Crypto.Encryt;

/// <summary>明密文对应关系（GMT0099 附录 C 图 C.3）。</summary>
public class EncryptEntry : OFDElement
{
    public EncryptEntry(Dom.DomElement proxy) : base(proxy) { }

    public EncryptEntry() : base("EncryptEntry") { }

    public EncryptEntry(ST_Loc path, ST_Loc ePath, ST_Loc decryptSeedLoc) : this()
    {
        SetPathA(path);
        SetEPath(ePath);
        SetDecryptSeedLoc(decryptSeedLoc);
    }

    public EncryptEntry(ST_Loc path, ST_Loc ePath) : this()
    {
        SetPathA(path);
        SetEPath(ePath);
    }

    public EncryptEntry(string path, string ePath, string decryptSeedLoc) : this()
    {
        SetPathA(path);
        SetEPath(ePath);
        SetDecryptSeedLoc(decryptSeedLoc);
    }

    public EncryptEntry(string path, string ePath) : this()
    {
        SetPathA(path);
        SetEPath(ePath);
    }

    /// <summary>【必选属性】设置 加密前包内文件绝对路径（空抛异常）。</summary>
    public EncryptEntry SetPathA(string path)
    {
        if (path == null || path.Length == 0) throw new ArgumentException("加密前包内文件的绝对路径(path)为空");
        return (EncryptEntry)AddAttribute("Path", path);
    }

    public EncryptEntry SetPathA(ST_Loc path) => SetPathA(path?.ToString() ?? throw new ArgumentException("加密前包内文件的绝对路径(path)为空"));

    /// <summary>【必选属性】获取 加密前包内文件绝对路径，不存在 null。</summary>
    public ST_Loc? GetPathA() => ST_Loc.GetInstance(AttributeValue("Path"));

    /// <summary>【必选属性】设置 加密后密文绝对路径（空抛异常）。</summary>
    public EncryptEntry SetEPath(string ePath)
    {
        if (ePath == null || ePath.Length == 0) throw new ArgumentException("加密后形成的包内密文的绝对路径(ePath)为空");
        return (EncryptEntry)AddAttribute("EPath", ePath);
    }

    public EncryptEntry SetEPath(ST_Loc ePath) => SetEPath(ePath?.ToString() ?? throw new ArgumentException("加密后形成的包内密文的绝对路径(ePath)为空"));

    /// <summary>【必选属性】获取 加密后密文绝对路径，不存在 null。</summary>
    public ST_Loc? GetEPath() => ST_Loc.GetInstance(AttributeValue("EPath"));

    /// <summary>【可选属性】设置 此项密文独有的密钥数据文件路径（null 删除属性）。</summary>
    public EncryptEntry SetDecryptSeedLoc(string? decryptSeedLoc)
    {
        if (decryptSeedLoc == null)
        {
            RemoveAttr("DecryptSeedLoc");
            return this;
        }
        return (EncryptEntry)AddAttribute("DecryptSeedLoc", decryptSeedLoc);
    }

    public EncryptEntry SetDecryptSeedLoc(ST_Loc? decryptSeedLoc) => SetDecryptSeedLoc(decryptSeedLoc?.ToString());

    /// <summary>【可选属性】获取 此项密文独有密钥数据文件路径，null 表示使用通用密钥数据。</summary>
    public ST_Loc? GetDecryptSeedLoc() => ST_Loc.GetInstance(AttributeValue("DecryptSeedLoc"));
}
