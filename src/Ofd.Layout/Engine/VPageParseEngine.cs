using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Layout.Edit;
using Ofd.Layout.Element;
using Ofd.Layout.Engine.Render;
using Ofd.Layout.Handler;
using Ofd.Pkg.Container;

namespace Ofd.Layout.Engine;

/// <summary>
/// 虚拟页面解析引擎（Java VPageParseEngine）：把虚拟页面写成 OFD 页面（Pages/Page_N/Content.xml）。
/// </summary>
public class VPageParseEngine
{
    private readonly DocDir _docDir;
    private readonly AtomicCounter _maxUnitId;
    private readonly Pages _pages;
    private readonly PagesDir _pagesDir;
    private readonly PageLayout _pageLayout;
    private readonly ResManager _resManager;

    /// <summary>已注册的元素处理器（按元素类型名索引）。</summary>
    private static readonly Dictionary<string, IProcessor> RegisteredProcessors = new();

    static VPageParseEngine()
    {
        // Div 无需注册：任何元素都先经 DivRender 绘制，保证 Div 内容在最下方
        Register("Img", new ImgRender());
        Register("Paragraph", new ParagraphRender());
        Register("Canvas", new CanvasRender());
        Register("AreaHolderBlock", new AreaHolderBlockRender());
    }

    /// <summary>注册元素处理器。</summary>
    public static void Register(string elementType, IProcessor processor) =>
        RegisteredProcessors[elementType] = processor;

    /// <summary>页面解析前处理器。</summary>
    private IVPageHandler? _beforePageParseHandler;

    public VPageParseEngine(PageLayout pageLayout, DocDir docDir, ResManager prm, AtomicCounter maxUnitId)
    {
        _docDir = docDir;
        _maxUnitId = maxUnitId;
        _pageLayout = pageLayout;
        _resManager = prm;

        Document document;
        try
        {
            document = docDir.GetDocument();
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("无法获取到Document.xml 对象", e);
        }

        var pages = document.GetPages();
        if (pages == null)
        {
            pages = new Pages();
            document.SetPages(pages);
        }
        _pages = pages;
        _pagesDir = docDir.ObtainPages();

        // 与页面引擎共用同一 DocDir 实例，避免容器缓存分叉（资源/占位区块文件写入同一缓存树）
        _resManager.SetDocDir(docDir);
    }

    /// <summary>解析虚拟页面队列为 OFD 页面。</summary>
    public void Process(List<VirtualPage>? vPageList)
    {
        if (vPageList == null || vPageList.Count == 0) return;

        var seq = new LinkedList<VirtualPage>(vPageList);
        while (seq.Count > 0)
        {
            var virtualPage = seq.First!.Value;
            seq.RemoveFirst();

            if (virtualPage == null) continue;

            if (virtualPage is AdditionVPage addition)
            {
                _beforePageParseHandler?.Handle(virtualPage);
                PageEdit(addition);
            }
            else
            {
                PageDir pageDir;
                if (virtualPage.GetPageNum() == null)
                {
                    pageDir = NewPage();
                    virtualPage.SetPageNum(_pages.GetSize());
                }
                else
                {
                    pageDir = AddNewPage(virtualPage.GetPageNum()!.Value - 1);
                }

                var pageLoc = pageDir.GetAbsLoc().Cat(PageDir.ContentFileName);

                _beforePageParseHandler?.Handle(virtualPage);
                ConvertPageContent(pageLoc, virtualPage, pageDir);
            }
        }
    }

    /// <summary>转换虚拟页面内容为实际 OFD 元素。</summary>
    private void ConvertPageContent(ST_Loc pageLoc, VirtualPage vPage, PageDir pageDir)
    {
        var page = new PageObj(); // 页面内容对象（含 Area/Content/Template）
        var vPageStyle = vPage.GetStyle();
        if (vPageStyle != null && !_pageLayout.Equals(vPageStyle))
        {
            // 与默认页面样式不一致 → 单独设置页面区域
            page.SetArea(vPageStyle.GetPageArea());
        }

        foreach (var tpl in vPage.GetTemplates()) page.AddTemplate(tpl);

        pageDir.SetContent(page);

        if (vPage.GetContent().Count == 0) return;

        var content = new Content();
        var layerArr = vPage.GetLayerContent();
        foreach (var layerContent in layerArr)
        {
            if (layerContent.Count == 0) continue;

            var type = layerContent[0].GetLayer();
            var ctLayer = new CT_Layer();
            ctLayer.SetType(type);
            ctLayer.SetObjID(_maxUnitId.IncrementAndGet());
            content.AddLayer(ctLayer);

            Convert2Layer(pageLoc, ctLayer, layerContent);
        }

        page.SetContent(content);
    }

    /// <summary>编辑已有页面（追加图层）。</summary>
    private void PageEdit(AdditionVPage virtualPage)
    {
        var pageLoc = virtualPage.GetPageLoc();
        var layerArr = virtualPage.GetLayerContent();

        foreach (var layerContent in layerArr)
        {
            if (layerContent.Count == 0) continue;

            var type = layerContent[0].GetLayer();
            var ctLayer = virtualPage.NewLayer(_maxUnitId);
            if (ctLayer == null) continue;

            ctLayer.SetType(type);
            ctLayer.SetObjID(_maxUnitId.IncrementAndGet());

            Convert2Layer(pageLoc, ctLayer, layerContent);
        }
    }

    /// <summary>把虚拟页面元素转为 OFD 元素加入图层。</summary>
    private void Convert2Layer(ST_Loc? pageLoc, CT_PageBlock to, List<Div> content)
    {
        foreach (var elem in content)
        {
            // 忽略占位符
            if (elem is PageAreaFiller || elem.IsPlaceholder()) continue;

            int objStart = _maxUnitId.Get();

            // 基础盒式模型（背景/边框）
            DivRender.Render(to, elem, _maxUnitId);

            // 扩展元素处理器
            if (RegisteredProcessors.TryGetValue(elem.ElementType(), out var processor))
            {
                processor.Render(pageLoc!, to, _resManager, elem, _maxUnitId);
            }

            int objEnd = _maxUnitId.Get();

            // 渲染完成回调
            var onRenderFinish = elem.GetOnRenderFinish();
            if (onRenderFinish != null)
            {
                var objIds = new List<ST_ID>();
                var resIds = new List<ST_ID>();
                var newResIds = _resManager.GetNewResIds();

                for (int id = objEnd; id > objStart; id--)
                {
                    var objId = new ST_ID(id);
                    if (newResIds.Contains(objId)) resIds.Add(objId);
                    else objIds.Add(objId);
                }

                onRenderFinish(pageLoc!, objIds, resIds);
            }
        }
    }

    /// <summary>创建页面容器并加入页树。</summary>
    private PageDir NewPage()
    {
        var pageDir = _pagesDir.NewPageDir();
        string pageLoc = $"Pages/Page_{pageDir.GetIndex()}/Content.xml";
        var page = new Page(_maxUnitId.IncrementAndGet(), pageLoc);
        _pages.AddPage(page);
        return pageDir;
    }

    /// <summary>创建页面容器并插入到指定索引（页码 - 1）。</summary>
    private PageDir AddNewPage(int index)
    {
        var pageDir = _pagesDir.NewPageDir();
        string pageLoc = $"Pages/Page_{pageDir.GetIndex()}/Content.xml";
        var page = new Page(_maxUnitId.IncrementAndGet(), pageLoc);

        int size = _pages.GetSize();
        if (index <= 0) index = 0;
        else if (index >= size) index = size;

        // 与 Java 的 elements().add(index, page) 等价：直接插入 DOM 子节点
        _pages.GetProxy().Kids.Insert(index, page.GetProxy());
        return pageDir;
    }

    public IVPageHandler? GetBeforePageParseHandler() => _beforePageParseHandler;

    public void SetBeforePageParseHandler(IVPageHandler? handler) => _beforePageParseHandler = handler;
}
