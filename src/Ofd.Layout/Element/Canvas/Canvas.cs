using Ofd.Core.BasicStructure.PageObj.Layer.Block;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 画板（Java Canvas）：用于更自由地向页面加入内容，绘制器负责产出图元。
/// </summary>
public class Canvas : Div
{
    /// <summary>绘制器。</summary>
    private IDrawer? _drawer;

    /// <summary>
    /// 优先使用的页面块：Canvas 生成的所有图元都存入该区块；
    /// 默认 null 表示每次绘制创建新的页面块。
    /// </summary>
    private CT_PageBlock? _preferBlock;

    /// <summary>创建 Canvas 并指定绘制器。</summary>
    public Canvas(double? width, double? height, IDrawer? drawer) : base(width, height)
    {
        _drawer = drawer;
    }

    /// <summary>创建 Canvas（宽高必须指定）。</summary>
    public Canvas(double? width, double? height) : base(width, height) { }

    /// <summary>在指定位置创建 Canvas。</summary>
    public Canvas(double x, double y, double w, double h) : base(x, y, w, h) { }

    /// <summary>按页面样式创建 Canvas。</summary>
    public Canvas(PageLayout style) : base(style.GetWidth(), style.GetHeight()) { }

    public virtual IDrawer? GetDrawer() => _drawer;

    /// <summary>重设绘制器。</summary>
    public virtual Canvas SetDrawer(IDrawer? drawer)
    {
        _drawer = drawer;
        return this;
    }

    /// <summary>Canvas 不接受宽度重设，直接返回盒式模型尺寸。</summary>
    public override Rectangle DoPrepare(double? widthLimit) =>
        new((GetWidth() ?? 0) + WidthPlus(), (GetHeight() ?? 0) + HeightPlus());

    /// <summary>获取优先使用的页面块（渲染后通常非空）。</summary>
    public CT_PageBlock? GetPreferBlock() => _preferBlock;

    /// <summary>设置优先使用的页面块（具有一定危险性，不了解用途请勿使用）。</summary>
    public void SetPreferBlock(CT_PageBlock? preferBlock) => _preferBlock = preferBlock;

    public override string ElementType() => "Canvas";
}
