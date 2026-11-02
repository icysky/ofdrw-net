using System.Collections;
using Ofd.Core.BasicType;

namespace Ofd.Core.Graph.PathObj;

/// <summary>
/// 图形轮廓数据（9.1 表 35/36）：由一系列紧缩的操作符和操作数构成。
/// 注：Java 上游 equals 对同类直接返回 false（明显 bug），C# 修正为按序列化结果比较。
/// </summary>
public class AbbreviatedData : OFDElement, IEnumerable<OptVal>, ICloneable
{
    /// <summary>绘制数据队列。</summary>
    private List<OptVal> _dataQueue;

    private static readonly HashSet<string> Opts = new(StringComparer.Ordinal)
    { "S", "M", "L", "Q", "B", "A", "C", "CM" };

    public AbbreviatedData(Dom.DomElement proxy) : base(proxy)
    {
        _dataQueue = Parse(ElementText(proxy));
    }

    public AbbreviatedData() : base("AbbreviatedData")
    {
        _dataQueue = new List<OptVal>();
    }

    public AbbreviatedData(List<OptVal> list) : this()
    {
        _dataQueue = new List<OptVal>(list);
    }

    /// <summary>解析紧缩字符串为数据队列（多空格视为一个；无法解析的数字按 0）。</summary>
    public static List<OptVal> Parse(string? dataStr)
    {
        var res = new List<OptVal>();
        if (string.IsNullOrEmpty(dataStr)) return res;

        var arr = dataStr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        string? opt = null;
        var values = new List<string>();
        foreach (var s in arr)
        {
            if (Opts.Contains(s))
            {
                if (opt != null) res.Add(new OptVal(opt, values));
                values = new List<string>();
                opt = s;
            }
            else
            {
                values.Add(s);
            }
        }
        if (opt != null) res.Add(new OptVal(opt, values));
        return res;
    }

    /// <summary>追加整个图形轮廓数据。</summary>
    public AbbreviatedData Append(AbbreviatedData? another)
    {
        if (another == null) return this;
        _dataQueue.AddRange(another._dataQueue);
        return this;
    }

    /// <summary>获取原始操作符和操作数。</summary>
    public List<OptVal> GetRawOptVal() => _dataQueue;

    /// <summary>刷新元素文本（每次修改后需 flush 才会进入 XML）。</summary>
    public AbbreviatedData Flush()
    {
        SetText(ToString());
        return this;
    }

    /// <summary>定义自绘制图形边线起始点坐标（S）。</summary>
    public AbbreviatedData DefineStart(double x, double y)
    {
        _dataQueue.Add(new OptVal("S", new[] { x, y }));
        return this;
    }

    public AbbreviatedData S(ST_Pos start) => DefineStart(start.X, start.Y);

    public AbbreviatedData S(double x, double y) => DefineStart(x, y);

    /// <summary>当前点移动到指定点（M）。</summary>
    public AbbreviatedData MoveTo(double x, double y)
    {
        _dataQueue.Add(new OptVal("M", new[] { x, y }));
        return this;
    }

    public AbbreviatedData M(double x, double y) => MoveTo(x, y);

    public AbbreviatedData M(ST_Pos target) => MoveTo(target.X, target.Y);

    /// <summary>连接线段（L）。</summary>
    public AbbreviatedData LineTo(double x, double y)
    {
        _dataQueue.Add(new OptVal("L", new[] { x, y }));
        return this;
    }

    public AbbreviatedData L(double x, double y) => LineTo(x, y);

    public AbbreviatedData L(ST_Pos p) => LineTo(p.X, p.Y);

    /// <summary>二次贝塞尔曲线（Q）。</summary>
    public AbbreviatedData QuadraticBezier(double x1, double y1, double x2, double y2)
    {
        _dataQueue.Add(new OptVal("Q", new[] { x1, y1, x2, y2 }));
        return this;
    }

    public AbbreviatedData Q(double x1, double y1, double x2, double y2) => QuadraticBezier(x1, y1, x2, y2);

    public AbbreviatedData Q(ST_Pos control, ST_Pos target) =>
        QuadraticBezier(control.X, control.Y, target.X, target.Y);

    /// <summary>三次贝塞尔曲线（B）。</summary>
    public AbbreviatedData CubicBezier(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        _dataQueue.Add(new OptVal("B", new[] { x1, y1, x2, y2, x3, y3 }));
        return this;
    }

    public AbbreviatedData B(double x1, double y1, double x2, double y2, double x3, double y3) =>
        CubicBezier(x1, y1, x2, y2, x3, y3);

    public AbbreviatedData B(ST_Pos control1, ST_Pos control2, ST_Pos target) =>
        CubicBezier(control1.X, control1.Y, control2.X, control2.Y, target.X, target.Y);

    /// <summary>圆弧（A）：large/sweep 只接受 0/1。</summary>
    public AbbreviatedData Arc(double rx, double ry, double angle, int large, int sweep, double x, double y)
    {
        if (large != 0 && large != 1)
            throw new ArgumentException("large 只接受 0 或 1");
        if (sweep != 0 && sweep != 1)
            throw new ArgumentException("sweep 只接受 0 或 1");
        _dataQueue.Add(new OptVal("A", new double[] { rx, ry, angle, large, sweep, x, y }));
        return this;
    }

    public AbbreviatedData A(double rx, double ry, double angle, int large, int sweep, double x, double y) =>
        Arc(rx, ry, angle, large, sweep, x, y);

    public AbbreviatedData A(double rx, double ry, double angle, int large, int sweep, ST_Pos target) =>
        Arc(rx, ry, angle, large, sweep, target.X, target.Y);

    /// <summary>自动闭合（C）。</summary>
    public AbbreviatedData ClosePath()
    {
        _dataQueue.Add(new OptVal("C"));
        return this;
    }

    /// <summary>自动闭合（C，Java 方法名 C）。</summary>
    public AbbreviatedData C() => ClosePath();

    /// <summary>撤销上一步操作。</summary>
    public AbbreviatedData Undo()
    {
        if (_dataQueue.Count > 0) _dataQueue.RemoveAt(_dataQueue.Count - 1);
        return this;
    }

    /// <summary>序列化为紧缩操作序列（各操作间单空格）。</summary>
    public override string ToString()
    {
        if (_dataQueue.Count == 0) return "";
        return string.Join(" ", _dataQueue.Select(v => v.ToString()));
    }

    /// <summary>深拷贝并刷新元素文本。</summary>
    public AbbreviatedData Clone()
    {
        var clone = new AbbreviatedData();
        clone._dataQueue = _dataQueue.Select(v => v.Clone()).ToList();
        clone.Flush();
        return clone;
    }

    object ICloneable.Clone() => Clone();

    /// <summary>绘制操作数量。</summary>
    public int Size() => _dataQueue.Count;

    public IEnumerator<OptVal> GetEnumerator() => _dataQueue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override bool Equals(object? obj)
    {
        if (obj is AbbreviatedData other) return ToString() == other.ToString();
        return obj != null && ToString() == obj.ToString();
    }

    public override int GetHashCode() => ToString().GetHashCode();
}
