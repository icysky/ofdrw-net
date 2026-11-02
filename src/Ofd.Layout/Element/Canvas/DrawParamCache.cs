using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Color.Color;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Layout.Engine;

namespace Ofd.Layout.Element.Canvas;

/// <summary>
/// 绘制参数缓存（Java DrawParamCache）：属性变更后失效，再次加入资源时重新构建。
/// </summary>
[Obsolete("通过资源管理解决绘制参数重复问题，该缓存结构冗余因此弃用。")]
public class DrawParamCache
{
    private CT_DrawParam? _cache;

    private ST_RefID? _relative;
    private double? _lineWidth;
    private LineJoinType? _join;
    private LineCapType? _cap;
    private double? _dashOffset;
    private ST_Array? _dashPattern;
    private double? _miterLimit;
    private int[]? _fillColor;
    private int[]? _strokeColor;

    public DrawParamCache()
    {
        // 默认黑色
        _fillColor = new[] { 0, 0, 0 };
        _strokeColor = new[] { 0, 0, 0 };
    }

    public ST_ID? GetID() => _cache?.GetID();

    public DrawParamCache SetRelative(ST_RefID? relative)
    {
        _cache = null;
        _relative = relative;
        return this;
    }

    public ST_RefID? GetRelative() => _relative;

    public DrawParamCache SetLineWidth(double? lineWidth)
    {
        _cache = null;
        _lineWidth = lineWidth;
        return this;
    }

    public double? GetLineWidth() => _lineWidth;

    public DrawParamCache SetJoin(LineJoinType? join)
    {
        _cache = null;
        _join = join;
        return this;
    }

    public LineJoinType? GetJoin() => _join;

    public DrawParamCache SetCap(LineCapType? cap)
    {
        _cache = null;
        _cap = cap;
        return this;
    }

    public LineCapType? GetCap() => _cap;

    public DrawParamCache SetDashOffset(double? dashOffset)
    {
        _cache = null;
        _dashOffset = dashOffset;
        return this;
    }

    public double? GetDashOffset() => _dashOffset;

    public DrawParamCache SetDashPattern(ST_Array? dashPattern)
    {
        _cache = null;
        _dashPattern = dashPattern;
        return this;
    }

    public ST_Array? GetDashPattern() => _dashPattern;

    public DrawParamCache SetMiterLimit(double? miterLimit)
    {
        _cache = null;
        _miterLimit = miterLimit;
        return this;
    }

    public double? GetMiterLimit() => _miterLimit;

    public DrawParamCache SetFillColor(int[]? fillColor)
    {
        _cache = null;
        _fillColor = fillColor;
        return this;
    }

    public int[]? GetFillColor() => _fillColor;

    public DrawParamCache SetStrokeColor(int[]? strokeColor)
    {
        _cache = null;
        _strokeColor = strokeColor;
        return this;
    }

    public int[]? GetStrokeColor() => _strokeColor;

    /// <summary>构造绘制参数并加入资源；未修改属性时复用缓存。</summary>
    public ST_ID? AddToResource(ResManager resManager)
    {
        if (_cache != null) return _cache.GetID();

        var param = new CT_DrawParam();
        if (_relative != null) param.SetRelative(_relative);
        if (_lineWidth != null) param.SetLineWidth(_lineWidth);
        if (_join != null) param.SetJoin(_join);
        if (_cap != null) param.SetCap(_cap);
        if (_dashOffset != null) param.SetDashOffset(_dashOffset);
        if (_dashPattern != null) param.SetDashPattern(_dashPattern);
        if (_miterLimit != null) param.SetMiterLimit(_miterLimit);
        if (_fillColor != null) param.SetFillColor(CT_Color.Rgb(_fillColor));
        if (_strokeColor != null) param.SetStrokeColor(CT_Color.Rgb(_strokeColor));

        var id = resManager.AddDrawParam(param);
        _cache = param;
        return id;
    }

    public DrawParamCache Clone()
    {
        var that = new DrawParamCache
        {
            _cache = _cache,
            _relative = _relative,
            _lineWidth = _lineWidth,
            _join = _join,
            _cap = _cap,
            _dashOffset = _dashOffset,
            _dashPattern = _dashPattern,
            _miterLimit = _miterLimit,
            _fillColor = _fillColor == null ? null : (int[])_fillColor.Clone(),
            _strokeColor = _strokeColor == null ? null : (int[])_strokeColor.Clone(),
        };
        return that;
    }
}
