using Ofd.Core.BasicType;

namespace Ofd.Core.Integrity;

/// <summary>GMT0099 防止夹带文件信息。</summary>
public class File : OFDElement
{
    public File(Dom.DomElement proxy) : base(proxy) { }

    public File() : base("File") { }

    public File(string id, string fileLoc) : this()
    {
        SetId(id);
        SetFileLoc(new ST_Loc(fileLoc));
    }

    public File(string id, ST_Loc fileLoc) : this()
    {
        SetId(id);
        SetFileLoc(fileLoc);
    }

    /// <summary>【必选属性】设置 文件标识（null 抛异常）。</summary>
    public File SetId(string id)
    {
        if (id == null) throw new ArgumentException("文件标识(ID)为空");
        return (File)AddAttribute("ID", id);
    }

    /// <summary>【必选属性】获取 文件标识，缺失抛异常。</summary>
    public string GetId()
    {
        var str = AttributeValue("ID");
        if (str == null) throw new InvalidOperationException("文件标识(ID)为空");
        return str;
    }

    /// <summary>【必选属性】设置 包内文件路径（null 抛异常）。</summary>
    public File SetFileLoc(ST_Loc fileLoc)
    {
        if (fileLoc == null) throw new ArgumentException("包内文件路径(FileLoc)为空");
        return (File)AddAttribute("FileLoc", fileLoc.ToString());
    }

    /// <summary>【必选属性】获取 包内文件路径，缺失抛异常。</summary>
    public ST_Loc GetFileLoc()
    {
        var fileLoc = AttributeValue("FileLoc");
        if (fileLoc == null) throw new InvalidOperationException("包内文件路径(FileLoc)为空");
        return ST_Loc.GetInstance(fileLoc)!;
    }
}
