using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Layout.Element.Canvas;

/// <summary>Canvas 放射状/圆形渐变（Java CanvasRadialGradient）。</summary>
public class CanvasRadialGradient
{
    public readonly CT_RadialShd RadialShd;

    /// <summary>创建放射状/圆形渐变。</summary>
    public CanvasRadialGradient(double x0, double y0, double r0, double x1, double y1, double r1)
    {
        RadialShd = new CT_RadialShd();
        RadialShd.SetStartPoint(ST_Pos.GetInstance(x0, y0));
        RadialShd.SetStartRadius(r0);
        RadialShd.SetEndPoint(ST_Pos.GetInstance(x1, y1));
        RadialShd.SetEndRadius(r1);
        RadialShd.SetExtend(Extend._1);
    }

    /// <summary>添加渐变颜色段（offset ∈ [0,1]；颜色支持 16 进制或颜色名）。</summary>
    public void AddColorStop(double offset, string color)
    {
        int[]? rgb = NamedColor.Rgb(color);
        if (rgb == null) return;

        var c = CT_Color.Rgb(rgb[0], rgb[1], rgb[2]);
        if (rgb.Length > 3) c.SetAlpha(rgb[3]);

        RadialShd.AddSegment(new Segment(offset, c));
    }
}
