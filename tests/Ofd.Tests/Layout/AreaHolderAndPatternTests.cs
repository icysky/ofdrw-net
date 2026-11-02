using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.Color.Pattern;
using Ofd.Layout.Element;
using Ofd.Layout.Element.Canvas;
using Xunit;

namespace Ofd.Tests.Layout;

/// <summary>区域占位区块、裁剪构造器、Canvas 底纹。</summary>
public class AreaHolderAndPatternTests
{
    [Fact]
    public void Area_holder_block_keeps_given_size()
    {
        var block = new AreaHolderBlock("sign-area", 40d, 20d);
        Assert.Equal("sign-area", block.GetAreaName());
        Assert.Equal("AreaHolderBlock", block.ElementType());

        // 不接受宽度重设：直接返回盒式尺寸（含 margin/padding/border）
        block.SetMargin(1).SetPadding(2).SetBorder(0.5);
        var rect = block.DoPrepare(1); // 限制值被忽略

        Assert.Equal(40 + block.WidthPlus(), rect.GetWidth(), 6);
        Assert.Equal(20 + block.HeightPlus(), rect.GetHeight(), 6);
        Assert.Equal(40d, block.GetWidth());

        // 绝对定位构造
        var abs = new AreaHolderBlock("abs-area", 1, 2, 30, 40);
        Assert.Equal("abs-area", abs.GetAreaName());
        Assert.Equal(1d, abs.GetX());
        Assert.Equal(2d, abs.GetY());
        Assert.Equal(30d, abs.GetWidth());

        abs.SetAreaName("renamed");
        Assert.Equal("renamed", abs.GetAreaName());
    }

    [Fact]
    public void Clip_factory_builds_clip_with_boundary_and_ctm()
    {
        var data = new AbbreviatedData();
        var ctm = new ST_Array(1, 0, 0, 1, 5, 6);

#pragma warning disable CS0618 // ClipFactory 标记为弃用（与上游一致）
        var factory = new ClipFactory(data, ctm);
        var clips = factory.Clips(new ST_Box(0, 0, 100, 200));
#pragma warning restore CS0618

        var clipList = clips.GetClips();
        Assert.Single(clipList);

        var areas = clipList[0].GetAreas();
        Assert.Single(areas);
        // CTM 沿用 Java Double.toString 语义（保留 .0）
        Assert.Equal("1.0 0.0 0.0 1.0 5.0 6.0", areas[0].GetCTM()!.ToString());

        var path = areas[0].GetClipObj() as CT_Path;
        Assert.NotNull(path);
        Assert.Equal("0 0 100 200", path!.GetBoundary()!.ToString());

        // 默认边界为 A4
#pragma warning disable CS0618
        var defaultClips = new ClipFactory(data).Clips();
#pragma warning restore CS0618
        var defaultPath = defaultClips.GetClips()[0].GetAreas()[0].GetClipObj() as CT_Path;
        Assert.Equal("0 0 210 297", defaultPath!.GetBoundary()!.ToString());

        // 克隆独立
#pragma warning disable CS0618
        var clone = factory.Clone();
#pragma warning restore CS0618
        Assert.NotSame(factory.GetCtm(), clone.GetCtm());
        Assert.Equal(factory.GetCtm()!.ToString(), clone.GetCtm()!.ToString());
    }

    [Fact]
    public void Canvas_pattern_maps_repetition_and_size()
    {
        var imgObj = new ImageObject(1);
        imgObj.SetBoundary(new ST_Box(0, 0, 10, 20));

        var pattern = new CanvasPattern(Path.Combine(Path.GetTempPath(), "a.png"), "row", imgObj);

        Assert.Equal(10d, pattern.Pattern.GetWidth(), 6);
        Assert.Equal(20d, pattern.Pattern.GetHeight(), 6);
        Assert.Equal(ReflectMethod.Row, pattern.Pattern.GetReflectMethod());
        Assert.Single(pattern.Pattern.GetCellContent().GetPageBlocks());

        // 其余重复样式
        Assert.Equal(ReflectMethod.Column,
            new CanvasPattern("b.png", "column", imgObj).Pattern.GetReflectMethod());
        Assert.Equal(ReflectMethod.RowAndColumn,
            new CanvasPattern("c.png", "row-column", imgObj).Pattern.GetReflectMethod());

        // repeat / 空串为默认 normal（不设置属性）
        var normal = new CanvasPattern("d.png", "repeat", imgObj);
        Assert.Equal(ReflectMethod.Normal, normal.Pattern.GetReflectMethod());

        Assert.Throws<ArgumentException>(() => new CanvasPattern("e.png", "repeat-x", imgObj));
    }

    [Fact]
    public void Canvas_pattern_transform_and_image_size()
    {
        var imgObj = new ImageObject(2);
        imgObj.SetBoundary(new ST_Box(0, 0, 10, 20));

        var pattern = new CanvasPattern("f.png", "repeat", imgObj);
        Assert.EndsWith("f.png", pattern.Img);

        pattern.SetTransform(new double[] { 1, 0, 0, 1, 2, 3 });
        Assert.Equal("1.0 0.0 0.0 1.0 2.0 3.0", pattern.Pattern.GetCTM()!.ToString());

        Assert.Throws<ArgumentException>(() => pattern.SetTransform(new double[] { 1, 2 }));

        pattern.SetImageSize(30, 40);
        Assert.Equal(30d, pattern.Pattern.GetWidth(), 6);
        Assert.Equal(40d, pattern.Pattern.GetHeight(), 6);
        Assert.Equal("0 0 30 40", imgObj.GetBoundary()!.ToString());
        Assert.Equal("30.0 0.0 0.0 40.0 0.0 0.0", imgObj.GetCTM()!.ToString());
    }
}
