using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicType;
using Ofd.Layout.Element;

namespace Ofd.Layout;

/// <summary>虚拟页面样式（Java PageLayout）：页面尺寸与页边距。</summary>
public class PageLayout
{
    public static PageLayout A0() => new(841d, 1189d);
    public static PageLayout A1() => new(594d, 841d);
    public static PageLayout A2() => new(420d, 594d);
    public static PageLayout A3() => new(297d, 420d);
    public static PageLayout A4() => new(210d, 297d);
    public static PageLayout A5() => new(148d, 210d);
    public static PageLayout A6() => new(105d, 148d);
    public static PageLayout A7() => new(74d, 105d);
    public static PageLayout A8() => new(52d, 74d);
    public static PageLayout A9() => new(37d, 52d);
    public static PageLayout A10() => new(26d, 37d);

    /// <summary>页面宽度。</summary>
    private double? _width;

    /// <summary>页面高度。</summary>
    private double? _height;

    /// <summary>外边距 [上,右,下,左]，默认上下 25.4mm、左右 31.7mm。</summary>
    private double[] _margin = { 25.4, 31.7, 25.4, 31.7 };

    public PageLayout(double? width, double? height)
    {
        _width = width;
        _height = height;
    }

    public PageLayout(ST_Box box)
    {
        if (box == null) throw new ArgumentException("box 为空");
        _width = box.Width;
        _height = box.Height;
    }

    public double? GetWidth() => _width;

    public PageLayout SetWidth(double? width)
    {
        _width = width;
        return this;
    }

    public double? GetHeight() => _height;

    public PageLayout SetHeight(double? height)
    {
        _height = height;
        return this;
    }

    public double[] GetMargin() => _margin;

    public PageLayout SetMargin(params double[]? margin)
    {
        _margin = ArrayParamTool.Arr4p(margin);
        return this;
    }

    public PageLayout SetMarginTop(double top)
    {
        _margin[0] = top;
        return this;
    }

    public double GetMarginTop() => _margin[0];

    public PageLayout SetMarginRight(double right)
    {
        _margin[1] = right;
        return this;
    }

    public double GetMarginRight() => _margin[1];

    public PageLayout SetMarginBottom(double bottom)
    {
        _margin[2] = bottom;
        return this;
    }

    public double GetMarginBottom() => _margin[2];

    public PageLayout SetMarginLeft(double left)
    {
        _margin[3] = left;
        return this;
    }

    public double GetMarginLeft() => _margin[3];

    /// <summary>实际可放置内容的宽度。</summary>
    public double ContentWidth() => (_width ?? 0) - GetMarginLeft() - GetMarginRight();

    /// <summary>实际可放置内容的高度。</summary>
    public double ContentHeight() => (_height ?? 0) - GetMarginTop() - GetMarginBottom();

    /// <summary>绘制区域原点 X。</summary>
    public double GetStartX() => GetMarginLeft();

    /// <summary>绘制区域原点 Y。</summary>
    public double GetStartY() => GetMarginTop();

    /// <summary>页面正文工作区域。</summary>
    public Rectangle GetWorkerArea() => new(GetStartX(), GetStartY(), ContentWidth(), ContentHeight());

    /// <summary>获取 OFD 页面区域（物理区域与显示区域均为实际页面大小，兼容骑缝章）。</summary>
    public CT_PageArea GetPageArea()
    {
        double w = _width ?? 0;
        double h = _height ?? 0;
        return new CT_PageArea()
            .SetPhysicalBox(0, 0, w, h)
            .SetApplicationBox(0, 0, w, h);
    }

    public override bool Equals(object? obj)
    {
        if (obj == this) return true;
        if (obj is not PageLayout other) return false;
        return _margin.SequenceEqual(other._margin) && _width == other._width && _height == other._height;
    }

    public override int GetHashCode() => HashCode.Combine(_width, _height, _margin.Length);

    /// <summary>克隆（边距数组深拷贝）。</summary>
    public PageLayout Clone()
    {
        var copy = new PageLayout(_width, _height);
        copy._margin = (double[])_margin.Clone();
        return copy;
    }
}
