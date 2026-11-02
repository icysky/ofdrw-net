using System.Globalization;
using Ofd.Core.Action;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Clips;
using Ofd.Core.PageDescription.DrawParam;

namespace Ofd.Core.PageDescription;

/// <summary>
/// 图元对象基类（8.5 图 45 表 34）：页面显示内容的最基本单元（文字、图形、图像及其组合）。
/// 泛型参数 T 用于链式 setter 返回具体子类型（对应 Java 的 CT_GraphicUnit&lt;T&gt;）。
/// </summary>
public abstract class CT_GraphicUnit<T> : OFDElement, IGraphicUnit where T : CT_GraphicUnit<T>
{
    protected CT_GraphicUnit(Dom.DomElement proxy) : base(proxy) { }

    protected CT_GraphicUnit(string name) : base(name) { }

    /// <summary>【可选属性】是否填充（默认 false，与 OFD 一致）。</summary>
    public T SetFill(bool? fill) => (T)AddAttribute("Fill", fill == null ? null : (fill.Value ? "true" : "false"));

    /// <summary>获取是否填充。</summary>
    public bool GetFill() => string.Equals(AttributeValue("Fill"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>【可选属性】是否描边（默认 true）。</summary>
    public T SetStroke(bool? stroke) => (T)AddAttribute("Stroke", stroke == null ? null : (stroke.Value ? "true" : "false"));

    /// <summary>获取是否描边。</summary>
    public bool GetStroke() => !string.Equals(AttributeValue("Stroke"), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>【必选属性】设置 外接矩形（超出裁剪）。</summary>
    public T SetBoundary(ST_Box boundary) => (T)AddAttribute("Boundary", boundary.ToString());

    /// <summary>【必选属性】设置 外接矩形（坐标形式）。</summary>
    public T SetBoundary(double topLeftX, double topLeftY, double width, double height) =>
        SetBoundary(new ST_Box(topLeftX, topLeftY, width, height));

    /// <summary>【必选属性】获取 外接矩形，不存在 null。</summary>
    public ST_Box? GetBoundary() => ST_Box.GetInstance(AttributeValue("Boundary"));

    /// <summary>【可选属性】设置 图元对象的名字（空白删除属性）。</summary>
    public T SetGraphicName(string? name)
    {
        if (name == null || name.Trim().Length == 0)
        {
            RemoveAttr("Name");
            return (T)this;
        }
        return (T)AddAttribute("Name", name);
    }

    /// <summary>【可选属性】获取 图元对象的名字，可能为 null。</summary>
    public string? GetGraphicName() => AttributeValue("Name");

    /// <summary>【可选属性】设置 图元是否可见（null 删除属性，缺省可见）。</summary>
    public T SetVisible(bool? visible)
    {
        if (visible == null)
        {
            RemoveAttr("Visible");
            return (T)this;
        }
        return (T)AddAttribute("Visible", visible.Value ? "true" : "false");
    }

    /// <summary>【可选属性】获取 图元是否可见，缺省 true。</summary>
    public bool GetVisible()
    {
        var str = AttributeValue("Visible");
        return str == null || str.Trim().Length == 0 || bool.Parse(str.Trim());
    }

    /// <summary>【可选属性】设置 图元变换矩阵。</summary>
    public T SetCTM(ST_Array ctm) => (T)AddAttribute("CTM", ctm.ToString());

    /// <summary>【可选属性】获取 图元变换矩阵，不存在 null。</summary>
    public ST_Array? GetCTM() => ST_Array.GetInstance(AttributeValue("CTM"));

    /// <summary>【可选属性】设置 引用资源文件中的绘制参数标识。</summary>
    public T SetDrawParam(ST_RefID id) => (T)AddAttribute("DrawParam", id.ToString());

    /// <summary>【可选属性】获取 绘制参数标识，不存在 null。</summary>
    public ST_RefID? GetDrawParam() => ST_RefID.GetInstance(AttributeValue("DrawParam"));

    /// <summary>【可选属性】设置 线宽（null 删除属性）。</summary>
    public T SetLineWidth(double? lineWidth)
    {
        if (lineWidth == null)
        {
            RemoveAttr("LineWidth");
            return (T)this;
        }
        return (T)AddAttribute("LineWidth", STBase.Fmt(lineWidth.Value));
    }

    /// <summary>【可选属性】获取 线宽，可能为 null。</summary>
    public double? GetLineWidth()
    {
        var str = AttributeValue("LineWidth");
        return str == null || str.Trim().Length == 0
            ? null
            : double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>【可选属性】设置 线端点样式（null 删除属性）。</summary>
    public T SetCap(LineCapType? cap)
    {
        if (cap == null)
        {
            RemoveAttr("Cap");
            return (T)this;
        }
        return (T)AddAttribute("Cap", cap.ToString());
    }

    /// <summary>【可选属性】获取 线端点样式，缺省 Butt。</summary>
    public LineCapType GetCap() => LineCapTypeExtensions.GetInstance(AttributeValue("Cap"));

    /// <summary>【可选属性】设置 线条连接样式（null 删除属性）。</summary>
    public T SetJoin(LineJoinType? join)
    {
        if (join == null)
        {
            RemoveAttr("Join");
            return (T)this;
        }
        return (T)AddAttribute("Join", join.ToString());
    }

    /// <summary>【可选属性】获取 线条连接样式，缺省 Miter。</summary>
    public LineJoinType GetJoin() => LineJoinTypeExtensions.GetInstance(AttributeValue("Join"));

    /// <summary>【可选属性】设置 Join 截断值（null 删除属性）。</summary>
    public T SetMiterLimit(double? miterLimit)
    {
        if (miterLimit == null)
        {
            RemoveAttr("MiterLimit");
            return (T)this;
        }
        return (T)AddAttribute("MiterLimit", STBase.Fmt(miterLimit.Value));
    }

    /// <summary>【可选属性】获取 Join 截断值，缺省 3.528。</summary>
    public double GetMiterLimit() => GetDouble("MiterLimit", 3.528d);

    /// <summary>【可选属性】设置 虚线开始位置（null 删除属性）。</summary>
    public T SetDashOffset(double? dashOffset)
    {
        if (dashOffset == null)
        {
            RemoveAttr("DashOffset");
            return (T)this;
        }
        return (T)AddAttribute("DashOffset", STBase.Fmt(dashOffset.Value));
    }

    /// <summary>【可选属性】获取 虚线开始位置，缺省 0。</summary>
    public double GetDashOffset() => GetDouble("DashOffset", 0d);

    /// <summary>【可选属性】设置 虚线重复样式（null 删除属性）。</summary>
    public T SetDashPattern(ST_Array? dashPattern)
    {
        if (dashPattern == null)
        {
            RemoveAttr("DashPattern");
            return (T)this;
        }
        return (T)AddAttribute("DashPattern", dashPattern.ToString());
    }

    /// <summary>【可选属性】获取 虚线重复样式，不存在 null。</summary>
    public ST_Array? GetDashPattern() => ST_Array.GetInstance(AttributeValue("DashPattern"));

    /// <summary>【可选属性】设置 图元透明度（钳制 0~255；null 删除属性）。</summary>
    public T SetAlpha(int? alpha)
    {
        if (alpha == null)
        {
            RemoveAttr("Alpha");
            return (T)this;
        }
        int v = alpha.Value;
        if (v < 0) v = 255;
        if (v > 255) v = 255;
        return (T)AddAttribute("Alpha", v.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>【可选属性】获取 图元透明度，缺省 255。</summary>
    public int GetAlpha()
    {
        var str = AttributeValue("Alpha");
        return str == null || str.Trim().Length == 0 ? 255 : int.Parse(str.Trim(), CultureInfo.InvariantCulture);
    }

    /// <summary>【可选】设置 图元对象的动作序列。</summary>
    public T SetActions(Actions actions)
    {
        Add(actions);
        return (T)this;
    }

    /// <summary>【可选】获取 图元对象的动作序列，不存在 null。</summary>
    public Actions? GetActions()
    {
        var e = GetOFDElement("Actions");
        return e == null ? null : new Actions(e);
    }

    /// <summary>【可选】设置 裁剪区域序列（空裁剪序列忽略，与 Java 一致）。</summary>
    public CT_GraphicUnit<T> SetClips(global::Ofd.Core.PageDescription.Clips.Clips? clips)
    {
        if (clips == null) return this;
        var ctClips = clips.GetClips();
        if (ctClips.Count == 0) return this;
        Add(clips);
        return this;
    }

    /// <summary>【可选】获取 裁剪区域序列，不存在 null。</summary>
    public global::Ofd.Core.PageDescription.Clips.Clips? GetClips()
    {
        var e = GetOFDElement("Clips");
        return e == null ? null : new global::Ofd.Core.PageDescription.Clips.Clips(e);
    }

    private double GetDouble(string name, double defaultValue)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return defaultValue;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    // ── IGraphicUnit（非泛型视图，供绘制上下文统一设置） ──

    ST_Box? IGraphicUnit.GetBoundaryBox() => GetBoundary();

    void IGraphicUnit.ApplyBoundary(ST_Box boundary) => SetBoundary(boundary);

    void IGraphicUnit.ApplyAlpha(int? alpha) => SetAlpha(alpha);

    ST_Array? IGraphicUnit.GetCtmArray() => GetCTM();

    void IGraphicUnit.ApplyCtm(ST_Array ctm) => SetCTM(ctm);

    void IGraphicUnit.ApplyDrawParam(ST_RefID id) => SetDrawParam(id);

    void IGraphicUnit.ApplyClips(global::Ofd.Core.PageDescription.Clips.Clips? clips) => SetClips(clips);
}
