using System.Globalization;
using Ofd.Core.BasicType;

namespace Ofd.Core.Integrity;

/// <summary>
/// 完整性保护描述文件（GMT0099 D.1，OFD 2.0 防夹带机制）：
/// 文件名固定为 "OFDEntries.xml"，放置于 OFD 根目录（构造元素名为 DocEntries）。
/// </summary>
public class OFDEntries : OFDElement
{
    public OFDEntries(Dom.DomElement proxy) : base(proxy) { }

    public OFDEntries() : base("DocEntries") { }

    /// <summary>【必选属性 OFD 2.0】设置 标识符。</summary>
    public OFDEntries SetId(string id) => (OFDEntries)AddAttribute("ID", id);

    /// <summary>【必选属性 OFD 2.0】获取 标识符，不存在 null。</summary>
    public string? GetId() => AttributeValue("ID");

    /// <summary>【可选属性 OFD 2.0】设置 创建者名称（null 删除属性）。</summary>
    public OFDEntries SetCreatorName(string? creatorName)
    {
        if (creatorName == null)
        {
            RemoveAttr("CreatorName");
            return this;
        }
        return (OFDEntries)AddAttribute("CreatorName", creatorName);
    }

    /// <summary>【可选属性 OFD 2.0】获取 创建者名称，不存在 null。</summary>
    public string? GetCreatorName() => AttributeValue("CreatorName");

    /// <summary>【可选属性 OFD 2.0】设置 模块版本（null 删除属性）。</summary>
    public OFDEntries SetVersion(string? version)
    {
        if (version == null)
        {
            RemoveAttr("Version");
            return this;
        }
        return (OFDEntries)AddAttribute("Version", version);
    }

    /// <summary>【可选属性 OFD 2.0】获取 模块版本，不存在 null。</summary>
    public string? GetVersion() => AttributeValue("Version");

    /// <summary>
    /// 【可选属性】设置 创建时间（null 删除属性）。
    /// 注：Java 上游用 DATE_FORMATTER（仅日期）写、用 DATETIME_FORMATTER 读，读写不对称；
    /// C# 统一为 ISO yyyy-MM-dd 写入，读取兼容日期与 ISO 日期时间。
    /// </summary>
    public OFDEntries SetCreationDate(DateTime? creationDate)
    {
        if (creationDate == null)
        {
            RemoveAttr("CreationDate");
            return this;
        }
        return (OFDEntries)AddAttribute("CreationDate", creationDate.Value.ToString(Const.DATE_FORMAT, CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 创建时间，不存在 null（兼容日期/日期时间格式）。</summary>
    public DateTime? GetCreationDate()
    {
        var str = AttributeValue("CreationDate");
        if (str == null || str.Trim().Length == 0) return null;
        string s = str.Trim();
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) return dt;
        throw new ArgumentException("日期时间格式不正确：" + s);
    }

    /// <summary>【必选 OFD 2.0】设置 针对防夹带文件形成的签名值路径（null 抛异常）。</summary>
    public OFDEntries SetSignedValueLoc(ST_Loc signedValueLoc)
    {
        if (signedValueLoc == null) throw new ArgumentException("防夹带文件形成的签名值(signedValueLoc)为空");
        return (OFDEntries)SetOFDEntity("SignedValueLoc", signedValueLoc.ToString());
    }

    /// <summary>【必选 OFD 2.0】获取 签名值路径，缺失抛异常。</summary>
    public ST_Loc GetSignedValueLoc()
    {
        var e = GetOFDElement("SignedValueLoc");
        if (e == null) throw new InvalidOperationException("防夹带文件形成的签名值(signedValueLoc)为空");
        return ST_Loc.GetInstance(ElementText(e).Trim())!;
    }

    /// <summary>【必选 OFD 2.0】设置 防止夹带文件列表（null 抛异常）。</summary>
    public OFDEntries SetFileList(FileList fileList)
    {
        if (fileList == null) throw new ArgumentException("防止夹带文件列表(FileList)为空");
        Set(fileList);
        return this;
    }

    /// <summary>【必选 OFD 2.0】获取 防止夹带文件列表，缺失抛异常。</summary>
    public FileList GetFileList()
    {
        var e = GetOFDElement("FileList");
        if (e == null) throw new InvalidOperationException("防止夹带文件列表(FileList)为空");
        return new FileList(e);
    }
}
