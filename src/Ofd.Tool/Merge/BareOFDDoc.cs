using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.Ofd.DocInfo;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Layout;
using Ofd.Layout.Engine;
using Ofd.Pkg;
using Ofd.Pkg.Container;

namespace Ofd.Tool.Merge;

/// <summary>
/// 裸 OFD 文档对象（Java BareOFDDoc）：提供较底层的文档构造能力，不涉及排版。
/// </summary>
public class BareOFDDoc : IDisposable
{
    private readonly string? _outPath;
    private bool _closed;

    /// <summary>OFD 打包容器。</summary>
    public readonly OFDDir OfdDir;

    /// <summary>文档内对象 ID 最大值。</summary>
    public readonly AtomicCounter MaxUnitID = new(0);

    /// <summary>资源管理器。</summary>
    public readonly ResManager Prm;

    /// <summary>文档公共数据。</summary>
    public CT_CommonData CData { get; }

    /// <summary>OFD 文档对象。</summary>
    public readonly Document Document;

    /// <summary>正在操作的文档目录。</summary>
    public readonly DocDir DocDir;

    /// <summary>在指定路径创建 OFD 文件。</summary>
    public BareOFDDoc(string outPath)
    {
        if (outPath == null) throw new ArgumentException("OFD文件存储路径(outPath)为空");
        if (Directory.Exists(outPath)) throw new ArgumentException("OFD文件存储路径(outPath)不能是目录");

        string? parent = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (!Directory.Exists(parent))
            throw new ArgumentException($"OFD文件存储路径(outPath)上级目录 [{parent}] 不存在");

        _outPath = outPath;

        var docInfo = new CT_DocInfo()
            .SetDocID(Guid.NewGuid())
            .SetCreationDate(DateOnly.FromDateTime(DateTime.Now))
            .SetCreator("OFD R&W")
            .SetCreatorVersion(GlobalVar.Version);

        var docBody = new DocBody()
            .SetDocInfo(docInfo)
            .SetDocRoot(new ST_Loc("Doc_0/Document.xml"));

        var ofd = new OFD().AddDocBody(docBody);

        Document = new Document();
        CData = new CT_CommonData();
        CData.SetPageArea(PageLayout.A4().GetPageArea());
        Document.SetCommonData(CData).SetPages(new Pages());

        OfdDir = OFDDir.NewOFD().SetOfd(ofd);

        var docDir = OfdDir.NewDoc();
        DocDir = docDir;
        docDir.SetDocument(Document);

        Prm = new ResManager(OfdDir, docDir, MaxUnitID);
    }

    /// <summary>回写 MaxUnitID 并打包。</summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;

        try
        {
            // 空文档（未分配任何对象）时 MaxUnitID 为 0，ST_ID 不允许 0，故跳过
            if (MaxUnitID.Get() > 0) CData.SetMaxUnitID(MaxUnitID.Get());

            if (_outPath == null) throw new ArgumentException("OFD文档输出地址错误或没有设置输出流");

            using var fs = File.Create(_outPath);
            OfdDir.Jar(fs);
        }
        finally
        {
            OfdDir.Clean();
        }
    }

    public void Dispose() => Close();
}
