namespace Ofd.Tool.Merge;

/// <summary>
/// 文档页面信息（Java DocPage）：文档路径 + 页面索引（从 1 起）。
/// </summary>
public class DocPage
{
    /// <summary>文档路径。</summary>
    public string Path;

    /// <summary>页面索引（从 1 起）。</summary>
    public int Index;

    /// <summary>创建文档页面信息。</summary>
    public DocPage(string path, int index)
    {
        Path = path;
        Index = index;
    }
}
