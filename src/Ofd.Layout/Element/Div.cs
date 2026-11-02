using LayerType = Ofd.Core.BasicStructure.PageObj.Layer.Type;
using Ofd.Layout.Engine;
using Ofd.Layout.Handler;
using Ofd.Layout.Element.Canvas;

namespace Ofd.Layout.Element;

/// <summary>
/// 盒式模型基础（Java Div&lt;T&gt;）：所有块级元素的基类。
/// <para>泛型 T 为链式调用返回类型，便于子类链式设置后保持自身类型。</para>
/// </summary>
public class Div : IRenderPrepare, IElementSplit
{
    /// <summary>背景颜色 (R,G,B)。</summary>
    private int[]? _backgroundColor;

    /// <summary>边框颜色 (R,G,B)。</summary>
    private int[]? _borderColor;

    /// <summary>内容宽度（null 自适应，且不大于版心宽度）。</summary>
    private double? _width;

    /// <summary>内容高度（null 自适应）。</summary>
    private double? _height;

    /// <summary>内边距 [上,右,下,左]。</summary>
    private double[] _padding = { 0d, 0d, 0d, 0d };

    /// <summary>边框宽度 [上,右,下,左]。</summary>
    private double[] _border = { 0d, 0d, 0d, 0d };

    /// <summary>外边距 [上,右,下,左]。</summary>
    private double[] _margin = { 0d, 0d, 0d, 0d };

    /// <summary>边框虚线样式 [偏移量,虚线长,空白长,...]。</summary>
    private double[]? _borderDash;

    /// <summary>固定布局左上角 X。</summary>
    private double? _x;

    /// <summary>固定布局左上角 Y。</summary>
    private double? _y;

    /// <summary>对段的占用情况。</summary>
    private Clear _clear = Clear.Both;

    /// <summary>在段中的浮动方向。</summary>
    private AFloat _aFloat = AFloat.Left;

    /// <summary>相对于段左边界距离。</summary>
    private double _left;

    /// <summary>相对于段右边界距离。</summary>
    private double _right;

    /// <summary>相对坐标 top。</summary>
    private double _top;

    /// <summary>整体透明度 [0,1]，null 不透明。</summary>
    private double? _opacity;

    /// <summary>定位方式，默认静态。</summary>
    private Position _position = Position.Static;

    /// <summary>空间不足时是否不拆分（true 不拆分）。</summary>
    private bool? _integrity = false;

    /// <summary>占位符（不参与渲染）。</summary>
    private bool _placeholder;

    /// <summary>图层，默认正文层。</summary>
    private LayerType _layer = LayerType.Body;

    /// <summary>渲染完成回调。</summary>
    private ElementRenderFinishHandler? _renderFinishHandler;

    public Div() { }

    public Div(double? width, double? height)
    {
        _width = width;
        _height = height;
    }

    /// <summary>创建绝对定位的 Div。</summary>
    public Div(double x, double y, double width, double height)
    {
        SetX(x);
        SetY(y);
        SetWidth(width);
        SetHeight(height);
        SetPosition(Position.Absolute);
    }

    /// <summary>链式返回自身。</summary>
    protected Div Self => this;

    /// <summary>是否无边框。</summary>
    public bool IsNoBorder() =>
        GetBorderTop() == 0d && GetBorderRight() == 0d && GetBorderBottom() == 0d && GetBorderLeft() == 0d;

    /// <summary>是否块级元素（绝对定位不算块级）。</summary>
    public bool IsBlockElement()
    {
        if (_position == Position.Absolute) return false;

        return _clear == Clear.Both
               || (_aFloat == AFloat.Right && _clear == Clear.Left)
               || (_aFloat == AFloat.Left && _clear == Clear.Right);
    }

    public double? GetOpacity() => _opacity;

    /// <summary>设置透明度（0 全透明，1 不透明，越界钳制）。</summary>
    public Div SetOpacity(double? opacity)
    {
        if (opacity != null)
        {
            if (opacity > 1) opacity = 1d;
            else if (opacity < 0) opacity = 0d;
        }
        _opacity = opacity;
        return Self;
    }

    public int[]? GetBorderColor() => _borderColor;

    /// <summary>设置边框颜色 RGB。</summary>
    public Div SetBorderColor(int r, int g, int b)
    {
        _borderColor = new[] { r, g, b };
        return Self;
    }

    /// <summary>设置边框颜色（数组，null 表示无色）。</summary>
    public Div SetBorderColor(int[]? rgb)
    {
        _borderColor = rgb;
        return Self;
    }

    /// <summary>设置边框颜色（支持 16 进制 / rgb() / 颜色名）。</summary>
    public Div SetBorderColor(string color)
    {
        int[]? c = NamedColor.Rgb(color);
        if (c == null || c.Length < 3) throw new ArgumentException("边框颜色 颜色值错误：" + color);
        SetBorderColor(c);
        return Self;
    }

    public double? GetTop() => _top;

    public Div SetTop(double? top)
    {
        _top = top ?? 0d;
        return Self;
    }

    public Position GetPosition() => _position;

    public Div SetPosition(Position position)
    {
        _position = position;
        return Self;
    }

    public int[]? GetBackgroundColor() => _backgroundColor;

    /// <summary>设置背景颜色 RGB。</summary>
    public Div SetBackgroundColor(int r, int g, int b)
    {
        _backgroundColor = new[] { r, g, b };
        return Self;
    }

    /// <summary>设置背景颜色（支持 16 进制 / rgb() / 颜色名）。</summary>
    public Div SetBackgroundColor(string color)
    {
        int[]? c = NamedColor.Rgb(color);
        if (c == null || c.Length < 3) throw new ArgumentException("背景颜色 颜色值错误：" + color);
        SetBackgroundColor(c);
        return Self;
    }

    public Div SetBackgroundColor(int[]? backgroundColor)
    {
        _backgroundColor = backgroundColor;
        return Self;
    }

    public double? GetWidth() => _width;

    public Div SetWidth(double? width)
    {
        _width = width;
        return Self;
    }

    public double? GetHeight() => _height;

    public Div SetHeight(double? height)
    {
        _height = height;
        return Self;
    }

    public double[] GetPadding() => _padding;

    /// <summary>设置内边距（1/2/3/4 参数归一化）。</summary>
    public Div SetPadding(params double[]? padding)
    {
        _padding = ArrayParamTool.Arr4p(padding);
        return Self;
    }

    public double[] GetBorder() => _border;

    /// <summary>设置边框宽度（1/2/3/4 参数归一化）。</summary>
    public Div SetBorder(params double[]? border)
    {
        _border = ArrayParamTool.Arr4p(border);
        return Self;
    }

    /// <summary>设置边框虚线样式（null 取消；单参 → {0,d,d}；奇数首参为偏移量；偶数补 0 偏移）。</summary>
    public Div SetBorderDash(params double[]? dash)
    {
        if (dash == null || dash.Length == 0)
        {
            _borderDash = null;
            return Self;
        }

        if (dash.Length == 1)
        {
            _borderDash = new[] { 0.0, dash[0], dash[0] };
            return Self;
        }

        if (dash.Length % 2 == 1)
        {
            _borderDash = (double[])dash.Clone();
        }
        else
        {
            var arr = new double[dash.Length + 1];
            arr[0] = 0.0;
            Array.Copy(dash, 0, arr, 1, dash.Length);
            _borderDash = arr;
        }
        return Self;
    }

    public double[]? GetBorderDash() => _borderDash;

    public double[] GetMargin() => _margin;

    /// <summary>设置外边距（1/2/3/4 参数归一化）。</summary>
    public Div SetMargin(params double[]? margin)
    {
        _margin = ArrayParamTool.Arr4p(margin);
        return Self;
    }

    public double GetMarginTop() => _margin[0];

    public Div SetMarginTop(double top)
    {
        _margin[0] = top;
        return Self;
    }

    public double GetMarginRight() => _margin[1];

    public Div SetMarginRight(double right)
    {
        _margin[1] = right;
        return Self;
    }

    public double GetMarginBottom() => _margin[2];

    public Div SetMarginBottom(double bottom)
    {
        _margin[2] = bottom;
        return Self;
    }

    public double GetMarginLeft() => _margin[3];

    public Div SetMarginLeft(double left)
    {
        _margin[3] = left;
        return Self;
    }

    public double GetBorderTop() => _border[0];

    public Div SetBorderTop(double top)
    {
        _border[0] = top;
        return Self;
    }

    public double GetBorderRight() => _border[1];

    public Div SetBorderRight(double right)
    {
        _border[1] = right;
        return Self;
    }

    public double GetBorderBottom() => _border[2];

    public Div SetBorderBottom(double bottom)
    {
        _border[2] = bottom;
        return Self;
    }

    public double GetBorderLeft() => _border[3];

    public Div SetBorderLeft(double left)
    {
        _border[3] = left;
        return Self;
    }

    public double GetPaddingTop() => _padding[0];

    public Div SetPaddingTop(double top)
    {
        _padding[0] = top;
        return Self;
    }

    public double GetPaddingRight() => _padding[1];

    public Div SetPaddingRight(double right)
    {
        _padding[1] = right;
        return Self;
    }

    public double GetPaddingBottom() => _padding[2];

    public Div SetPaddingBottom(double bottom)
    {
        _padding[2] = bottom;
        return Self;
    }

    public double GetPaddingLeft() => _padding[3];

    public Div SetPaddingLeft(double left)
    {
        _padding[3] = left;
        return Self;
    }

    public double? GetX() => _x;

    public Div SetX(double? x)
    {
        _x = x;
        return Self;
    }

    public double? GetY() => _y;

    public Div SetY(double? y)
    {
        _y = y;
        return Self;
    }

    /// <summary>设置位置。</summary>
    public Div SetXY(double? x, double? y)
    {
        SetX(x);
        SetY(y);
        return Self;
    }

    /// <summary>设置盒式模型主要属性。</summary>
    public Div SetBox(double? x, double? y, double? width, double? height)
    {
        SetX(x);
        SetY(y);
        SetWidth(width);
        SetHeight(height);
        return Self;
    }

    public Clear GetClear() => _clear;

    public Div SetClear(Clear clear)
    {
        _clear = clear;
        return Self;
    }

    public AFloat GetFloat() => _aFloat;

    /// <summary>设置浮动样式（居中需同时把 Clear 设为 None 或 Right）。</summary>
    public Div SetFloat(AFloat aFloat)
    {
        _aFloat = aFloat;
        return Self;
    }

    public double GetLeft() => _left;

    public Div SetLeft(double? left)
    {
        _left = left ?? 0d;
        return Self;
    }

    public double GetRight() => _right;

    public Div SetRight(double? right)
    {
        _right = right ?? 0d;
        return Self;
    }

    /// <summary>是否不可拆分。</summary>
    public bool? IsIntegrity() => _integrity;

    public Div SetIntegrity(bool? integrity)
    {
        _integrity = integrity;
        return Self;
    }

    /// <summary>额外宽度（左右 margin + padding + border）。</summary>
    public double WidthPlus() =>
        _margin[1] + _margin[3] + (_padding[1] + _padding[3]) + (_border[1] + _border[3]);

    /// <summary>额外高度（上下 margin + padding + border）。</summary>
    public double HeightPlus() =>
        _margin[0] + _margin[2] + (_padding[0] + _padding[2]) + (_border[0] + _border[2]);

    /// <summary>计算尺寸（宽高未设置时返回空矩形；超出版心时收缩宽度）。</summary>
    public virtual Rectangle DoPrepare(double? widthLimit)
    {
        if (_height == null || _width == null) return Rectangle.Empty;
        if (widthLimit == null) throw new ArgumentNullException(nameof(widthLimit));

        double limit = widthLimit.Value - WidthPlus();
        if (_width.Value > limit)
        {
            // 与 Java 一致：超出版心时重置宽度
            SetWidth(limit);
        }

        return new Rectangle(_width!.Value + WidthPlus(), _height!.Value + HeightPlus());
    }

    /// <summary>模型区域大小（需在宽高确定后调用）。</summary>
    public Rectangle Box() => new((_width ?? 0) + WidthPlus(), (_height ?? 0) + HeightPlus());

    public LayerType GetLayer() => _layer;

    public Div SetLayer(LayerType layer)
    {
        _layer = layer;
        return Self;
    }

    public bool IsPlaceholder() => _placeholder;

    public Div SetPlaceholder(bool placeholder)
    {
        _placeholder = placeholder;
        return Self;
    }

    /// <summary>设置显示样式（inlineBlock → Clear.None；block → Clear.Both）。</summary>
    public Div SetDisplay(Display display) => display == Display.InlineBlock ? SetClear(Clear.None) : SetClear(Clear.Both);

    public Display GetDisplay()
    {
        if (IsBlockElement()) return Display.Block;
        return _clear == Clear.None ? Display.InlineBlock : Display.Block;
    }

    /// <summary>克隆为全新 Div。</summary>
    public virtual Div Clone()
    {
        var div = new Div();
        return CopyTo(div);
    }

    /// <summary>把当前属性复制到目标元素。</summary>
    public virtual Div CopyTo(Div div)
    {
        div.SetBackgroundColor(_backgroundColor == null ? null : (int[])_backgroundColor.Clone());
        div.SetBorderColor(_borderColor == null ? null : (int[])_borderColor.Clone());
        div.SetWidth(_width);
        div.SetHeight(_height);
        div.SetPadding((double[])_padding.Clone());
        div.SetBorder((double[])_border.Clone());
        div.SetMargin((double[])_margin.Clone());
        div.SetX(_x);
        div.SetY(_y);
        div.SetClear(_clear);
        div.SetFloat(_aFloat);
        div.SetLeft(_left);
        div.SetRight(_right);
        div.SetTop(_top);
        div.SetPosition(_position);
        div.SetIntegrity(_integrity);
        div.SetPlaceholder(_placeholder);
        div.SetLayer(_layer);
        div.OnRenderFinish(_renderFinishHandler);
        return div;
    }

    /// <summary>
    /// 按给定高度切分元素：依次消耗 margin → border → padding → content，
    /// 切分处无 margin/border/padding。返回 {上半, 下半}。
    /// </summary>
    public virtual Div[] Split(double sHeight)
    {
        if (_width == null || _height == null)
            throw new InvalidOperationException("切分元素必须要有固定的宽度（width）和高度（height）");

        double totalH = _height.Value + HeightPlus();
        if (totalH <= sHeight) return new Div[] { this }; // 无需切分

        Div div1 = Clone();
        Div div2 = Clone();

        if (GetMarginTop() >= sHeight)
        {
            double deltaM = GetMarginTop() - sHeight;
            div1.SetMarginTop(sHeight)
                .SetBorderTop(0d).SetPaddingTop(0d).SetHeight(0d)
                .SetPaddingBottom(0d).SetBorderBottom(0d).SetMarginBottom(0d)
                .SetPlaceholder(true);
            div2.SetMarginTop(deltaM);
        }
        else if (GetMarginTop() + GetBorderTop() >= sHeight)
        {
            double deltaB = GetBorderTop() - (sHeight - GetMarginTop());
            div1.SetBorderTop(sHeight - GetMarginTop())
                .SetPaddingTop(0d).SetHeight(0d)
                .SetPaddingBottom(0d).SetBorderBottom(0d).SetMarginBottom(0d);
            div2.SetMarginTop(0d).SetBorderTop(deltaB);
        }
        else if (GetMarginTop() + GetBorderTop() + GetPaddingTop() >= sHeight)
        {
            double deltaP = GetPaddingTop() - (sHeight - GetMarginTop() - GetBorderTop());
            div1.SetPaddingTop(sHeight - GetMarginTop() - GetBorderTop())
                .SetHeight(0d).SetPaddingBottom(0d).SetBorderBottom(0d).SetMarginBottom(0d);
            div2.SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(deltaP);
        }
        else if (GetMarginTop() + GetBorderTop() + GetPaddingTop() + GetHeight() >= sHeight)
        {
            var divs = ContentSplitAdjust(sHeight, div1, div2);
            div1 = divs[0];
            div2 = divs[1];
        }
        else if (GetMarginTop() + GetBorderTop() + GetPaddingTop() + (_height ?? 0) + GetPaddingBottom() >= sHeight)
        {
            double deltaP = sHeight - (GetMarginTop() + GetBorderTop() + GetPaddingTop() + (_height ?? 0));
            div1.SetPaddingBottom(deltaP).SetBorderBottom(0d).SetMarginBottom(0d);
            div2.SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(0d).SetHeight(0d)
                .SetPaddingBottom(div2.GetPaddingBottom() - deltaP);
        }
        else if (GetMarginTop() + GetBorderTop() + GetPaddingTop() + (_height ?? 0) + GetPaddingBottom() + GetBorderBottom() >= sHeight)
        {
            double deltaB = sHeight - (GetMarginTop() + GetBorderTop() + GetPaddingTop() + (_height ?? 0) + GetPaddingBottom());
            div1.SetBorderBottom(deltaB).SetMarginBottom(0d);
            div2.SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(0d).SetHeight(0d)
                .SetPaddingBottom(0d).SetBorderBottom(div2.GetBorderBottom() - deltaB);
        }
        else
        {
            double deltaM = sHeight - (GetMarginTop() + GetBorderTop() + GetPaddingTop()
                                       + (_height ?? 0) + GetPaddingBottom() + GetBorderBottom());
            div1.SetMarginBottom(deltaM);
            div2.SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(0d).SetHeight(0d)
                .SetPaddingBottom(0d).SetBorderBottom(0d)
                .SetMarginBottom(div2.GetMarginBottom() - deltaM)
                .SetPlaceholder(true);
        }

        return new Div[] { div1, div2 };
    }

    /// <summary>
    /// 内容分割调整：div1 无下边、总高为切分高度；div2 无上边、高为剩余高度。
    /// </summary>
    public virtual Div[] ContentSplitAdjust(double sHeight, Div div1, Div div2)
    {
        double h1 = sHeight - (div1.GetMarginTop() + div1.GetBorderTop() + div1.GetPaddingTop());
        div1.SetHeight(h1).SetMarginBottom(0d).SetBorderBottom(0d).SetPaddingBottom(0d);

        double h2 = (div2.GetHeight() ?? 0) - h1;
        div2.SetHeight(h2).SetMarginTop(0d).SetBorderTop(0d).SetPaddingTop(0d);

        return new Div[] { div1, div2 };
    }

    /// <summary>元素类型名称（供处理器选择）。</summary>
    public virtual string ElementType() => "Div";

    public ElementRenderFinishHandler? GetOnRenderFinish() => _renderFinishHandler;

    /// <summary>设置渲染完成回调。</summary>
    public void OnRenderFinish(ElementRenderFinishHandler? renderFinishHandler) => _renderFinishHandler = renderFinishHandler;

    /// <summary>空间占位符（共享段空间且不可分割）。</summary>
    public static Div Placeholder(double width, double height, AFloat aFloat) =>
        new Div()
            .SetPlaceholder(true)
            .SetWidth(width)
            .SetHeight(height)
            .SetFloat(aFloat)
            .SetClear(Clear.None)
            .SetIntegrity(true);

    /// <summary>空间占位符。</summary>
    public static Div Placeholder(Rectangle rec, AFloat aFloat) =>
        Placeholder(rec.GetWidth(), rec.GetHeight(), aFloat);

    public override string ToString() =>
        $"Div{{width={_width}, height={_height}, x={_x}, y={_y}, clear={_clear}, float={_aFloat}, " +
        $"left={_left}, right={_right}, top={_top}, opacity={_opacity}, position={_position}, " +
        $"placeholder={_placeholder}, layer={_layer}}}";
}

// 说明：Java 的 Div<T extends Div> 通过擦除使子类同时是 Div；
// C# 泛型无法表达该关系，故采用单一非泛型 Div + 虚方法返回 Div，
// 子类（Paragraph/Span 等）通过 new 包装返回自身类型以保留链式体验。
