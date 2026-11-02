using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Tool.Merge;

/// <summary>
/// OFD 文档页面删除器（Java OFDPageDeleter）：
/// 仅删除文档树中的页面节点，不删除资源与页面对象 Content.xml。
/// </summary>
public class OFDPageDeleter : IDisposable
{
    private readonly OFDDir _ofdDir;
    private readonly OFD _ofd;
    private readonly Document _ofdRoot;
    private readonly OFDReader _reader;
    private readonly string _outPath;

    public OFDPageDeleter(string src, string? outPath)
    {
        if (src == null || !File.Exists(src)) throw new ArgumentException("OFD文件不存在");
        if (outPath == null) throw new ArgumentException("输出路径（out）为空");

        _reader = new OFDReader(src);
        _outPath = outPath;
        _ofdDir = _reader.GetOFDDir();
        _ofd = _ofdDir.GetOfd();

        var rl = _reader.GetResourceLocator();
        var docBody = _ofd.GetDocBody() ?? throw new InvalidOperationException("OFD 缺少 DocBody");
        var docRoot = docBody.GetDocRoot() ?? throw new InvalidOperationException("OFD 缺少 DocRoot");

        _ofdRoot = rl.Get(docRoot, e => new Document(e));
    }

    /// <summary>删除指定索引的页面（索引从 0 起）。</summary>
    public OFDPageDeleter Delete(params int[] indexes)
    {
        if (indexes == null || indexes.Length == 0) return this;

        var pages = _ofdRoot.GetPages();
        if (pages == null) return this;

        var tbd = new List<Page>();
        foreach (int index in indexes)
        {
            var list = pages.GetPages();
            for (int i = 0; i < list.Count; i++)
            {
                if (i == index) tbd.Add(list[i]);
            }
        }

        foreach (var page in tbd) pages.Remove(page);
        return this;
    }

    /// <summary>保存删除后的文档并关闭。</summary>
    public void Close()
    {
        var docBody = _ofd.GetDocBody();
        docBody?.GetDocInfo()?.SetModDate(DateOnly.FromDateTime(DateTime.Now));

        using (var fs = File.Create(_outPath)) _ofdDir.Jar(fs);

        _reader.Dispose();
    }

    public void Dispose() => Close();
}
