using Ofd.Font;
using Ofd.Layout.Element;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>段落布局：行切分、首行缩进、尺寸推导、分页切分。</summary>
public class ParagraphTests
{
    private static Paragraph FourPlaceholderLines(double width = 20, double holdWidth = 20, double height = 5)
    {
        var p = new Paragraph(width, null);
        for (int i = 0; i < 4; i++) p.Add(new PlaceholderSpan(holdWidth, height));
        return p;
    }

    [Fact]
    public void Paragraph_defaults_match_java()
    {
        var p = new Paragraph("文本");

        Assert.Equal(Clear.Both, p.GetClear());
        Assert.Equal(2d, p.GetLineSpace());
        Assert.Equal(TextAlign.Left, p.GetTextAlign());
        Assert.Null(p.GetFirstLineIndent());
        Assert.Null(p.GetFirstLineIndentWidth());
        Assert.Null(p.GetDefaultFont());
        Assert.Single(p.GetContents());
        Assert.Empty(p.GetLines());
        Assert.Equal("Paragraph", p.ElementType());

        Assert.Throws<ArgumentException>(() => new Paragraph((string)null!));
    }

    [Fact]
    public void Paragraph_add_text_applies_default_style()
    {
        var p = new Paragraph().SetFontSize(12).SetDefaultFont(Ofd.Font.FontName.SimHei.Font());
        p.Add("abc");

        var span = p.GetContents().First!.Value;
        Assert.Equal("abc", span.GetText());
        Assert.Equal(12d, span.GetFontSize());
        Assert.Equal("黑体", span.GetFont().GetName());

        // 后设置字号 + refreshBeforeAdd 生效
        p.SetFontSize(8, true);
        Assert.Equal(8d, p.GetContents().First.Value.GetFontSize());

        // null 文本被忽略
        p.Add((string?)null);
        Assert.Single(p.GetContents());
    }

    [Fact]
    public void Paragraph_layout_splits_lines_and_derives_size()
    {
        var p = FourPlaceholderLines();
        var rect = p.DoPrepare(100);

        // 每行恰好容纳一个 20mm 占位符 → 4 行
        Assert.Equal(4, p.GetLines().Count);
        Assert.All(p.GetLines(), l => Assert.Equal(20d, l.GetWidth(), 6));

        // 行高 = 5 + 行间距 2 = 7
        Assert.Equal(4 * 7d + 0.0001, p.GetHeight()!.Value, 4);
        Assert.Equal(20d, p.GetWidth()!.Value, 6);
        Assert.Equal(p.GetWidth()!.Value, rect.GetWidth(), 6);
        Assert.Equal(p.GetHeight()!.Value, rect.GetHeight(), 4);
    }

    [Fact]
    public void Paragraph_width_null_uses_longest_line()
    {
        var p = new Paragraph();
        p.Add(new PlaceholderSpan(8d, 5d));
        p.DoPrepare(100);

        Assert.Equal(8d, p.GetWidth()!.Value, 6);
        Assert.Single(p.GetLines());
    }

    [Fact]
    public void Paragraph_preset_height_is_kept()
    {
        var p = new Paragraph(20, 99);
        p.Add(new PlaceholderSpan(20d, 5d));
        p.DoPrepare(100);

        Assert.Equal(99d, p.GetHeight()!.Value, 6);
    }

    [Fact]
    public void Paragraph_width_limit_shrinks_line_width()
    {
        var p = new Paragraph();           // 未设置宽度
        p.Add(new PlaceholderSpan(30d, 5d));

        // 限制 25 → 行宽 25，占位符 30 不可分割且超宽 → 被忽略，无有效行
        p.DoPrepare(25);
        Assert.Empty(p.GetLines());
        Assert.Equal(0d + 0.0001, p.GetHeight()!.Value, 4);
    }

    [Fact]
    public void Paragraph_first_line_indent_inserts_placeholder()
    {
        var p = new Paragraph(50, null).SetFirstLineIndent(2);
        p.Add(new Span("abc").SetFontSize(5));

        p.DoPrepare(100);

        var firstInline = p.GetLines().First!.Value.GetInlineSpans()[0];
        var holder = Assert.IsType<PlaceholderSpan>(firstInline);
        Assert.Equal(2, holder.GetHoldNum());
        Assert.Equal(2 * 5d, holder.GetHoldWidth(), 6); // 数量 × (字号 + 字间距)

        // 源内容不被修改（缩进只作用于布局副本）
        Assert.IsType<Span>(p.GetContents().First!.Value);
        Assert.IsNotType<PlaceholderSpan>(p.GetContents().First.Value);

        // 清除缩进
        p.ClearFirstLineIndent();
        Assert.Null(p.GetFirstLineIndent());

        // 指定缩进宽度优先于字符数
        var p2 = new Paragraph(50, null).SetFirstLineIndent(2).SetFirstLineIndentWidth(7.5);
        p2.Add(new Span("abc").SetFontSize(5));
        p2.DoPrepare(100);
        var holder2 = Assert.IsType<PlaceholderSpan>(p2.GetLines().First!.Value.GetInlineSpans()[0]);
        Assert.Equal(7.5, holder2.GetHoldWidth(), 6);
    }

    [Fact]
    public void Paragraph_split_by_height_distributes_lines()
    {
        var p = FourPlaceholderLines();
        p.DoPrepare(100);
        Assert.Equal(4, p.GetLines().Count);

        // 每行 7mm，总高 28.0001；切分高度 14 → 前两行留在本段
        var parts = p.Split(14);

        Assert.Equal(2, parts.Length);
        var p1 = Assert.IsType<Paragraph>(parts[0]);
        var p2 = Assert.IsType<Paragraph>(parts[1]);

        Assert.Equal(2, p1.GetLines().Count);
        Assert.Equal(2, p2.GetLines().Count);
        Assert.Equal(14d, p1.GetHeight()!.Value, 6);
        Assert.Equal(14d, p2.GetHeight()!.Value, 6);
        Assert.Equal(0d, p2.GetMarginTop(), 6);
    }

    [Fact]
    public void Paragraph_split_when_not_enough_space_for_one_line()
    {
        var p = FourPlaceholderLines();
        p.DoPrepare(100);

        // 切分高度 3 < 单行 7 → 整体挪到下一段，本段用占位符
        var parts = p.Split(3);

        Assert.Equal(2, parts.Length);
        var placeholder = parts[0];
        Assert.True(placeholder.IsPlaceholder());
        Assert.Equal(p.GetWidth()!.Value + p.WidthPlus(), placeholder.GetWidth()!.Value, 6);
        Assert.Same(p, parts[1]);
    }

    [Fact]
    public void Paragraph_clone_has_style_but_no_content()
    {
        var p = FourPlaceholderLines().SetLineSpace(3).SetFontSize(9).SetTextAlign(TextAlign.Center);
        p.DoPrepare(100);

        var clone = (Paragraph)p.Clone();

        Assert.Equal(3d, clone.GetLineSpace());
        Assert.Equal(9d, clone.GetFontSize());
        Assert.Equal(TextAlign.Center, clone.GetTextAlign());
        Assert.Empty(clone.GetContents());
        Assert.Empty(clone.GetLines());
        Assert.Equal(20d, clone.GetWidth()!.Value, 6);
    }

    [Fact]
    public void Br_and_page_area_filler_flags()
    {
        var br = new BR();
        Assert.IsAssignableFrom<Div>(br);

        var filler = new PageAreaFiller();
        Assert.True(filler.IsPlaceholder());
        Assert.Equal(Clear.Both, filler.GetClear());
        Assert.Equal(AFloat.Center, filler.GetFloat());
    }
}
