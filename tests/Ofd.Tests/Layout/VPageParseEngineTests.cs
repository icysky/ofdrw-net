using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Layout;
using Ofd.Layout.AreaHolder;
using Ofd.Layout.Edit;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Tests.Layout;

/// <summary>虚拟页面解析引擎：页面生成、图层装配、处理器调度、区域占位、追加页面。</summary>
public class VPageParseEngineTests : IDisposable
{
    private readonly string _root;
    private readonly DocDir _docDir;
    private readonly ResManager _res;
    private readonly AtomicCounter _counter;

    public VPageParseEngineTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_vpage_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);

        var ofdDir = new OFDDir(Path.Combine(_root, "ofd"));
        _docDir = ofdDir.NewDoc();
        _docDir.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(new Pages()));

        _res = new ResManager(ofdDir, _docDir, new AtomicCounter(100));
        _counter = new AtomicCounter(100);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // ignore
        }
    }

    private VPageParseEngine NewEngine(PageLayout? layout = null) =>
        new(layout ?? PageLayout.A4(), _docDir, _res, _counter);

    /// <summary>绝对定位的 Canvas（内含一条线段）。</summary>
    private static Canvas LineCanvas(double x, double y, double w = 50, double h = 20) =>
        new Canvas(x, y, w, h).SetDrawer(new Line(w, h).SetBeginPoint(0, 0).SetEndPoint(10, 10));

    [Fact]
    public void Process_writes_page_content_with_layers()
    {
        var engine = NewEngine();
        var page = new VirtualPage(PageLayout.A4());
        page.Add(LineCanvas(5, 6));

        engine.Process(new List<VirtualPage> { page });

        var pages = _docDir.GetDocument().GetPages()!;
        Assert.Equal(1, pages.GetSize());
        Assert.Equal("Pages/Page_0/Content.xml", pages.GetPageByIndex(0).GetBaseLoc()!.Loc);

        // Content.xml 已落盘并可读回
        var pageDir = _docDir.ObtainPages().GetByIndex(0);
        var content = pageDir.GetContent().GetContent()!;
        var layers = content.GetLayers();
        Assert.Single(layers);
        Assert.Equal(LayerType.Body, layers[0].GetType());

        // Canvas 的页块 + 内部线段
        var block = Assert.IsType<CT_PageBlock>(Assert.Single(layers[0].GetPageBlocks()));
        var path = Assert.IsType<PathObject>(Assert.Single(block.GetPageBlocks()));
        Assert.True(path.GetStroke());
    }

    [Fact]
    public void Process_skips_placeholder_elements()
    {
        var engine = NewEngine();
        var page = new VirtualPage(PageLayout.A4());
        page.Add(new PageAreaFiller().SetPosition(Position.Absolute).SetX(0).SetY(0).SetWidth(10));
        page.Add(Div.Placeholder(10, 10, AFloat.Left).SetXY(0, 0).SetPosition(Position.Absolute));

        engine.Process(new List<VirtualPage> { page });

        var content = _docDir.ObtainPages().GetByIndex(0).GetContent().GetContent()!;
        // 占位符仍会产生图层，但图层内不产出任何图元（与 Java 一致）
        var layer = Assert.Single(content.GetLayers());
        Assert.Empty(layer.GetPageBlocks());
    }

    [Fact]
    public void Process_inserts_page_at_specified_number()
    {
        var engine = NewEngine();

        var first = new VirtualPage(PageLayout.A4());
        first.Add(LineCanvas(0, 0));
        engine.Process(new List<VirtualPage> { first });

        var second = new VirtualPage(PageLayout.A4()).SetPageNum(1);
        second.Add(LineCanvas(1, 1));
        engine.Process(new List<VirtualPage> { second });

        var pages = _docDir.GetDocument().GetPages()!;
        Assert.Equal(2, pages.GetSize());
        // 插入到第 1 页 → 新页(Page_1)在前
        Assert.Equal("Pages/Page_1/Content.xml", pages.GetPageByIndex(0).GetBaseLoc()!.Loc);
        Assert.Equal("Pages/Page_0/Content.xml", pages.GetPageByIndex(1).GetBaseLoc()!.Loc);
    }

    [Fact]
    public void Process_invokes_before_page_handler()
    {
        var engine = NewEngine();
        int handled = 0;
        engine.SetBeforePageParseHandler(new LambdaHandler(_ => handled++));

        var page = new VirtualPage(PageLayout.A4());
        page.Add(LineCanvas(0, 0));

        engine.Process(new List<VirtualPage> { page });
        Assert.Equal(1, handled);
        Assert.NotNull(engine.GetBeforePageParseHandler());
    }

    [Fact]
    public void Process_registers_area_holder_block()
    {
        var engine = NewEngine();
        var page = new VirtualPage(PageLayout.A4());
        var holder = new AreaHolderBlock("signArea", 10, 10, 40, 20);
        page.Add(holder);

        engine.Process(new List<VirtualPage> { page });

        // AreaHolderBlocks.xml 已创建并登记
        Assert.True(AreaHolderBlocksProcess.Exist(_docDir));
        var blocks = AreaHolderBlocksProcess.Get(_docDir);
        var block = Assert.Single(blocks.GetAreaHolderBlocks());
        Assert.Equal("signArea", block.GetAreaName());
        Assert.NotNull(block.GetPageBlockID());
        Assert.NotNull(block.GetPageFile());
        Assert.Equal(40d, block.GetBoundary()!.Width, 6);
        Assert.Equal(20d, block.GetBoundary()!.Height, 6);

        // 页面内在该区域有一个空页块占位
        var content = _docDir.ObtainPages().GetByIndex(0).GetContent().GetContent()!;
        var layer = Assert.Single(content.GetLayers());
        var placeholder = Assert.IsType<CT_PageBlock>(Assert.Single(layer.GetPageBlocks()));
        Assert.Equal(block.GetPageBlockID()!.GetRefId()!.Id, placeholder.GetObjID()!.Id);

        // 按名称查找
        Assert.NotNull(AreaHolderBlocksProcess.Find(blocks, "signArea"));
        Assert.Null(AreaHolderBlocksProcess.Find(blocks, "nope"));
    }

    [Fact]
    public void Process_appends_to_existing_page_via_addition_vpage()
    {
        var engine = NewEngine();

        var pageObj = new PageObj().SetContent(new Content());
        var addition = new AdditionVPage(pageObj, new Ofd.Core.BasicType.ST_Loc("/Doc_0/Pages/Page_0/Content.xml"));

        var canvas = LineCanvas(2, 3);
        addition.Add(canvas);

        engine.Process(new List<VirtualPage> { addition });

        // 页面内容新增 1 个图层，且包含 Canvas 页块
        var layers = pageObj.GetContent()!.GetLayers();
        Assert.Single(layers);
        var block = Assert.IsType<CT_PageBlock>(Assert.Single(layers[0].GetPageBlocks()));
        Assert.Single(block.GetPageBlocks());
    }

    [Fact]
    public void Process_ignores_empty_input()
    {
        var engine = NewEngine();
        engine.Process(null);
        engine.Process(new List<VirtualPage>());
        Assert.Equal(0, _docDir.GetDocument().GetPages()!.GetSize());
    }

    private sealed class LambdaHandler : Ofd.Layout.Handler.IVPageHandler
    {
        private readonly Action<VirtualPage> _action;

        public LambdaHandler(Action<VirtualPage> action) => _action = action;

        public void Handle(VirtualPage page) => _action(page);
    }
}
