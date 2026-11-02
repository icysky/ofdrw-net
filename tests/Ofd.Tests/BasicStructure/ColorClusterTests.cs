using System.Text;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.PageDescription.Color.Pattern;
using Xunit;

namespace Ofd.Tests.BasicStructure;

/// <summary>颜色族（底纹/渐变）：8.3.3 底纹、8.3.4 轴向/径向/高洛德渐变。</summary>
public class ColorClusterTests
{
    [Fact]
    public void Color_set_and_get_cluster_types()
    {
        var axial = new CT_AxialShd()
            .SetMapType(MapType.Reflect)
            .SetMapUnit(50)
            .SetExtend(Extend._3)
            .SetStartPoint(new ST_Pos(0, 0))
            .SetEndPoint(new ST_Pos(100, 0))
            .AddSegment(new Segment(0, CT_Color.Rgb(255, 0, 0)))
            .AddSegment(new Segment(1.0, CT_Color.Rgb(0, 0, 255)));

        var color = new CT_Color().SetColor(axial);
        Assert.IsType<CT_AxialShd>(color.GetColor());
        Assert.NotNull(color.GetColorByType<CT_AxialShd>());
        Assert.Null(color.GetColorByType<CT_RadialShd>());

        // 序列化 → 回读
        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(color.Proxy));
        var parsed = new CT_Color(DomParser.Parse(xml));
        var pAxial = Assert.IsType<CT_AxialShd>(parsed.GetColor());
        Assert.Equal(MapType.Reflect, pAxial.GetMapType());
        Assert.Equal(50, pAxial.GetMapUnit());
        Assert.Equal(Extend._3, pAxial.GetExtend());
        Assert.Equal("0 0", pAxial.GetStartPoint()!.ToString());
        Assert.Equal("100 0", pAxial.GetEndPoint()!.ToString());
        var segs = pAxial.GetSegments();
        Assert.Equal(2, segs.Count);
        Assert.Equal(0, segs[0].GetPosition());
        Assert.Equal(1.0, segs[1].GetPosition());
        Assert.Equal("255 0 0", segs[0].GetColor()!.GetValue()!.ToString());

        // 切换颜色族应清除旧的
        color.SetColor(new CT_GouraudShd());
        Assert.IsType<CT_GouraudShd>(color.GetColor());
        Assert.Null(color.GetOFDElement("AxialShd"));
    }

    [Fact]
    public void RadialShd_required_fields_and_defaults()
    {
        var radial = new CT_RadialShd();
        Assert.Throws<InvalidOperationException>(() => radial.GetStartPoint());
        Assert.Throws<InvalidOperationException>(() => radial.GetEndPoint());
        Assert.Throws<InvalidOperationException>(() => radial.GetEndRadius());
        Assert.Equal(0d, radial.GetEccentricity()); // 缺省 0
        Assert.Equal(0d, radial.GetAngle());
        Assert.Equal(0d, radial.GetStartRadius());

        radial.SetStartPoint(new ST_Pos(10, 10))
            .SetEndPoint(new ST_Pos(50, 50))
            .SetEndRadius(40)
            .SetStartRadius(5)
            .SetEccentricity(0.5)
            .SetAngle(30)
            .AddSegment(new Segment(0, CT_Color.Rgb(255, 255, 255)))
            .AddSegment(new Segment(1.0, CT_Color.Rgb(0, 0, 0)));

        Assert.Equal(40, radial.GetEndRadius());
        Assert.Equal(0.5, radial.GetEccentricity());
        Assert.Equal(30, radial.GetAngle());

        // SetAngle(null) 删除属性（上游 NPE bug 已修）
        radial.SetAngle(null);
        Assert.Null(radial.Proxy.Attr("Angle"));
    }

    [Fact]
    public void GouraudShd_extend_uses_numeric_flag_and_points()
    {
        var g = new CT_GouraudShd();
        Assert.False(g.GetExtend()); // 缺省不填充
        g.SetExtend(true);
        Assert.Equal("1", g.Proxy.Attr("Extend")); // 布尔映射为 1/0（非 true/false）
        Assert.True(g.GetExtend());

        g.AddPoint(new Point().SetX(0).SetY(0).SetColor(CT_Color.Rgb(255, 0, 0)))
         .AddPoint(new Point().SetX(10).SetY(0).SetColor(CT_Color.Rgb(0, 255, 0)).SetEdgeFlag(EdgeFlag._1))
         .AddPoint(new Point().SetX(0).SetY(10).SetColor(CT_Color.Rgb(0, 0, 255)));

        Assert.Equal(3, g.GetPoints().Count);
        Assert.Equal(EdgeFlag._1, g.GetPoints()[1].GetEdgeFlag());
        Assert.Throws<InvalidOperationException>(() => new Point().GetX()); // X 必选

        g.SetBackColor(CT_Color.Rgb(9, 9, 9));
        Assert.Equal("BackColor", g.GetBackColor()!.Proxy.Name);
        g.SetBackColor(null); // 删除元素（上游误用 removeAttr）
        Assert.Null(g.GetOFDElement("BackColor"));
    }

    [Fact]
    public void LaGouraudShd_vertices_per_row()
    {
        var la = new CT_LaGouraudShd();
        Assert.Throws<InvalidOperationException>(() => la.GetVerticesPerRow());
        la.SetVerticesPerRow(3);
        Assert.Equal(3, la.GetVerticesPerRow());
        la.AddPoint(new Point().SetX(0).SetY(0).SetColor(CT_Color.Rgb(0, 0, 0)))
          .AddPoint(new Point().SetX(1).SetY(0).SetColor(CT_Color.Rgb(1, 1, 1)))
          .AddPoint(new Point().SetX(0).SetY(1).SetColor(CT_Color.Rgb(2, 2, 2)))
          .AddPoint(new Point().SetX(1).SetY(1).SetColor(CT_Color.Rgb(3, 3, 3)));
        Assert.Equal(4, la.GetPoints().Count);
        Assert.Equal("LaGouraudShd", la.Proxy.Name);
    }

    [Fact]
    public void Pattern_steps_clamp_to_cell_size_and_roundtrip()
    {
        var pattern = new CT_Pattern().SetWidth(10).SetHeight(20);
        Assert.Equal(10, pattern.GetXStep());  // 缺省为宽度
        Assert.Equal(20, pattern.GetYStep());

        pattern.SetXStep(5);                    // 小于宽度 → 按宽度
        Assert.Equal("10", pattern.Proxy.Attr("XStep"));
        pattern.SetYStep(30);                   // 大于高度 → 按设定
        Assert.Equal("30", pattern.Proxy.Attr("YStep"));

        var cell = new CellContent();
        cell.SetThumbnail(ST_RefID.GetInstance(7));
        var p = new PathObject(new ST_ID(8));
        p.SetBoundary(0, 0, 10, 10);
        cell.AddPageBlock(p);
        pattern.SetCellContent(cell)
            .SetReflectMethod(ReflectMethod.RowAndColumn)
            .SetRelativeTo(RelativeTo.Page)
            .SetCTM(ST_Array.UnitCTM());

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(pattern.Proxy));
        var parsed = new CT_Pattern(DomParser.Parse(xml));
        Assert.Equal(10, parsed.GetWidth());
        Assert.Equal(20, parsed.GetHeight());
        Assert.Equal(10, parsed.GetXStep());
        Assert.Equal(30, parsed.GetYStep());
        Assert.Equal(ReflectMethod.RowAndColumn, parsed.GetReflectMethod());
        Assert.Equal(RelativeTo.Page, parsed.GetRelativeTo());
        Assert.Equal("1 0 0 1 0 0", parsed.GetCTM()!.ToString());
        var pCell = parsed.GetCellContent();
        Assert.Equal(7, pCell.GetThumbnail()!.GetRefId()!.Id);
        Assert.IsType<PathObject>(pCell.GetPageBlocks()[0]);

        // Width 缺失时 GetXStep / GetWidth 抛异常（与 Java 一致）
        Assert.Throws<InvalidOperationException>(() => new CT_Pattern().GetWidth());
        Assert.Throws<InvalidOperationException>(() => new CT_Pattern().GetCellContent());
    }

    [Fact]
    public void ColorCluster_factory_dispatches_all_five_types()
    {
        Assert.IsType<CT_Pattern>(ColorClusterTypeFactory.GetInstance(Parse("Pattern")));
        Assert.IsType<CT_AxialShd>(ColorClusterTypeFactory.GetInstance(Parse("AxialShd")));
        Assert.IsType<CT_RadialShd>(ColorClusterTypeFactory.GetInstance(Parse("RadialShd")));
        Assert.IsType<CT_GouraudShd>(ColorClusterTypeFactory.GetInstance(Parse("GouraudShd")));
        Assert.IsType<CT_LaGouraudShd>(ColorClusterTypeFactory.GetInstance(Parse("LaGouraudShd")));
        Assert.Throws<ArgumentException>(() => ColorClusterTypeFactory.GetInstance(Parse("Bogus")));

        static DomElement Parse(string name) =>
            DomParser.Parse($"<ofd:{name} xmlns:ofd=\"http://www.ofdspec.org/2016\"/>");
    }
}
