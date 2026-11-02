using Ofd.Core.BasicType;

namespace Ofd.Core.Versions;

/// <summary>版本包含的文件列表（19.2 标 71）。</summary>
public class FileList : OFDElement
{
    public FileList(Dom.DomElement proxy) : base(proxy) { }

    public FileList() : base("FileList") { }

    /// <summary>增加 文件列表文件描述（null 忽略）。</summary>
    public FileList AddFile(File? file)
    {
        if (file == null) return this;
        Add(file);
        return this;
    }

    /// <summary>增加 文件列表文件描述。</summary>
    public FileList AddFile(string id, ST_Loc fileLoc) => AddFile(new File(id, fileLoc));

    /// <summary>获取 文件列表文件描述列表。</summary>
    public List<File> GetFiles() => GetOFDElements("File", e => new File(e));
}
