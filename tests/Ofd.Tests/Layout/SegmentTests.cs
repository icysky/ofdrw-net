using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Layout.Engine;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>段与分段引擎：独占/浮动规则、可拆分标记、换行与绝对定位处理。</summary>
public class SegmentTests
{
    private static Div Block(double w = 30, double h = 10) => new Div(w, h).SetClear(Clear.Both);

    private static Div Inline(double w = 30, double h = 10, AFloat f = AFloat.Left, Clear c = Clear.None) =>
        new Div(w, h).SetClear(c).SetFloat(f);

    [Fact]
    public void Segment_adds_block_element_and_exhausts_width()
    {
        var seg = new Segment(100);

        Assert.True(seg.IsEmpty());
        Assert.True(seg.TryAdd(null));
        Assert.True(seg.IsEmpty());

        Assert.True(seg.TryAdd(Block()));
        Assert.False(seg.IsEmpty());
        Assert.Equal(10d, seg.GetHeight(), 6);
        Assert.Equal(100d, seg.GetWidth(), 6);

        // 独占元素耗尽宽度后无法再加入
        Assert.False(seg.TryAdd(Block(10, 5)));
        Assert.False(seg.TryAdd(Inline(1, 1)));
    }

    [Fact]
    public void Segment_shrinks_element_that_exceeds_width()
    {
        var seg = new Segment(100);
        // 与 Java 一致：DoPrepare 会把超宽元素收缩到段宽后再加入
        Assert.True(seg.TryAdd(Block(120, 10)));
        Assert.Equal(100d, seg.GetContent()[0].GetWidth()!.Value, 6);
        Assert.Single(seg.GetContent());
    }

    [Fact]
    public void Segment_inline_float_rules()
    {
        var seg = new Segment(100);

        Assert.True(seg.TryAdd(Inline()));
        Assert.False(seg.IsCenterFloat());

        // 已有左浮动 → 再加入 左浮动 + clear left 被拒绝
        Assert.False(seg.TryAdd(Inline(20, 10, AFloat.Left, Clear.Left)));

        // 段内非居中布局时，居中元素被拒绝
        Assert.False(seg.TryAdd(Inline(20, 10, AFloat.Center)));

        // 普通左浮动可继续加入
        Assert.True(seg.TryAdd(Inline(20, 10, AFloat.Left)));
        Assert.Equal(2, seg.GetContent().Count);
        Assert.Equal(10d * 2, seg.GetContent().Sum(_ => 10d), 6);
    }

    [Fact]
    public void Segment_right_float_duplicate_rejected()
    {
        var seg = new Segment(100);
        Assert.True(seg.TryAdd(Inline(30, 10, AFloat.Right, Clear.Right)));
        Assert.False(seg.TryAdd(Inline(20, 10, AFloat.Right, Clear.Right)));
    }

    [Fact]
    public void Segment_center_layout_rules()
    {
        var seg = new Segment(100);

        // 首个居中元素
        Assert.True(seg.TryAdd(Inline(30, 10, AFloat.Center)));
        Assert.True(seg.IsCenterFloat());

        // 居中布局中：非居中元素被拒绝
        Assert.False(seg.TryAdd(Inline(20, 10, AFloat.Left)));

        // clear left 的居中元素被拒绝
        Assert.False(seg.TryAdd(Inline(20, 10, AFloat.Center, Clear.Left)));

        // 普通居中元素可加入
        Assert.True(seg.TryAdd(Inline(20, 10, AFloat.Center)));
        Assert.Equal(2, seg.GetContent().Count);
    }

    [Fact]
    public void Segment_marks_blockable_and_remain_filler()
    {
        var seg = new Segment(100);

        // integrity = true（图片等不可拆分）→ 段不可拆分
        var fixedDiv = new Div(10, 10).SetClear(Clear.None).SetFloat(AFloat.Left).SetIntegrity(true);
        Assert.True(seg.TryAdd(fixedDiv));
        Assert.False(seg.IsBlockable());

        // 加入可拆分元素 → 段可拆分
        var splittable = new Div(10, 10).SetClear(Clear.None).SetFloat(AFloat.Left).SetIntegrity(false);
        Assert.True(seg.TryAdd(splittable));
        Assert.True(seg.IsBlockable());

        // PageAreaFiller → 剩余空间填充段
        var filler = new PageAreaFiller();
        var seg2 = new Segment(100);
        Assert.True(seg2.TryAdd(filler));
        Assert.True(seg2.IsRemainAreaFiller());
    }

    [Fact]
    public void Segment_enumerates_content_with_sizes()
    {
        var seg = new Segment(100);
        seg.TryAdd(Inline(30, 10));
        seg.TryAdd(Inline(20, 5));

        var pairs = seg.ToList();
        Assert.Equal(2, pairs.Count);
        Assert.Same(seg.GetContent()[0], pairs[0].Key);
        Assert.Equal(30d, pairs[0].Value.GetWidth(), 6);
        Assert.Equal(5d, pairs[1].Value.GetHeight(), 6);

        // 重复枚举稳定
        Assert.Equal(2, seg.Count());
    }

    [Fact]
    public void Segmentation_splits_block_elements()
    {
        var engine = new SegmentationEngine(new PageLayout(100, 200).SetMargin(0));

        var segments = engine.Process(new List<Div> { Block(50, 10), Block(50, 20) });

        Assert.Equal(2, segments.Count);
        Assert.Equal(10d, segments[0].GetHeight(), 6);
        Assert.Equal(20d, segments[1].GetHeight(), 6);
        Assert.All(segments, s => Assert.Equal(100d, s.GetWidth(), 6));
    }

    [Fact]
    public void Segmentation_packs_inline_elements_until_full()
    {
        var engine = new SegmentationEngine(new PageLayout(100, 200).SetMargin(0));

        var segments = engine.Process(new List<Div>
        {
            Inline(40, 10),
            Inline(40, 12),
            Inline(40, 15),
        });

        Assert.Equal(2, segments.Count);
        Assert.Equal(2, segments[0].GetContent().Count);
        Assert.Equal(12d, segments[0].GetHeight(), 6); // 取最高
        Assert.Single(segments[1].GetContent());
    }

    [Fact]
    public void Segmentation_handles_br_and_absolute()
    {
        var engine = new SegmentationEngine(new PageLayout(100, 200).SetMargin(0));

        // 绝对定位元素被跳过
        var abs = new Div(1, 2, 30, 10); // x,y,w,h → Absolute
        var segments = engine.Process(new List<Div> { abs, Inline(20, 10) });
        Assert.Single(segments);
        Assert.Single(segments[0].GetContent());

        // BR 结束当前段
        var withBr = engine.Process(new List<Div> { Inline(20, 10), new BR(), Inline(20, 10) });
        Assert.Equal(2, withBr.Count);

        // 空段前的 BR 被忽略
        var leadingBr = engine.Process(new List<Div> { new BR(), Inline(20, 10) });
        Assert.Single(leadingBr);

        // 空输入
        Assert.Empty(engine.Process(null));
        Assert.Empty(engine.Process(new List<Div>()));
    }
}
