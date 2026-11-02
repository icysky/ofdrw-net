using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Color.Color;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// Canvas 线性渐变（Java CanvasGradient）：与 HTML Canvas API 兼容。
/// </summary>
public class CanvasGradient
{
    public readonly CT_AxialShd AxialShd;

    /// <summary>创建线性渐变。</summary>
    public CanvasGradient(double x0, double y0, double x1, double y1)
    {
        AxialShd = new CT_AxialShd();
        AxialShd.SetStartPoint(ST_Pos.GetInstance(x0, y0));
        AxialShd.SetEndPoint(ST_Pos.GetInstance(x1, y1));
    }

    /// <summary>添加渐变颜色段（offset ∈ [0,1]；颜色支持 16 进制或颜色名）。</summary>
    public void AddColorStop(double offset, string color)
    {
        int[]? rgb = NamedColor.Rgb(color);
        if (rgb == null) return;

        var c = CT_Color.Rgb(rgb[0], rgb[1], rgb[2]);
        if (rgb.Length > 3) c.SetAlpha(rgb[3]); // 含透明度

        AxialShd.AddSegment(new Segment(offset, c));
    }
}
