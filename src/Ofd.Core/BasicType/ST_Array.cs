using System.Globalization;
using System.Text;

namespace Ofd.Core.BasicType;

/// <summary>
/// 数组，以空格分割元素。元素可以是除 ST_Loc、ST_Array 外的数据类型，不可嵌套。
/// 示例：1 2.0 5.0。——《GB/T 33190-2016》 表 2 基本数据类型
/// （Java org.ofdrw.core.basicType.ST_Array）
/// </summary>
public class ST_Array : STBase, IEquatable<ST_Array>
{
    /// <summary>元素收容（保持插入序）。</summary>
    private List<string> _array = new();

    /// <summary>获取一个单位矩阵变换参数（1 0 0 1 0 0）。</summary>
    public static ST_Array UnitCTM() => new("1", "0", "0", "1", "0", "0");

    /// <summary>
    /// 矩阵相乘（6 元素 CTM：a b c d e f）。
    /// </summary>
    public ST_Array MtxMul(ST_Array array)
    {
        if (_array.Count != 6 || array.Size() != 6)
            throw new ArgumentException("矩阵乘法数组规模必须 6元素(a b c d e f)");
        double[][] a = ToMtx();
        double[][] b = array.ToMtx();

        var res = new double[3][];
        for (int i = 0; i < 3; i++) res[i] = new double[3];

        for (int k = 0; k < 3; k++)
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    res[i][j] += a[i][k] * b[k][j];

        return new ST_Array(
            Fmt(res[0][0]), Fmt(res[0][1]),
            Fmt(res[1][0]), Fmt(res[1][1]),
            Fmt(res[2][0]), Fmt(res[2][1]));
    }

    /// <summary>获取 ST_Array 实例，如果参数非法则返还 null。</summary>
    public static ST_Array? GetInstance(string? arrStr)
    {
        if (arrStr == null || "null".Equals(arrStr, StringComparison.Ordinal) || arrStr.Trim().Length == 0)
            return null;
        // Java split("\\s+")：按连续空白切分
        return new ST_Array(arrStr.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>通用构造：null/空白串跳过，double 经 Fmt 格式化，其余 ToString。</summary>
    public ST_Array(params object?[] arr)
    {
        if (arr == null) throw new ArgumentException("参数不能为空");
        _array = new List<string>(arr.Length);
        foreach (var item in arr)
        {
            if (item == null) continue;
            object? v = item;
            if (v is string s)
            {
                if (s.Trim().Length == 0) continue;
            }
            else if (v is double d)
            {
                v = Fmt(d);
            }
            _array.Add(v.ToString()!);
        }
    }

    /// <summary>
    /// 创建变换矩阵（注意：Java 版此处用 Double.toString 而非 fmt，
    /// 整数输出带 ".0"，如 "1.0 0.0 0.0 1.0 0.0 0.0"）。
    /// </summary>
    public ST_Array(double a, double b, double c, double d, double e, double f)
    {
        _array = new List<string>(6)
        {
            JavaDoubleToString(a), JavaDoubleToString(b), JavaDoubleToString(c),
            JavaDoubleToString(d), JavaDoubleToString(e), JavaDoubleToString(f),
        };
    }

    public ST_Array Add(string item)
    {
        _array.Add(item);
        return this;
    }

    public List<string> GetArray() => _array;

    /// <summary>获取期待长度的字符串数组：长度不足自动填充空白字符，超出则截取。</summary>
    public string[] ExpectStrArr(int expectCount)
    {
        var res = new string[expectCount];
        for (int i = 0; i < expectCount; i++)
            res[i] = i < _array.Count ? _array[i] : "";
        return res;
    }

    /// <summary>获取浮点数组（尽力解析）。</summary>
    public double[] ToDouble() => _array.Select(ToDouble).ToArray();

    /// <summary>获取整形数组（尽力解析）。</summary>
    public int[] ToInt() => _array.Select(ToInt).ToArray();

    public ST_Array SetArray(List<string> array)
    {
        _array = array;
        return this;
    }

    /// <summary>数组长度。</summary>
    public int Size() => _array.Count;

    /// <summary>反序列化为 3x3 矩阵数组（要求恰 6 元素）。</summary>
    public double[][] ToMtx()
    {
        if (Size() != 6) throw new ArgumentException("矩阵数组必须有 9个元素");
        double[][] mtx = new double[3][];
        for (int i = 0; i < 3; i++) mtx[i] = new double[3];
        mtx[0][0] = double.Parse(_array[0], NumberStyles.Float, CultureInfo.InvariantCulture);
        mtx[0][1] = double.Parse(_array[1], NumberStyles.Float, CultureInfo.InvariantCulture);
        mtx[0][2] = 0;
        mtx[1][0] = double.Parse(_array[2], NumberStyles.Float, CultureInfo.InvariantCulture);
        mtx[1][1] = double.Parse(_array[3], NumberStyles.Float, CultureInfo.InvariantCulture);
        mtx[1][2] = 0;
        mtx[2][0] = double.Parse(_array[4], NumberStyles.Float, CultureInfo.InvariantCulture);
        mtx[2][1] = double.Parse(_array[5], NumberStyles.Float, CultureInfo.InvariantCulture);
        mtx[2][2] = 1;
        return mtx;
    }

    /// <summary>获取 index 上的数字（浮点）。</summary>
    public double Get(int index) =>
        double.Parse(_array[index], NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>获取 index 上的数字（整数，支持 "#" 前缀十六进制）。</summary>
    public int GetInt(int index)
    {
        string s = _array[index];
        return s.StartsWith('#')
            ? int.Parse(s[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    /// <summary>获取期待长度的 int 数组，无法解析或长度不足则补充 0。</summary>
    public int[] ExpectIntArr(int len)
    {
        var res = new int[len];
        for (int i = 0; i < len; i++)
        {
            if (i >= _array.Count) break;
            try
            {
                res[i] = GetInt(i);
            }
            catch (FormatException)
            {
                res[i] = 0;
            }
            catch (OverflowException)
            {
                res[i] = 0;
            }
            catch (ArgumentOutOfRangeException)
            {
                res[i] = 0;
            }
        }
        return res;
    }

    /// <summary>获取期待长度的 double 数组，无法解析或长度不足则补充 0。</summary>
    public double[] ExpectArr(int len)
    {
        var res = new double[len];
        for (int i = 0; i < len; i++)
        {
            if (i >= _array.Count) break;
            try
            {
                res[i] = Get(i);
            }
            catch (FormatException)
            {
                res[i] = 0;
            }
        }
        return res;
    }

    public ST_Array Clone()
    {
        var res = new ST_Array();
        foreach (string item in _array)
            res.Add(item);
        return res;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (string item in _array)
            sb.Append(' ').Append(item);
        return sb.ToString().Trim();
    }

    public bool Equals(ST_Array? other) => other != null && _array.SequenceEqual(other._array);

    public override bool Equals(object? obj) => obj is ST_Array other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int h = 17;
            foreach (var s in _array)
                h = h * 31 + (s?.GetHashCode() ?? 0);
            return h;
        }
    }
}
