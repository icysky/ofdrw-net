using Ofd.Core.BasicType;
using Ofd.Font;
using Ofd.Core.Graph.PathObj;
using Ofd.Core.PageDescription.DrawParam;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 画布上下文中的绘制参数状态（Java CanvasState）：save/restore 的栈元素。
/// </summary>
public class CanvasState
{
    /// <summary>上下文路径数据。</summary>
    public AbbreviatedData? Path;

    /// <summary>变换矩阵。</summary>
    public ST_Array? Ctm;

    /// <summary>绘制文字设置（默认宋体，字号 1）。</summary>
    public FontSetting Font;

    /// <summary>透明度（0.0 完全透明 ~ 1.0 不透明）。</summary>
    public double? GlobalAlpha;

    /// <summary>绘制参数。</summary>
    public CT_DrawParam DrawParam;

    /// <summary>裁剪区域。</summary>
    public AbbreviatedData? ClipArea;

    /// <summary>填充颜色（16 进制或颜色对象）。</summary>
    public object? FillStyle;

    /// <summary>描边颜色（16 进制或颜色对象）。</summary>
    public object? StrokeStyle;

    /// <summary>字体样式字符串（CSS 格式）。</summary>
    public string? FontStyle;

    public CanvasState()
    {
        DrawParam = new CT_DrawParam();
        Font = new FontSetting(1d, Ofd.Font.FontName.SimSun.Font());
    }

    /// <summary>获取绘制参数。</summary>
    public CT_DrawParam GetDrawParam() => DrawParam;

    /// <summary>克隆状态（深拷贝路径/矩阵/字体/裁剪区）。</summary>
    public CanvasState Clone()
    {
        var that = new CanvasState();
        if (Path != null) that.Path = Path.Clone();
        if (Ctm != null) that.Ctm = (ST_Array)Ctm.Clone();
        if (Font != null) that.Font = Font.Clone();
        if (GlobalAlpha != null) that.GlobalAlpha = GlobalAlpha;

        // 与 Java 一致：绘制参数克隆后回写自身（原地替换）
        if (DrawParam != null) DrawParam = new CT_DrawParam(DrawParam.GetProxy().Clone());

        if (ClipArea != null) that.ClipArea = ClipArea.Clone();
        that.FillStyle = FillStyle;
        that.StrokeStyle = StrokeStyle;
        that.FontStyle = FontStyle;
        return that;
    }
}
