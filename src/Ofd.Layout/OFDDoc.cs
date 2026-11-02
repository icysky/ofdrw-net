using Ofd.Core.Attachment;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.Ofd.DocInfo;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Layout.Edit;
using Ofd.Layout.Engine;
using Ofd.Layout.Element;
using Ofd.Layout.Exceptions;
using Ofd.Layout.Handler;
using Ofd.Pkg;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Layout;

/// <summary>
/// 虚拟文档对象（Java OFDDoc）：以 API 方式构造 OFD 文档并打包为 OFD 文件。
/// <para>
/// 注：附件（Attachment）、注释（Annotation）、水印（Watermark）相关方法随 P4.4 edit 包移植后补充。
/// </para>
/// </summary>
public class OFDDoc : IDisposable
{
    /// <summary>已有 OFD 文档解析器（仅修改模式有效）。</summary>
    private OFDReader? _reader;

    /// <summary>OFD 打包容器。</summary>
    private OFDDir _ofdDir = null!;

    /// <summary>输出路径（与输出流二选一）。</summary>
    private readonly string? _outPath;

    /// <summary>输出流（与输出路径二选一）。</summary>
    private readonly Stream? _outStream;

    /// <summary>文档内对象 ID 最大值（自增提供器）。</summary>
    private AtomicCounter _maxUnitId = new(0);

    /// <summary>资源管理器。</summary>
    private ResManager _prm = null!;

    /// <summary>流式布局元素队列。</summary>
    private readonly List<Div> _streamQueue = new();

    /// <summary>固定布局虚拟页面队列。</summary>
    private readonly List<VirtualPage> _vPageList = new();

    /// <summary>编辑期流式内容集合。</summary>
    private readonly List<StreamCollect> _sPageList = new();

    /// <summary>页面样式（默认 A4）。</summary>
    private PageLayout _pageLayout = PageLayout.A4();

    /// <summary>文档公共数据（保留引用便于操作）。</summary>
    private CT_CommonData _cdata = null!;

    private bool _closed;

    /// <summary>OFD 文档对象。</summary>
    private Document _ofdDocument = null!;

    /// <summary>渲染结束回调。</summary>
    private IRenderFinishHandler? _renderingEndHandler;

    /// <summary>页面解析前处理器。</summary>
    private IVPageHandler? _onPageHandler;

    /// <summary>注释渲染器（仅在需要时初始化）。</summary>
    private AnnotationRender? _annotationRender;

    /// <summary>在指定路径创建 OFD 文件。</summary>
    public OFDDoc(string outPath) : this()
    {
        if (outPath == null) throw new ArgumentException("OFD文件存储路径(outPath)为空");
        if (Directory.Exists(outPath)) throw new ArgumentException("OFD文件存储路径(outPath)不能是目录");

        var parent = Path.GetDirectoryName(Path.GetFullPath(outPath));
        if (parent == null || !Directory.Exists(parent))
            throw new ArgumentException($"OFD文件存储路径(outPath)上级目录 [{parent}] 不存在");

        _outPath = outPath;
        (_ofdDocument, _cdata, _ofdDir, _prm) = ContainerInitNew();
    }

    /// <summary>输出到指定流（由调用者负责关闭）。</summary>
    public OFDDoc(Stream outStream) : this()
    {
        _outStream = outStream ?? throw new ArgumentException("OFD文件输出流(outStream)为空");
        (_ofdDocument, _cdata, _ofdDir, _prm) = ContainerInitNew();
    }

    /// <summary>修改已有 OFD 文档。</summary>
    public OFDDoc(OFDReader reader, string outPath) : this()
    {
        if (reader == null) throw new ArgumentException("OFD解析器(reader)不能为空");
        if (outPath == null) throw new ArgumentException("OFD文件存储路径(outPath)为空");
        if (Directory.Exists(outPath)) throw new ArgumentException("OFD文件存储路径(outPath)不能是目录");

        _outPath = outPath;
        _reader = reader;

        try
        {
            (_ofdDocument, _cdata, _ofdDir, _prm, _maxUnitId) = ContainerInitExisting(reader);
        }
        catch (Exception e)
        {
            throw new DocReadException("OFD文件解析异常", e);
        }
    }

    /// <summary>修改已有 OFD 文档并输出到流。</summary>
    public OFDDoc(OFDReader reader, Stream outStream) : this()
    {
        if (reader == null) throw new ArgumentException("OFD解析器(reader)不能为空");
        _outStream = outStream ?? throw new ArgumentException("OFD文件输出流(outStream)为空");
        _reader = reader;

        try
        {
            (_ofdDocument, _cdata, _ofdDir, _prm, _maxUnitId) = ContainerInitExisting(reader);
        }
        catch (Exception e)
        {
            throw new DocReadException("OFD文件解析异常", e);
        }
    }

    /// <summary>私有基础构造：仅初始化只读字段。</summary>
    private OFDDoc()
    {
        // 只读字段的实际赋值在各公开构造函数中完成（ContainerInit*）
        _ofdDocument = null!;
        _cdata = null!;
        _ofdDir = null!;
        _prm = null!;
    }

    /// <summary>新建文档容器。</summary>
    private (Document Doc, CT_CommonData CData, OFDDir OfdDir, ResManager Prm) ContainerInitNew()
    {
        var docInfo = new CT_DocInfo()
            .SetDocID(Guid.NewGuid())
            .SetCreationDate(DateOnly.FromDateTime(DateTime.Now))
            .SetCreator("OFD R&W")
            .SetCreatorVersion(GlobalVar.Version);

        var docBody = new DocBody()
            .SetDocInfo(docInfo)
            .SetDocRoot(new ST_Loc("Doc_0/Document.xml"));

        var ofd = new OFD().AddDocBody(docBody);

        var document = new Document();
        var cdata = new CT_CommonData();

        // 设置页面属性（默认 RGB，不设置颜色空间）
        cdata.SetPageArea(_pageLayout.GetPageArea());

        document.SetCommonData(cdata);
        document.SetPages(new Pages()); // 页面引用集合，解析虚拟页面时填充

        var ofdDir = OFDDir.NewOFD().SetOfd(ofd);
        var docDir = ofdDir.NewDoc();
        docDir.SetDocument(document);

        return (document, cdata, ofdDir, new ResManager(ofdDir, docDir, _maxUnitId));
    }

    /// <summary>通过已有文档初始化容器。</summary>
    private (Document, CT_CommonData, OFDDir, ResManager, AtomicCounter) ContainerInitExisting(OFDReader reader)
    {
        var ofdDir = reader.GetOFDDir();
        var ofd = ofdDir.GetOfd();
        var docBody = ofd.GetDocBody() ?? throw new InvalidOperationException("OFD 缺少 DocBody");

        // 设置文档修改时间
        docBody.GetDocInfo()?.SetModDate(DateOnly.FromDateTime(DateTime.Now));

        var rl = reader.GetResourceLocator();
        var docRoot = docBody.GetDocRoot() ?? throw new InvalidOperationException("OFD 缺少 DocRoot");
        var document = rl.Get(docRoot, e => new Document(e));

        var cdata = document.GetCommonData() ?? new CT_CommonData();
        var maxUnitId = new AtomicCounter((int)(cdata.GetMaxUnitID()?.Id ?? 0));

        var prm = new ResManager(reader);
        return (document, cdata, ofdDir, prm, maxUnitId);
    }

    /// <summary>设置页面默认样式（同步文档页面区域）。</summary>
    public OFDDoc SetDefaultPageLayout(PageLayout? pageLayout)
    {
        if (pageLayout == null) return this;
        _pageLayout = pageLayout;
        _cdata.SetPageArea(pageLayout.GetPageArea());
        return this;
    }

    /// <summary>获取页面样式（副本）。</summary>
    public PageLayout GetPageLayout() => _pageLayout.Clone();

    /// <summary>加入流式布局元素。</summary>
    public OFDDoc Add(Div item)
    {
        if (_streamQueue.Contains(item))
            throw new ArgumentException("元素已经存在，请重复放入");

        _streamQueue.Add(item);
        return this;
    }

    /// <summary>加入固定布局虚拟页面。</summary>
    public OFDDoc AddVPage(VirtualPage virtualPage)
    {
        _vPageList.Add(virtualPage);
        return this;
    }

    /// <summary>加入编辑期流式内容集合。</summary>
    public OFDDoc AddStreamCollect(StreamCollect streamCollect)
    {
        _sPageList.Add(streamCollect);
        return this;
    }

    /// <summary>获取指定页码的追加页面对象（仅修改模式）。</summary>
    public AdditionVPage GetAVPage(int pageNum)
    {
        if (_reader == null)
            throw new InvalidOperationException("仅在修改模式下允许获取追加页面对象（AdditionVPage）");

        var pageAbsLoc = _reader.GetPageAbsLoc(pageNum);
        var rl = _reader.GetResourceLocator();

        try
        {
            var page = rl.Get(pageAbsLoc, e => new Ofd.Core.BasicStructure.PageObj.PageObj(e));
            var avp = new AdditionVPage(page, pageAbsLoc);
            avp.SetPageNum(pageNum);

            AddVPage(avp); // 自动加入虚拟页面列表
            return avp;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("OFD解析失败，原因:" + e.Message, e);
        }
    }

    /// <summary>获取 OFD 虚拟容器。</summary>
    public OFDDir GetOfdDir() => _ofdDir;

    /// <summary>获取文档根节点。</summary>
    public Document GetOfdDocument() => _ofdDocument;

    /// <summary>设置渲染结束回调。</summary>
    public OFDDoc OnRenderFinish(IRenderFinishHandler? handler)
    {
        _renderingEndHandler = handler;
        return this;
    }

    /// <summary>获取正在编辑文档的 Reader（新建模式为 null）。</summary>
    public OFDReader? GetReader() => _reader;

    /// <summary>获取资源管理器。</summary>
    public ResManager GetResManager() => _prm;

    /// <summary>获取页面解析前回调。</summary>
    public IVPageHandler? GetOnPage() => _onPageHandler;

    /// <summary>设置页面解析前回调（页头/页脚等）。</summary>
    public OFDDoc OnPage(IVPageHandler? handler)
    {
        _onPageHandler = handler;
        return this;
    }

    /// <summary>
    /// 为整个文档添加水印（遍历全部页面，按注释形式写入，仅修改模式）。
    /// </summary>
    public OFDDoc AddWatermark(Watermark? watermark)
    {
        if (watermark == null) return this;

        if (_reader == null)
            throw new InvalidOperationException("仅在修改模式下允许获取追加注释对象，请使用reader构造");

        int pageNums = _reader.GetNumberOfPages();
        if (pageNums == 0) return this;

        for (int i = 1; i <= pageNums; i++) AddAnnotation(i, watermark);
        return this;
    }

    /// <summary>
    /// 向指定页添加注释（仅修改模式）。
    /// </summary>
    public OFDDoc AddAnnotation(int pageNum, Annotation? annotation)
    {
        if (annotation == null) return this;

        if (_reader == null)
            throw new InvalidOperationException("仅在修改模式下允许获取追加注释对象，请使用reader构造");

        _annotationRender ??= new AnnotationRender(_ofdDir.ObtainDocDefault(), _prm, _maxUnitId);

        var pageInfo = _reader.GetPageInfo(pageNum);
        _annotationRender.Render(pageInfo, annotation);
        return this;
    }

    /// <summary>
    /// 向文档添加附件（放置于默认资源目录 <c>/Doc_0/Res/</c>，同名替换）。
    /// </summary>
    public OFDDoc AddAttachment(Attachment? attachment)
    {
        if (attachment == null) return this;

        var docDefault = _ofdDir.ObtainDocDefault();
        string resAbsPath = docDefault.ObtainRes().GetAbsLoc().ToString();
        return AddAttachment(resAbsPath, attachment);
    }

    /// <summary>
    /// 向文档添加附件（放置于指定容器绝对路径，如同名替换未禁用则先清理旧附件）。
    /// </summary>
    public OFDDoc AddAttachment(string absPath, Attachment? attachment)
    {
        if (attachment == null) return this;
        if (string.IsNullOrEmpty(absPath) || !absPath.StartsWith('/'))
            throw new ArgumentException("附件在OFD容器内的绝对位置(absPath)不能为空或不合法");

        string? file = attachment.GetFile();
        if (file == null || !File.Exists(file)) return this;

        // 放入容器指定目录
        var fileAbsLoc = new ST_Loc(absPath).Cat(Path.GetFileName(file));
        fileAbsLoc = _ofdDir.PutFileAbs(fileAbsLoc.ToString(), file);

        // 附件大小（KB）
        double size = new FileInfo(file).Length / 1024d;
        var ctAttachment = attachment.GetAttachment()
            .SetId(_maxUnitId.IncrementAndGet().ToString())
            .SetCreationDate(DateTime.Now)
            .SetSize(size)
            .SetFileLoc(fileAbsLoc);

        var docDefault = _ofdDir.ObtainDocDefault();
        var attachments = ObtainAttachments(docDefault);

        if (!attachment.IsDisableReplace())
            CleanOldAttachment(attachments, attachment.GetName());

        attachments.AddAttachment(ctAttachment);
        return this;
    }

    /// <summary>删除指定名称的附件（不存在则忽略）。</summary>
    public OFDDoc DeleteAttachment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return this;

        var docDefault = _ofdDir.ObtainDocDefault();
        var attachments = ObtainAttachments(docDefault);
        CleanOldAttachment(attachments, name);
        return this;
    }

    /// <summary>清理已有同名附件（记录与文件）。</summary>
    private void CleanOldAttachment(Attachments attachments, string name)
    {
        foreach (var att in attachments.GetAttachments())
        {
            if (att.GetAttachmentName() != name) continue;

            // 删除记录
            attachments.Remove(att);

            // 删除文件
            var fileLoc = att.GetFileLoc();
            if (fileLoc == null) continue;

            try
            {
                string file = _ofdDir.GetFileAbs(fileLoc.ToString());
                if (File.Exists(file)) File.Delete(file);
            }
            catch (IOException)
            {
                // 文件删除失败不影响记录清理
            }
            break;
        }
    }

    /// <summary>获取附件列表文件，不存在则创建（并在文档中登记）。</summary>
    private Attachments ObtainAttachments(DocDir docDir)
    {
        if (docDir.Exist(DocDir.Attachments))
            return new Attachments(docDir.GetObjByFile(DocDir.Attachments));

        var attachments = new Attachments();
        docDir.PutObj(DocDir.Attachments, attachments);
        _ofdDocument.SetAttachments(docDir.GetAbsLoc().Cat(DocDir.Attachments));
        return attachments;
    }

    /// <summary>
    /// 关闭文档并生成 OFD：流式队列分段 → 虚拟页面 → 页面解析 → 打包。
    /// </summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;

        try
        {
            // 1) 流式布局 → 虚拟页面
            if (_streamQueue.Count > 0)
            {
                var sgmEngine = new SegmentationEngine(_pageLayout);
                var analyzer = new StreamingLayoutAnalyzer(_pageLayout);

                var sgmQueue = sgmEngine.Process(_streamQueue);
                var vPages = analyzer.Analyze(sgmQueue);
                _vPageList.AddRange(vPages);
            }

            // 2) 编辑期流式集合
            foreach (var sCollect in _sPageList)
                _vPageList.AddRange(sCollect.Analyze(_pageLayout));

            // 3) 虚拟页面 → OFD 页面
            if (_vPageList.Count > 0)
            {
                var docDefault = _ofdDir.ObtainDocDefault();
                var parseEngine = new VPageParseEngine(_pageLayout, docDefault, _prm, _maxUnitId);
                parseEngine.SetBeforePageParseHandler(_onPageHandler);
                parseEngine.Process(_vPageList);
            }

            // 新建模式下没有任何页面则视为错误
            if (_vPageList.Count == 0 && _reader == null)
                throw new InvalidOperationException("OFD文档中没有页面，无法生成OFD文档");

            // 4) 渲染结束回调
            _renderingEndHandler?.Handle(_maxUnitId, _ofdDir, _ofdDir.ObtainDocDefault().GetIndex());

            // 5) 回写 MaxUnitID
            _cdata.SetMaxUnitID(_maxUnitId.Get());

            // 6) 打包
            if (_outPath != null)
            {
                using var fs = File.Create(_outPath);
                _ofdDir.Jar(fs);
            }
            else if (_outStream != null)
            {
                _ofdDir.Jar(_outStream);
            }
            else
            {
                throw new ArgumentException("OFD文档输出地址错误或没有设置输出流");
            }
        }
        finally
        {
            _reader?.Dispose();
            _ofdDir.Clean(); // 清理生成过程中的工作区文件
        }
    }

    public void Dispose() => Close();
}
