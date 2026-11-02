using Ofd.Layout;
using Ofd.Layout.Element;
using Xunit;
using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;

namespace Ofd.Tests.Layout;

/// <summary>虚拟页面：绝对定位约束、图层分组、页码与模板。</summary>
public class VirtualPageTests
{
    private static Div Absolute(double x = 1, double y = 2, double w = 30, double h = 10,
        LayerType layer = LayerType.Body) =>
        new Div(x, y, w, h).SetLayer(layer);

    [Fact]
    public void Virtual_page_constructors()
    {
        var byStyle = new VirtualPage(PageLayout.A4());
        Assert.Equal(210d, byStyle.GetStyle()!.GetWidth());
        Assert.Null(byStyle.GetPageNum());
        Assert.Empty(byStyle.GetContent());
        Assert.Empty(byStyle.GetTemplates());

        var bySize = new VirtualPage(100, 50);
        Assert.Equal(100d, bySize.GetStyle()!.GetWidth());
        Assert.Equal(50d, bySize.GetStyle()!.GetHeight());

        // 空页面不产生图层分组
        Assert.Empty(bySize.GetLayerContent());
    }

    [Fact]
    public void Virtual_page_add_validations()
    {
        var page = new VirtualPage(PageLayout.A4());

        // 空元素忽略
        page.Add(null);
        Assert.Empty(page.GetContent());

        // 非绝对定位
        Assert.Throws<ArgumentException>(() => page.Add(new Div(30, 10)));

        // 缺少坐标
        var noXY = new Div().SetPosition(Position.Absolute).SetWidth(30).SetHeight(10);
        Assert.Throws<ArgumentException>(() => page.Add(noXY));

        // 缺少宽度
        var noWidth = new Div().SetPosition(Position.Absolute).SetX(1).SetY(2).SetHeight(10);
        Assert.Throws<ArgumentException>(() => page.Add(noWidth));
    }

    [Fact]
    public void Virtual_page_adds_absolute_element_and_prepares()
    {
        var page = new VirtualPage(PageLayout.A4());
        var div = Absolute();
        div.SetMargin(1).SetPadding(2);

        page.Add(div);

        var stored = Assert.Single(page.GetContent());
        Assert.Same(div, stored);
        Assert.Equal(30d, div.GetWidth()!.Value, 6);

        // AddUnsafe 不做校验
        page.AddUnsafe(new Div(10, 10));
        Assert.Equal(2, page.GetContent().Count);
    }

    [Fact]
    public void Virtual_page_groups_by_layer()
    {
        var page = new VirtualPage(PageLayout.A4());
        var fg = Absolute(layer: LayerType.Foreground);
        var body = Absolute(layer: LayerType.Body);
        var bg = Absolute(layer: LayerType.Background);

        page.Add(fg).Add(body).Add(bg);

        Assert.Single(page.GetContent(LayerType.Foreground));
        Assert.Single(page.GetContent(LayerType.Body));
        Assert.Single(page.GetContent(LayerType.Background));

        var layers = page.GetLayerContent();
        Assert.Equal(3, layers.Count);
        Assert.Same(fg, layers[0][0]);   // 前景层
        Assert.Same(body, layers[1][0]); // 正文层
        Assert.Same(bg, layers[2][0]);   // 背景层
    }

    [Fact]
    public void Virtual_page_num_and_templates()
    {
        var page = new VirtualPage(PageLayout.A4()).SetPageNum(3);
        Assert.Equal(3, page.GetPageNum());
        Assert.Throws<ArgumentException>(() => page.SetPageNum(0));

        // 模板：order 为空则不写 ZOrder
        // 模板ID为页面对象ID（OFD ST_ID 仅支持数字）
        page.AddTemplate("101", null);
        page.AddTemplate("102", LayerType.Foreground);

        Assert.Equal(2, page.GetTemplates().Count);
        Assert.NotNull(page.GetTemplates()[0].GetTemplateID());
        // 未指定 order 时不写 ZOrder 属性（默认按 Body 解释）
        Assert.Null(page.GetTemplates()[0].GetProxy().Attr("ZOrder"));
        Assert.Equal(LayerType.Body, page.GetTemplates()[0].GetZOrder());
        Assert.Equal(LayerType.Foreground, page.GetTemplates()[1].GetZOrder());

        Assert.Throws<FormatException>(() => page.AddTemplate("bad-id", null));
    }
}
