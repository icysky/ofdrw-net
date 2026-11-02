using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.Signatures.Sig;

namespace Ofd.Core.Crypto.Encryt;

/// <summary>
/// 加密描述信息（GMT0099 C.3 表 C.2）：多重加密时多个节点可通过 Relative 串联。
/// </summary>
public class CT_EncryptInfo : OFDElement
{
    public CT_EncryptInfo(Dom.DomElement proxy) : base(proxy) { }

    public CT_EncryptInfo() : base("EncryptInfo") { }

    /// <summary>【必选属性 OFD 2.0】设置 加密操作标识（空串抛异常）。</summary>
    public CT_EncryptInfo SetId(string id)
    {
        if (id == null || id.Length == 0) throw new ArgumentException("加密操作标识(id)为空");
        return (CT_EncryptInfo)AddAttribute("ID", id);
    }

    /// <summary>【必选属性 OFD 2.0】获取 加密操作标识，不存在 null。</summary>
    public string? GetId() => AttributeValue("ID");

    /// <summary>【可选属性 OFD 2.0】设置 上一次加密操作标识（null 删除属性）。</summary>
    public CT_EncryptInfo SetRelative(string? id)
    {
        if (id == null)
        {
            RemoveAttr("Relative");
            return this;
        }
        return (CT_EncryptInfo)AddAttribute("Relative", id);
    }

    /// <summary>【可选属性 OFD 2.0】获取 上一次加密操作标识，可能为 null。</summary>
    public string? GetRelative() => AttributeValue("Relative");

    /// <summary>【必选】设置 加密组件信息（null 抛异常）。</summary>
    public CT_EncryptInfo SetProvider(Provider provider)
    {
        if (provider == null) throw new ArgumentException("加密组件的相关信息（Provider）为空");
        Set(provider);
        return this;
    }

    /// <summary>【必选】获取 加密组件信息，缺失抛异常。</summary>
    public Provider GetProvider()
    {
        var e = GetOFDElement("Provider");
        if (e == null) throw new InvalidOperationException("加密组件的相关信息（Provider）为空");
        return new Provider(e);
    }

    /// <summary>【必选】设置 文档加密范围描述（null 抛异常）。</summary>
    public CT_EncryptInfo SetEncryptScope(string encryptScope)
    {
        if (encryptScope == null) throw new ArgumentException("文档加密相关内容的描述（EncryptScope）为空");
        return (CT_EncryptInfo)SetOFDEntity("EncryptScope", encryptScope);
    }

    /// <summary>【必选】获取 文档加密范围描述，缺失抛异常。</summary>
    public string GetEncryptScope()
    {
        var e = GetOFDElement("EncryptScope");
        if (e == null) throw new InvalidOperationException("文档加密相关内容的描述（EncryptScope）为空");
        return ElementText(e).Trim();
    }

    /// <summary>【可选 OFD 2.0】设置 加密操作附加描述集合（null 删除元素）。</summary>
    public CT_EncryptInfo SetParameters(Parameters? parameters)
    {
        if (parameters == null)
        {
            RemoveOFDElemByNames("Parameters");
            return this;
        }
        Set(parameters);
        return this;
    }

    /// <summary>【可选 OFD 2.0】获取 加密操作附加描述集合，可能为 null。</summary>
    public Parameters? GetParameters()
    {
        var e = GetOFDElement("Parameters");
        return e == null ? null : new Parameters(e);
    }

    /// <summary>【可选】设置 加密时间（ISO yyyy-MM-ddTHH:mm:ss；null 删除元素）。</summary>
    public CT_EncryptInfo SetEncryptDate(DateTime? encryptDate)
    {
        if (encryptDate == null)
        {
            RemoveOFDElemByNames("EncryptDate");
            return this;
        }
        return (CT_EncryptInfo)SetOFDEntity("EncryptDate", encryptDate.Value.ToString(Const.DATETIME_FORMAT, CultureInfo.InvariantCulture));
    }

    /// <summary>【可选】获取 加密时间，可能为 null。</summary>
    public DateTime? GetEncryptDate()
    {
        var str = GetOFDElementText("EncryptDate");
        if (str == null || str.Trim().Length == 0) return null;
        return DateTime.ParseExact(str.Trim(), Const.DATETIME_FORMAT, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选】获取 加密时间原始字符串。</summary>
    public string? GetEncryptDateStr() => GetOFDElementText("EncryptDate");

    /// <summary>【必选】设置 指向包内的二进制密钥描述文件（null 抛异常）。</summary>
    public CT_EncryptInfo SetDecryptSeedLoc(ST_Loc decryptSeedLoc)
    {
        if (decryptSeedLoc == null) throw new ArgumentException("指向包内的二进制密钥描述文件(decryptSeedLoc)为空");
        return (CT_EncryptInfo)SetOFDEntity("DecryptSeedLoc", decryptSeedLoc.ToString());
    }

    public CT_EncryptInfo SetDecryptSeedLoc(string decryptSeedLoc)
    {
        if (decryptSeedLoc == null) throw new ArgumentException("指向包内的二进制密钥描述文件(decryptSeedLoc)为空");
        return (CT_EncryptInfo)SetOFDEntity("DecryptSeedLoc", decryptSeedLoc);
    }

    /// <summary>【必选】获取 指向包内的二进制密钥描述文件，不存在 null。</summary>
    public ST_Loc? GetDecryptSeedLoc() => ST_Loc.GetInstance(GetOFDElementText("DecryptSeedLoc"));

    /// <summary>【必选】设置 明密文映射表（或其加密后文件）路径（null 抛异常）。</summary>
    public CT_EncryptInfo SetEntriesMapLoc(ST_Loc entriesMapLoc)
    {
        if (entriesMapLoc == null) throw new ArgumentException("明密文映射表或其加密后的文件存储的路径（entriesMapLoc）为空");
        return (CT_EncryptInfo)SetOFDEntity("EntriesMapLoc", entriesMapLoc.ToString());
    }

    public CT_EncryptInfo SetEntriesMapLoc(string entriesMapLoc)
    {
        if (entriesMapLoc == null) throw new ArgumentException("明密文映射表或其加密后的文件存储的路径（entriesMapLoc）为空");
        return (CT_EncryptInfo)SetOFDEntity("EntriesMapLoc", entriesMapLoc);
    }

    /// <summary>【必选】获取 明密文映射表路径，缺失抛异常。</summary>
    public ST_Loc GetEntriesMapLoc()
    {
        var entriesMapLoc = GetOFDElementText("EntriesMapLoc");
        if (entriesMapLoc == null) throw new InvalidOperationException("明密文映射表或其加密后的文件存储的路径（entriesMapLoc）为空");
        return ST_Loc.GetInstance(entriesMapLoc)!;
    }
}
