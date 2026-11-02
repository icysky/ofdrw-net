using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Engine;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>流式布局分析：分页、浮动定位、可拆分元素切分、剩余空间填充。</summary>
public class StreamingLayoutTests
{
    private static Segment Seg(params Div[] divs)
    {
        var s = new Segment(210);
        foreach (var d in divs) s.TryAdd(d);
        return s;
    }

    private static Div Block(double w, double h, bool integrity = true) =>
        new Div(w, h).SetClear(Clear.Both).SetIntegrity(integrity);

    private static Div Inline(double w, double h, AFloat f, Clear c = Clear.None) =>
        new Div(w, h).SetClear(c).SetFloat(f);

    [Fact]
    public void Fitting_segment_is_positioned_on_first_page()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var div = Block(100, 50);

        var pages = analyzer.Analyze(new List<Segment> { Seg(div) });

        var page = Assert.Single(pages);
        var placed = Assert.Single(page.GetContent());
        Assert.Same(div, placed);
        Assert.Equal(Position.Absolute, placed.GetPosition());
        Assert.Equal(0d, placed.GetX()!.Value, 6);
        Assert.Equal(0d, placed.GetY()!.Value, 6);
    }

    [Fact]
    public void Left_and_right_floats_are_positioned_at_edges()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var left = Inline(40, 10, AFloat.Left);
        var right = Inline(30, 10, AFloat.Right);

        var pages = analyzer.Analyze(new List<Segment> { Seg(left, right) });

        Assert.Single(pages);
        Assert.Equal(0d, left.GetX()!.Value, 6);
        Assert.Equal(210d - 30d, right.GetX()!.Value, 6);
    }

    [Fact]
    public void Center_floats_are_centered_in_segment()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var a = Inline(40, 10, AFloat.Center);
        var b = Inline(60, 10, AFloat.Center);

        var pages = analyzer.Analyze(new List<Segment> { Seg(a, b) });

        Assert.Single(pages);
        // 总宽 100 → 起始 (210-100)/2 = 55
        Assert.Equal(55d, a.GetX()!.Value, 6);
        Assert.Equal(95d, b.GetX()!.Value, 6);
    }

    [Fact]
    public void Non_blockable_overflow_starts_new_page()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var d1 = Block(100, 200);
        var d2 = Block(100, 200);

        var pages = analyzer.Analyze(new List<Segment> { Seg(d1), Seg(d2) });

        Assert.Equal(2, pages.Count);
        Assert.Single(pages[0].GetContent());
        Assert.Single(pages[1].GetContent());
        Assert.Equal(0d, d2.GetY()!.Value, 6); // 新页从 0 开始
    }

    [Fact]
    public void Blockable_segment_is_split_across_pages()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var d1 = Block(100, 200);              // 占用 200，剩余 97
        var d2 = Block(100, 200, integrity: false); // 可拆分

        var pages = analyzer.Analyze(new List<Segment> { Seg(d1), Seg(d2) });

        Assert.Equal(2, pages.Count);

        var first = pages[0].GetContent();
        Assert.Equal(2, first.Count);
        Assert.Equal(97d, first[1].GetHeight()!.Value, 6);

        var second = pages[1].GetContent();
        Assert.Single(second);
        Assert.Equal(103d, second[0].GetHeight()!.Value, 6);
    }

    [Fact]
    public void Remain_area_filler_forces_page_break()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var fillerSegment = Seg(new PageAreaFiller());
        var div = Block(100, 50);

        var pages = analyzer.Analyze(new List<Segment> { fillerSegment, Seg(div) });

        Assert.Equal(2, pages.Count);
        // 填充段本身不产生内容
        Assert.Empty(pages[0].GetContent());
        Assert.Single(pages[1].GetContent());
    }

    [Fact]
    public void Relative_position_is_applied_before_absolute()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4().SetMargin(0));
        var div = Block(100, 50);
        div.SetPosition(Position.Relative).SetLeft(5).SetRight(2).SetTop(3);

        var pages = analyzer.Analyze(new List<Segment> { Seg(div) });

        var placed = Assert.Single(pages[0].GetContent());
        Assert.Equal(Position.Absolute, placed.GetPosition());
        Assert.Equal(3d, placed.GetX()!.Value, 6);  // 0 + left5 - right2
        Assert.Equal(3d, placed.GetY()!.Value, 6);  // 0 + top3
    }

    [Fact]
    public void Empty_input_returns_no_pages()
    {
        var analyzer = new StreamingLayoutAnalyzer(PageLayout.A4());
        Assert.Empty(analyzer.Analyze(null));
        Assert.Empty(analyzer.Analyze(new List<Segment>()));
    }
}
