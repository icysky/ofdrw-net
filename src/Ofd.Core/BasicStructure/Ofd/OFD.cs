using Ofd.Core.Versions;

namespace Ofd.Core.BasicStructure.Ofd;

/// <summary>
/// 主入口 OFD.xml（《GB/T 33190-2016》 图 3）。
/// </summary>
public class OFD : OFDElement
{
    /// <summary>文件格式的版本号（固定值 1.2，表 3）。</summary>
    public const string VERSION = "1.2";

    /// <summary>文件格式子集类型（"OFD"，长期保存为 "OFD-A"）。</summary>
    public const string DOC_TYPE = "OFD";

    public OFD(Dom.DomElement proxy) : base(proxy) { }

    public OFD() : base("OFD")
    {
        AddAttribute("Version", VERSION);
        AddAttribute("DocType", DOC_TYPE);
    }

    /// <summary>用文件对象入口列表创建文档对象。</summary>
    public OFD(IList<DocBody> docBodies) : this()
    {
        foreach (var item in docBodies)
            if (item != null)
                Add(item);
    }

    /// <summary>用文件对象入口创建文档对象。</summary>
    public OFD(DocBody docBody) : this()
    {
        Add(docBody);
    }

    /// <summary>获取 文件格式版本号。</summary>
    public string? GetVersion() => AttributeValue("Version");

    /// <summary>设置 文件版本号。</summary>
    public OFD SetVersion(string version) => (OFD)AddAttribute("Version", version);

    /// <summary>获取 文件格式子集类型："OFD"（标准）/ "OFD-A"（GB/T 42133 长期保存）/ null（缺失）。</summary>
    public string? GetDocType() => AttributeValue("DocType");

    /// <summary>设置 文件格式子集类型（"OFD" 默认 / "OFD-A"）。</summary>
    public OFD SetDocType(string docType)
    {
        if (docType == null || docType.Length == 0)
            throw new ArgumentException("DocType 不能为空");
        return (OFD)AddAttribute("DocType", docType);
    }

    /// <summary>增加文件对象入口（可多个，一个文档包含多个版式文档）。</summary>
    public OFD AddDocBody(DocBody docBody)
    {
        Add(docBody);
        return this;
    }

    /// <summary>获取第一个文档入口。</summary>
    public DocBody? GetDocBody()
    {
        var e = GetOFDElement("DocBody");
        return e == null ? null : new DocBody(e);
    }

    /// <summary>获取指定序号的文档（从 0 起）。</summary>
    public DocBody GetDocBody(int num) => GetDocBodies()[num];

    /// <summary>获取所有文档入口。</summary>
    public List<DocBody> GetDocBodies() => GetOFDElements("DocBody", e => new DocBody(e));
}
