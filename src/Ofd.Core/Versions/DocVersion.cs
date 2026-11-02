using Ofd.Core.BasicType;

namespace Ofd.Core.Versions;

/// <summary>
/// 版本（版本信息在独立的文件中描述；版本定义结构列出该版本所需的所有文件）。
/// 19.2 版本 图 90 表 71。
/// </summary>
public class DocVersion : OFDElement
{
    public DocVersion(Dom.DomElement proxy) : base(proxy) { }

    public DocVersion() : base("DocVersion") { }

    public DocVersion(string id, FileList fileList) : this()
    {
        SetID(id).SetFileList(fileList);
    }

    /// <summary>设置 版本标识符。</summary>
    public DocVersion SetID(string id)
    {
        if (id == null || id.Trim().Length == 0)
            throw new ArgumentException("版本标识符（ID）不能为空");
        return (DocVersion)AddAttribute("ID", id);
    }

    /// <summary>获取 版本标识符。</summary>
    public string GetID()
    {
        var str = AttributeValue("ID");
        if (str == null || str.Trim().Length == 0)
            throw new ArgumentException("版本标识符（ID）不能为空");
        return str;
    }

    /// <summary>
    /// 设置 该文件适用的格式版本（null 删除属性）。
    /// 注：Java 上游误写 removeAttr(version)（null），已修正为删除 "Version" 属性。
    /// </summary>
    public DocVersion SetVersion(string? version)
    {
        if (version == null)
        {
            RemoveAttr("Version");
            return this;
        }
        return (DocVersion)AddAttribute("Version", version);
    }

    /// <summary>获取 该文件适用的格式版本，null 表示不存在。</summary>
    public string? GetVersion() => AttributeValue("Version");

    /// <summary>设置 版本名称（null 删除属性）。</summary>
    public DocVersion SetDocVersionName(string? name)
    {
        if (name == null)
        {
            RemoveAttr("Name");
            return this;
        }
        return (DocVersion)AddAttribute("Name", name);
    }

    /// <summary>获取 版本名称，null 表示不存在。</summary>
    public string? GetDocVersionName() => AttributeValue("Name");

    /// <summary>设置 创建时间（null 删除属性；写入 ISO yyyy-MM-dd）。</summary>
    public DocVersion SetCreationDate(DateOnly? creationDate)
    {
        if (creationDate == null)
        {
            RemoveAttr("CreationDate");
            return this;
        }
        return (DocVersion)AddAttribute("CreationDate", creationDate.Value.ToString(Const.DATE_FORMAT));
    }

    /// <summary>
    /// 获取 创建时间。
    /// 注：Java 上游读子元素文本（与 setter 写属性不一致），已修正为读属性。
    /// </summary>
    public DateOnly? GetCreationDate()
    {
        var dateStr = AttributeValue("CreationDate");
        return dateStr == null ? null : DateOnly.ParseExact(dateStr, Const.DATE_FORMAT);
    }

    /// <summary>设置 版本包含的文件列表。</summary>
    public DocVersion SetFileList(FileList fileList)
    {
        if (fileList == null)
            throw new ArgumentException("版本包含的文件列表（FileList）不能为空");
        Add(fileList);
        return this;
    }

    /// <summary>获取 版本包含的文件列表。</summary>
    public FileList? GetFileList()
    {
        var e = GetOFDElement("FileList");
        return e == null ? null : new FileList(e);
    }

    /// <summary>设置 该版本的入口文件。</summary>
    public DocVersion SetDocRoot(ST_Loc docRoot)
    {
        if (docRoot == null)
            throw new ArgumentException("该版本的入口文件（DocRoot）不能为空");
        return (DocVersion)AddOFDEntity("DocRoot", docRoot);
    }

    /// <summary>获取 该版本的入口文件。</summary>
    public ST_Loc? GetDocRoot()
    {
        var e = GetOFDElement("DocRoot");
        return e == null ? null : new ST_Loc(ElementText(e).Trim());
    }
}
