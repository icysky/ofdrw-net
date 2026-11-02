using Ofd.Core.BasicType;

namespace Ofd.Core.Integrity;

/// <summary>GMT0099-2020 D.1 防止夹带文件列表。</summary>
public class FileList : OFDElement
{
    public FileList(Dom.DomElement proxy) : base(proxy) { }

    public FileList() : base("FileList") { }

    /// <summary>【必选】增加 防止夹带文件信息（null 忽略）。</summary>
    public FileList AddFile(File? file)
    {
        if (file == null) return this;
        Add(file);
        return this;
    }

    /// <summary>【必选】增加 防止夹带文件信息（路径字符串）。</summary>
    public FileList AddFile(string id, string fileLoc) => AddFile(new File(id, fileLoc));

    /// <summary>【必选】增加 防止夹带文件信息（ST_Loc）。</summary>
    public FileList AddFile(string id, ST_Loc fileLoc) => AddFile(new File(id, fileLoc));

    /// <summary>【必选】获取 防止夹带文件信息列表。</summary>
    public List<File> GetFiles() => GetOFDElements("File", e => new File(e));
}
