using Ofd.Core.BasicType;

namespace Ofd.Core.Versions;

/// <summary>文件列表文件描述（19.2 表 71）。</summary>
public class File : OFDElement
{
    public File(Dom.DomElement proxy) : base(proxy) { }

    public File() : base("File") { }

    public File(string id, ST_Loc loc) : this()
    {
        SetID(id).SetFile(loc);
    }

    /// <summary>设置 文件列表文件标识。</summary>
    public File SetID(string id)
    {
        if (id == null)
            throw new ArgumentException("文件列表文件标识（ID）不能为空");
        return (File)AddAttribute("ID", id);
    }

    /// <summary>获取 文件列表文件标识。</summary>
    public string GetID()
    {
        var str = AttributeValue("ID");
        if (str == null || str.Trim().Length == 0)
            throw new ArgumentException("文件列表文件标识（ID）不能为空");
        return str;
    }

    /// <summary>设置 文件列表文件描述。</summary>
    public File SetFile(ST_Loc file)
    {
        if (file == null)
            throw new ArgumentException("文件列表文件描述（File）不能为空");
        return (File)SetText(file.ToString());
    }

    /// <summary>获取 文件列表文件描述。</summary>
    public ST_Loc GetFile()
    {
        var str = GetText();
        if (str == null || str.Trim().Length == 0)
            throw new ArgumentException("文件列表文件描述（File）不能为空");
        return ST_Loc.GetInstance(str)!;
    }
}
