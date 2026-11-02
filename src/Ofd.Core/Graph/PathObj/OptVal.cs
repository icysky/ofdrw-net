using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.Graph.Tight.Method;

namespace Ofd.Core.Graph.PathObj;

/// <summary>操作符和操作数（图形轮廓数据的最小单元）。</summary>
public class OptVal : ICloneable
{
    /// <summary>操作符（S/M/L/Q/B/A/C/CM）。</summary>
    public string Opt;

    /// <summary>操作数。</summary>
    public double[] Values = Array.Empty<double>();

    public OptVal(string opt)
    {
        Opt = opt;
    }

    public OptVal(string opt, double[]? values)
    {
        Opt = opt;
        if (values != null) Values = values;
    }

    /// <summary>以字符串操作数序列构造（无法解析的数字按 0，与 Java 一致）。</summary>
    public OptVal(string opt, List<string>? valueStrList)
    {
        Opt = opt;
        if (valueStrList != null)
        {
            Values = new double[valueStrList.Count];
            for (int i = 0; i < valueStrList.Count; i++)
            {
                try
                {
                    Values[i] = double.Parse(valueStrList[i], NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    Values[i] = 0;
                }
            }
        }
    }

    public string GetOpt() => Opt;

    public void SetOpt(string opt) => Opt = opt;

    public double[] GetValues() => Values;

    public void SetValues(double[] values) => Values = values;

    /// <summary>数组长度不足时补 0 到期待长度。</summary>
    public static double[] Filling(double[]? arr, int num)
    {
        if (arr == null) arr = new double[num];
        if (arr.Length < num) return arr.Concat(new double[num - arr.Length]).ToArray();
        return arr;
    }

    /// <summary>转换为非紧缩绘制指令；无法识别操作符返回 null。</summary>
    public Command? ToCmd()
    {
        switch (Opt)
        {
            case "S":
            case "M":
            {
                var arr = Filling(Values, 2);
                return new Move(arr[0], arr[1]);
            }
            case "L":
            {
                var arr = Filling(Values, 2);
                return new Line(arr[0], arr[1]);
            }
            case "Q":
            {
                var arr = Filling(Values, 4);
                return new QuadraticBezier(arr[0], arr[1], arr[2], arr[3]);
            }
            case "B":
            {
                var arr = Filling(Values, 6);
                return new CubicBezier(arr[0], arr[1], arr[2], arr[3], arr[4], arr[5]);
            }
            case "A":
            {
                var arr = Filling(Values, 7);
                return new Arc(arr[0], arr[1], arr[2], arr[3], arr[4], arr[5], arr[6]);
            }
            case "C":
                return new Close();
            default:
                return null;
        }
    }

    /// <summary>获取期待数量的参数值（不足补 0）。</summary>
    public double[] ExpectValues() => Opt switch
    {
        "S" or "M" or "L" => Filling(Values, 2),
        "Q" => Filling(Values, 4),
        "B" => Filling(Values, 6),
        "A" => Filling(Values, 7),
        _ => Array.Empty<double>(),
    };

    public OptVal Clone() => new(Opt, (double[])Values.Clone());

    object ICloneable.Clone() => Clone();

    /// <summary>序列化为紧缩形式（数值走 fmt）。</summary>
    public override string ToString()
    {
        switch (Opt)
        {
            case "S":
            case "M":
            case "L":
            {
                var arr = Filling(Values, 2);
                return $"{Opt} {STBase.Fmt(arr[0])} {STBase.Fmt(arr[1])}";
            }
            case "Q":
            {
                var arr = Filling(Values, 4);
                return $"Q {STBase.Fmt(arr[0])} {STBase.Fmt(arr[1])} {STBase.Fmt(arr[2])} {STBase.Fmt(arr[3])}";
            }
            case "B":
            {
                var arr = Filling(Values, 6);
                return $"B {STBase.Fmt(arr[0])} {STBase.Fmt(arr[1])} {STBase.Fmt(arr[2])} {STBase.Fmt(arr[3])} {STBase.Fmt(arr[4])} {STBase.Fmt(arr[5])}";
            }
            case "A":
            {
                var arr = Filling(Values, 7);
                return $"A {STBase.Fmt(arr[0])} {STBase.Fmt(arr[1])} {STBase.Fmt(arr[2])} {STBase.Fmt(arr[3])} {STBase.Fmt(arr[4])} {STBase.Fmt(arr[5])} {STBase.Fmt(arr[6])}";
            }
            case "C":
                return "C";
            default:
                return "";
        }
    }
}
