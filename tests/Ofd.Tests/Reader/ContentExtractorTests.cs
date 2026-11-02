using System.Drawing;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Core.Text;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Ofd.Reader.Extractor;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.Reader;

/// <summary>文本抽取链路：DeltaTool / ContentExtractor / RegionTextExtractorFilter。</summary>
public class ContentExtractorTests : IDisposable
{
    private readonly string _root;

    public ContentExtractorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_text_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    private static TextObject MakeText(long id, double x, double y, string content, params double[] deltaX)
    {
        var text = new TextObject(new ST_ID(id));
        text.SetBoundary(x, y, 200, 20);
        text.SetFont(ST_RefID.GetInstance(1));
        text.SetSize(3.5);
        var code = new TextCode().SetCoordinate(x, y).SetContent(content);
        if (deltaX.Length != 0) code.SetDeltaX(deltaX);
        text.AddTextCode(code);
        return text;
    }

    private string BuildTextOfd()
    {
        string root = Path.Combine(_root, "text");
        var ofdDir = new OFDDir(root);
        var doc = ofdDir.NewDoc();

        doc.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(new Pages()
                .AddPage(new PageNode().SetID(new ST_ID(1)).SetBaseLoc(new ST_Loc("Pages/Page_0/Content.xml")))
                .AddPage(new PageNode().SetID(new ST_ID(2)).SetBaseLoc(new ST_Loc("Pages/Page_1/Content.xml")))));

        var pagesDir = doc.ObtainPages();

        // 第 1 页：正文层（直接文本 + 嵌套页块内文本）
        var layer1 = new CT_Layer().SetType(LayerType.Body);
        layer1.SetObjID(20);
        layer1.AddPageBlock(MakeText(21, 10, 20, "第一段", 3.5, 3.5, 3.5));
        var nested = new CT_PageBlock();
        nested.AddPageBlock(MakeText(22, 10, 40, "嵌套文字", 3.5));
        layer1.AddPageBlock(nested);
        pagesDir.NewPageDir().SetContent(new PageObjType().SetContent(new Content().AddLayer(layer1)));

        // 第 2 页：正文层（单文本）
        var layer2 = new CT_Layer().SetType(LayerType.Body);
        layer2.SetObjID(23);
        layer2.AddPageBlock(MakeText(24, 10, 20, "第二页", 3.5));
        pagesDir.NewPageDir().SetContent(new PageObjType().SetContent(new Content().AddLayer(layer2)));

        ofdDir.SetOfd(new OFD().AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml"))));
        ofdDir.Flush();

        string ofdFile = Path.Combine(_root, "text.ofd");
        ofdDir.Jar(ofdFile);
        return ofdFile;
    }

    [Fact]
    public void ContentExtractor_extracts_page_and_all_text()
    {
        using var reader = new OFDReader(BuildTextOfd());
        var extractor = new ContentExtractor(reader);

        // 单页文本（含嵌套页块递归）
        var page1 = extractor.GetPageContent(1);
        Assert.Equal(new[] { "第一段", "嵌套文字" }, page1);
        Assert.Equal(new[] { "第二页" }, extractor.GetPageContent(2));

        // 文本对象
        Assert.Equal(2, extractor.GetPageTextObject(1).Count);
        Assert.Single(extractor.GetPageTextObject(2));

        // 全文档
        Assert.Equal(new[] { "第一段", "嵌套文字", "第二页" }, extractor.ExtractAll());
        Assert.Equal(3, extractor.ExtractAllTextObject().Count);

        // 遍历回调
        var visited = new List<string>();
        extractor.Traverse((pageNum, contents) => visited.Add(pageNum + ":" + string.Join("|", contents)));
        Assert.Equal(new[] { "1:第一段|嵌套文字", "2:第二页" }, visited);
    }

    [Fact]
    public void ContentExtractor_with_region_filter()
    {
        using var reader = new OFDReader(BuildTextOfd());

        // 字符位置 = Boundary.TopLeft + TextCode 坐标（叠加语义，与 Java 一致）：
        // 第 1 页 "第一段" 与第 2 页 "第二页" 落在 y=40，嵌套文字落在 y=80 → 矩形 y<50 排除嵌套文字
        var filter = new RegionTextExtractorFilter(new RectangleF(0, 0, 500, 50));
        var extractor = new ContentExtractor(reader, filter);

        Assert.Equal(new[] { "第一段" }, extractor.GetPageContent(1));
        // 第 2 页 y=20 也在区域内 → 全文档两段
        Assert.Equal(new[] { "第一段", "第二页" }, extractor.ExtractAll());
        // 过滤后无命中的页面返回空集合（不产生空串）
        Assert.Empty(new ContentExtractor(reader, new RegionTextExtractorFilter(new RectangleF(0, 900, 10, 10)))
            .GetPageContent(1));
    }

    [Fact]
    public void DeltaTool_expands_g_syntax_and_pads_last_value()
    {
        // null → 空
        Assert.Empty(DeltaTool.GetDelta(null, 3));

        // 普通值
        Assert.Equal(new[] { 10f, 20f, 30f }, DeltaTool.GetDelta(ST_Array.GetInstance("10 20 30"), 3));

        // g 压缩：1 g 3 2 → 1,2,2,2
        Assert.Equal(new[] { 1f, 2f, 2f, 2f }, DeltaTool.GetDelta(ST_Array.GetInstance("1 g 3 2"), 4));

        // 不足时用最后一个值补齐：10 20 + 2 个 20
        Assert.Equal(new[] { 10f, 20f, 20f, 20f }, DeltaTool.GetDelta(ST_Array.GetInstance("10 20"), 4));
    }

    [Fact]
    public void Region_filter_uses_deltas_and_ctm()
    {
        // 无 CTM：每个字符间距 10，矩形覆盖 x<25 → "ABC"
        var text = new TextObject(new ST_ID(1));
        text.SetBoundary(0, 0, 100, 20);
        text.AddTextCode(new TextCode().SetCoordinate(0, 10).SetContent("ABCD").SetDeltaX(10, 10, 10, 10));
        var code = text.GetTextCodes()[0];

        var filter1 = new RegionTextExtractorFilter(new RectangleF(0, 0, 25, 20));
        Assert.Equal("ABC", filter1.GetAllowText(text, code));

        // 无 Delta：所有字符重叠于同一点 → 全命中或全不命中
        var text2 = new TextObject(new ST_ID(2));
        text2.SetBoundary(0, 0, 100, 20);
        text2.AddTextCode(new TextCode().SetCoordinate(50, 10).SetContent("XY"));
        var code2 = text2.GetTextCodes()[0];
        Assert.Equal("XY", new RegionTextExtractorFilter(new RectangleF(40, 0, 30, 20)).GetAllowText(text2, code2));
        Assert.Null(new RegionTextExtractorFilter(new RectangleF(0, 0, 10, 10)).GetAllowText(text2, code2));

        // 带 CTM（平移 100）：x' = x + 100
        var text3 = new TextObject(new ST_ID(3));
        text3.SetBoundary(0, 0, 100, 20);
        text3.SetCTM(new ST_Array(1d, 0d, 0d, 1d, 100d, 0d));
        text3.AddTextCode(new TextCode().SetCoordinate(0, 10).SetContent("ABCD").SetDeltaX(10, 10, 10, 10));
        var code3 = text3.GetTextCodes()[0];
        Assert.Equal("ABC", new RegionTextExtractorFilter(new RectangleF(100, 0, 25, 20)).GetAllowText(text3, code3));
        Assert.Null(new RegionTextExtractorFilter(new RectangleF(0, 0, 25, 20)).GetAllowText(text3, code3));

        // 空内容返回 null
        var empty = new TextObject(new ST_ID(4));
        empty.SetBoundary(0, 0, 10, 10);
        empty.AddTextCode(new TextCode().SetCoordinate(0, 0).SetContent(""));
        Assert.Null(new RegionTextExtractorFilter(new RectangleF(0, 0, 100, 100))
            .GetAllowText(empty, empty.GetTextCodes()[0]));
    }
}
