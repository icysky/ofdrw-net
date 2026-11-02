using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicType;
using Ofd.Layout.Element.Canvas;

namespace Ofd.Layout.Edit;

/// <summary>
/// 注释对象（Java Annotation）：注释参数 + 绘制器，最终构建为 OFD 的 <see cref="Annot"/>。
/// </summary>
public class Annotation
{
    /// <summary>绘制器。</summary>
    private IDrawer? _drawer;

    /// <summary>注释类型。</summary>
    private AnnotType _type;

    /// <summary>注释创建者。</summary>
    private string? _creator;

    /// <summary>最近一次修改时间。</summary>
    private DateOnly? _lastModDate;

    /// <summary>注释子类型。</summary>
    private string? _subtype;

    /// <summary>是否显示。</summary>
    private bool? _visible;

    /// <summary>Remark 是否不随页面旋转。</summary>
    private bool? _noRotate;

    /// <summary>注释参数（保持插入顺序）。</summary>
    private List<KeyValuePair<string, string>>? _parameters;

    /// <summary>注释说明内容。</summary>
    private string? _remark;

    /// <summary>Remark 是否不能被用户更改。</summary>
    private bool? _readOnly;

    /// <summary>Remark 是否随页面打印。</summary>
    private bool? _print;

    /// <summary>Remark 是否不随页面缩放。</summary>
    private bool? _noZoom;

    /// <summary>注释空间。</summary>
    private ST_Box _boundary;

    /// <summary>创建注释对象。</summary>
    public Annotation(double x, double y, double width, double height, AnnotType type, IDrawer drawer)
        : this(new ST_Box(x, y, width, height), type, drawer) { }

    /// <summary>创建注释对象。</summary>
    public Annotation(ST_Box boundary, AnnotType type, IDrawer drawer)
    {
        _boundary = boundary ?? throw new ArgumentException("注释对象绘制空间(boundary)不能为空");
        _type = type;
        _drawer = drawer ?? throw new ArgumentException("注解绘制器(drawer)不能为空");
    }

    /// <summary>构建 OFD 注释对象。</summary>
    public Annot Build()
    {
        var annot = new Annot()
            .SetType(_type)
            .SetCreator(_creator ?? "OFD R&W")
            .SetLastModDate(_lastModDate ?? DateOnly.FromDateTime(DateTime.Now));

        if (_visible != null) annot.SetVisible(_visible.Value);
        if (_subtype != null) annot.SetSubtype(_subtype);
        if (_print != null) annot.SetPrint(_print);
        if (_noZoom != null) annot.SetNoZoom(_noZoom);
        if (_noRotate != null) annot.SetNoRotate(_noRotate);
        if (_readOnly != null) annot.SetReadOnly(_readOnly);
        if (_remark != null) annot.SetRemark(_remark);

        if (_parameters != null)
        {
            foreach (var kv in _parameters) annot.AddParameter(kv.Key, kv.Value);
        }

        annot.SetAppearance(new Appearance(_boundary));
        return annot;
    }

    public ST_Box GetBoundary() => _boundary;

    public void SetBoundary(ST_Box boundary) => _boundary = boundary;

    public IDrawer? GetDrawer() => _drawer;

    public Annotation SetDrawer(IDrawer drawer)
    {
        _drawer = drawer;
        return this;
    }

    public Annotation SetType(AnnotType type)
    {
        _type = type;
        return this;
    }

    public AnnotType GetType() => _type;

    public Annotation SetCreator(string? creator)
    {
        _creator = creator;
        return this;
    }

    public string? GetCreator() => _creator;

    public Annotation SetLastModDate(DateOnly? lastModDate)
    {
        _lastModDate = lastModDate;
        return this;
    }

    public DateOnly? GetLastModDate() => _lastModDate;

    public Annotation SetSubtype(string? subtype)
    {
        _subtype = subtype;
        return this;
    }

    public string? GetSubtype() => _subtype;

    /// <summary>设置是否显示（默认 true）。</summary>
    public Annotation SetVisible(bool visible)
    {
        _visible = visible;
        return this;
    }

    public bool GetVisible() => _visible ?? true;

    /// <summary>设置是否随页面打印（默认 true）。</summary>
    public Annotation SetPrint(bool print)
    {
        _print = print;
        return this;
    }

    public bool GetPrint() => _print ?? true;

    /// <summary>设置是否不随页面缩放（默认 false）。</summary>
    public Annotation SetNoZoom(bool noZoom)
    {
        _noZoom = noZoom;
        return this;
    }

    public bool GetNoZoom() => _noZoom ?? false;

    /// <summary>设置是否不随页面旋转（默认 false）。</summary>
    public Annotation SetNoRotate(bool noRotate)
    {
        _noRotate = noRotate;
        return this;
    }

    public bool GetNoRotate() => _noRotate ?? false;

    /// <summary>设置是否不能被用户更改（默认 true）。</summary>
    public Annotation SetReadOnly(bool readOnly)
    {
        _readOnly = readOnly;
        return this;
    }

    public bool GetReadOnly() => _readOnly ?? true;

    public Annotation SetRemark(string? remark)
    {
        _remark = remark;
        return this;
    }

    public string? GetRemark() => _remark;

    /// <summary>增加注释参数。</summary>
    public Annotation AddParameter(string? name, string parameter)
    {
        if (name == null) return this;
        _parameters ??= new List<KeyValuePair<string, string>>();

        for (int i = 0; i < _parameters.Count; i++)
        {
            if (_parameters[i].Key == name)
            {
                _parameters[i] = new KeyValuePair<string, string>(name, parameter);
                return this;
            }
        }

        _parameters.Add(new KeyValuePair<string, string>(name, parameter));
        return this;
    }

    /// <summary>获取注释参数（不存在时为空集合）。</summary>
    public IReadOnlyDictionary<string, string> GetParameters() =>
        _parameters?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new Dictionary<string, string>();
}
