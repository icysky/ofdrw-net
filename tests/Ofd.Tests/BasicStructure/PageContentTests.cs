using System.Text;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.CompositeObj;
using Ofd.Core.Dom;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.Graph.Tight;
using Ofd.Core.Image;
using Ofd.Core.PageDescription.Clips;
using Ofd.Core.Text;
using Ofd.Core.Text.Text;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;
using PageContent = Ofd.Core.BasicStructure.PageObj.Content;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.BasicStructure;

/// <summary>页面内容对象树（7.7）：PageObj/Content/CT_Layer/四种图元对象/路径数据/裁剪/矢量图。</summary>
public class PageContentTests
{
    private static PageObjType BuildPage()
    {
        // 背景层：一个嵌套页块 + 图形对象
        var bgLayer = new CT_Layer().SetType(LayerType.Background);
        bgLayer.SetObjID(10);
        var block = new CT_PageBlock();
        var pathObj = new PathObject(new ST_ID(11));
        pathObj.SetBoundary(10, 10, 100, 50)
            .SetFill(true)
            .SetStroke(false)
            .SetFillColor(Ofd.Core.PageDescription.Color.Color.CT_Color.Rgb(255, 0, 0));
        pathObj.SetAbbreviatedData(new AbbreviatedData()
            .M(10, 10).L(110, 10).L(110, 60).L(10, 60).C());
        block.AddPageBlock(pathObj);
        bgLayer.AddPageBlock(block);

        // 正文层：文字对象 + 图像对象 + 复合对象
        var bodyLayer = new CT_Layer().SetType(LayerType.Body).SetDrawParam(ST_RefID.GetInstance(20));
        bodyLayer.SetObjID(12);

        var textObj = new TextObject(new ST_ID(13));
        textObj.SetBoundary(20, 20, 80, 10)
            .SetFont(ST_RefID.GetInstance(1))
            .SetSize(3.5)
            .SetFillColor(Ofd.Core.PageDescription.Color.Color.CT_Color.Rgb(0, 0, 0));
        textObj.AddTextCode(new TextCode().SetCoordinate(20, 25).SetContent("测试文本").SetDeltaX(3.5, 3.5, 3.5, 3.5));
        textObj.AddCGTransform(new CT_CGTransform().SetCodePosition(0).SetCodeCount(1).SetGlyphCount(1)
            .SetGlyphs(new ST_Array(0, 1, 2, 3)));

        var imgObj = new ImageObject(new ST_ID(14));
        imgObj.SetBoundary(0, 0, 50, 50).SetResourceID(ST_RefID.GetInstance(30));
        imgObj.SetSubstitution(ST_RefID.GetInstance(31));
        imgObj.SetBorder(new Border().SetLineWidth(1).SetHorizonalCornerRadius(2));

        var compObj = new CompositeObject(new ST_ID(15));
        compObj.SetBoundary(100, 100, 200, 100).SetResourceID(ST_RefID.GetInstance(40));

        bodyLayer.AddPageBlock(textObj).AddPageBlock(imgObj).AddPageBlock(compObj);

        // 前景层：带裁剪的图形
        var fgLayer = new CT_Layer().SetType(LayerType.Foreground);
        fgLayer.SetObjID(16);
        var clippedPath = new PathObject(new ST_ID(17));
        clippedPath.SetBoundary(0, 0, 100, 100);
        clippedPath.SetFill(true);
        clippedPath.SetAbbreviatedData(new AbbreviatedData().M(0, 0).L(100, 0).L(100, 100).C());
        clippedPath.SetClips(new Clips().AddClip(new CT_Clip().AddArea(new Area()
            .SetClipObj(new Ofd.Core.Graph.PathObj.CT_Path().SetBoundary(10, 10, 50, 50)))));
        fgLayer.AddPageBlock(clippedPath);

        var content = new PageContent().AddLayer(bgLayer).AddLayer(bodyLayer).AddLayer(fgLayer);
        return new PageObjType().SetContent(content)
            .AddTemplate(new Template().SetTemplateID(ST_RefID.GetInstance(5)).SetZOrder(LayerType.Background))
            .AddPageRes(new ST_Loc("Res/PageRes.xml"));
    }

    [Fact]
    public void Page_content_tree_round_trip()
    {
        var page = BuildPage();
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(page.Proxy));
        var parsed = new PageObjType(DomParser.Parse(xml));

        var content = parsed.GetContent()!;
        Assert.Equal(3, content.GetLayers().Count);

        // 层排序：背景(1) → 正文(3) → 前景(5)
        var ordered = content.GetOrderedLayers();
        Assert.Equal(LayerType.Background, ordered[0].GetType());
        Assert.Equal(LayerType.Body, ordered[1].GetType());
        Assert.Equal(LayerType.Foreground, ordered[2].GetType());
        Assert.Equal(12, ordered[1].GetObjID()!.Id);
        Assert.Equal(20, ordered[1].GetDrawParam()!.GetRefId()!.Id);

        // 背景层：嵌套页块中的图形对象
        var bgBlocks = ordered[0].GetPageBlocks();
        var nested = Assert.IsType<CT_PageBlock>(bgBlocks[0]);
        var pathObj = Assert.IsType<PathObject>(nested.GetPageBlocks()[0]);
        Assert.Equal(11, pathObj.GetID()!.Id);
        Assert.Equal("10 10 100 50", pathObj.GetBoundary()!.ToString());
        Assert.True(pathObj.GetFill());
        Assert.False(pathObj.GetStroke());
        Assert.Equal("255 0 0", pathObj.GetFillColor()!.GetValue()!.ToString());
        Assert.Equal("M 10 10 L 110 10 L 110 60 L 10 60 C", pathObj.GetAbbreviatedDataText());

        // 正文层：文字/图像/复合对象
        var bodyBlocks = ordered[1].GetPageBlocks();
        var textObj = Assert.IsType<TextObject>(bodyBlocks[0]);
        Assert.Equal(13, textObj.GetID()!.Id);
        Assert.Equal(1, textObj.GetFont()!.GetRefId()!.Id);
        Assert.Equal(3.5, textObj.GetSize());
        var textCode = Assert.Single(textObj.GetTextCodes());
        Assert.Equal("测试文本", textCode.GetContent());
        Assert.Equal(20, textCode.GetX());
        Assert.Equal(25, textCode.GetY());
        Assert.Equal("3.500 3.500 3.500 3.500", textCode.GetDeltaX()!.ToString()); // fmt 3 位
        var cg = Assert.Single(textObj.GetCGTransforms());
        Assert.Equal(0, cg.GetCodePosition());
        Assert.Equal("0 1 2 3", cg.GetGlyphs()!.ToString());

        var imgObj = Assert.IsType<ImageObject>(bodyBlocks[1]);
        Assert.Equal(30, imgObj.GetResourceID()!.GetRefId()!.Id);
        Assert.Equal(31, imgObj.GetSubstitution()!.GetRefId()!.Id);
        var border = imgObj.GetBorder()!;
        Assert.Equal(1, border.GetLineWidth());
        Assert.Equal(2, border.GetHorizonalCornerRadius());
        Assert.Equal(0.353, new Border().GetLineWidth()); // 缺省边框线宽 0.353mm

        var compObj = Assert.IsType<CompositeObject>(bodyBlocks[2]);
        Assert.Equal(40, compObj.GetResourceID()!.GetRefId()!.Id);

        // 前景层：裁剪
        var fgBlocks = ordered[2].GetPageBlocks();
        var clipped = Assert.IsType<PathObject>(fgBlocks[0]);
        var clips = clipped.GetClips()!;
        var clip = Assert.Single(clips.GetClips());
        var area = Assert.Single(clip.GetAreas());
        Assert.IsType<Ofd.Core.Graph.PathObj.CT_Path>(area.GetClipObj());

        // 页级：模板 + 页资源
        var tpl = Assert.Single(parsed.GetTemplates());
        Assert.Equal(5, tpl.GetTemplateID()!.GetRefId()!.Id);
        Assert.Equal(LayerType.Background, tpl.GetZOrder());
        Assert.Equal("Res/PageRes.xml", Assert.Single(parsed.GetPageResList())!.ToString());
    }

    [Fact]
    public void Content_requires_layer_id()
    {
        Assert.Throws<ArgumentException>(() => new PageContent().AddLayer(new CT_Layer()));
        var layer = new CT_Layer().SetType(LayerType.Body);
        layer.SetObjID(1);
        var ok = new PageContent().AddLayer(layer);
        Assert.Single(ok.GetLayers());
    }

    [Fact]
    public void AbbreviatedData_build_serialize_parse_cycle()
    {
        var data = new AbbreviatedData()
            .S(0, 0)
            .M(1, 1)
            .L(2, 2)
            .Q(3, 3, 4, 4)
            .B(5, 5, 6, 6, 7, 7)
            .A(10, 5, 30, 1, 0, 8, 8)
            .C();
        string s = data.ToString();
        Assert.Equal("S 0 0 M 1 1 L 2 2 Q 3 3 4 4 B 5 5 6 6 7 7 A 10 5 30 1 0 8 8 C", s);
        Assert.Equal(7, data.Size());

        // 解析回环
        var parsed = AbbreviatedData.Parse(s);
        Assert.Equal(7, parsed.Count);
        Assert.Equal(s, string.Join(" ", parsed.Select(v => v.ToString())));
        Assert.Equal("M", parsed[1].GetOpt());
        Assert.Equal("A", parsed[5].GetOpt());
        Assert.Equal(new double[] { 10, 5, 30, 1, 0, 8, 8 }, parsed[5].ExpectValues());

        // A 指令参数校验
        Assert.Throws<ArgumentException>(() => data.A(1, 1, 0, 2, 0, 0, 0));

        // 撤销
        data.Undo();
        Assert.Equal(6, data.Size());

        // Clone 独立
        var clone = data.Clone();
        clone.L(9, 9);
        Assert.Equal(6, data.Size());
    }

    [Fact]
    public void TextCode_delta_g_syntax_expands()
    {
        // "1 g 3 2" 表示 1，然后 2 重复 3 次 → "1 2 2 2"
        var tc = new TextCode();
        tc.Proxy.SetAttr("DeltaX", "1 g 3 2");
        Assert.Equal("1 2 2 2", tc.GetDeltaX()!.ToString());
        // 无 g 时原样
        tc.Proxy.SetAttr("DeltaY", "1 2 3");
        Assert.Equal("1 2 3", tc.GetDeltaY()!.ToString());
    }

    [Fact]
    public void GraphicUnit_defaults_and_clamps()
    {
        var img = new ImageObject(new ST_ID(50));
        Assert.True(img.GetVisible());          // 缺省可见
        Assert.Equal(255, img.GetAlpha());      // 缺省不透明
        Assert.Equal(3.528, img.GetMiterLimit());
        Assert.Equal(Ofd.Core.PageDescription.DrawParam.LineCapType.Butt, img.GetCap());
        img.SetAlpha(-10);                      // 负值钳制为 255（Java 同）
        Assert.Equal(255, img.GetAlpha());
        img.SetAlpha(100);
        Assert.Equal(100, img.GetAlpha());
        img.SetVisible(false);
        Assert.False(img.GetVisible());
    }

    [Fact]
    public void VectorG_with_composite_content()
    {
        var vectorG = new CT_VectorG()
            .SetID(new ST_ID(40))
            .SetWidth(200)
            .SetHeight(100);
        var vPath = new PathObject(new ST_ID(41));
        vPath.SetBoundary(0, 0, 10, 10);
        vectorG.AddContent(vPath);
        var vText = new TextObject(new ST_ID(42));
        vText.SetFont(ST_RefID.GetInstance(1));
        vText.SetSize(2);
        vectorG.AddContent(vText);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(vectorG.Proxy));
        var parsed = new CT_VectorG(DomParser.Parse(xml));
        Assert.Equal(200, parsed.GetWidth());
        Assert.Equal(100, parsed.GetHeight());
        var content = parsed.GetContent();
        Assert.Equal(2, content.GetPageBlocks().Count);
        Assert.IsType<PathObject>(content.GetPageBlocks()[0]);
        Assert.IsType<TextObject>(content.GetPageBlocks()[1]);

        // CompositeGraphicUnits 容器 + Res 工厂分派
        var units = new CompositeGraphicUnits().AddCompositeGraphicUnit(parsed);
        var res = new Ofd.Core.BasicStructure.Res.Res().AddResource(units);
        Assert.IsType<CompositeGraphicUnits>(res.GetResources()[0]);
    }

    [Fact]
    public void Clip_requires_clipable_object()
    {
        var clip = new CT_Clip();
        var textArea = new Area().SetClipObj(new Ofd.Core.Text.Text.CT_Text()); // Text 可作为裁剪对象
        clip.AddArea(textArea);
        Assert.IsType<Ofd.Core.Text.Text.CT_Text>(clip.GetAreas()[0].GetClipObj());
    }

}
