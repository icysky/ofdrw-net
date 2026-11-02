using System.Globalization;

namespace Ofd.Core.BasicType;

/// <summary>
/// ST 基础类型的公共工具（对应 Java org.ofdrw.core.basicType.STBase，abstract 值类型基类）。
/// </summary>
public abstract class STBase
{
    /// <summary>对应 Java STBase.MAXKEEP：fmt 小数保留位数上限。</summary>
    public const int MaxKeep = 3;

    /// <summary>
    /// 数值格式化，与 Java STBase.fmt(double) 输出字节级一致。
    /// golden 基准：tests/Ofd.Tests/TestData/fmt-java.txt（3049/3049 全绿，见 docs/ofdrw-net10-highrisk-recheck.md）。
    /// </summary>
    public static string Fmt(double d)
    {
        if (d == JavaLongCast(d)) return JavaLongCast(d).ToString(CultureInfo.InvariantCulture);
        if (-1 < d && d < 1)
        {
            string str = JavaF(d, 8);
            if (str == "0.00000000") return "0";
            int len = str.Length;
            for (int i = 0; i < len; i++)
            {
                char c = str[i];
                if (c - '0' <= 0) continue;
                if (i + MaxKeep <= len) return str.Substring(0, i + MaxKeep);
                return str.Substring(0, len);
            }
            return str;
        }
        return JavaF(d, MaxKeep);
    }

    /// <summary>Java (long) 强转语义：NaN→0、饱和到 long 边界、向零截断。</summary>
    public static long JavaLongCast(double d)
    {
        if (double.IsNaN(d)) return 0;
        if (d >= 9223372036854775807.0) return long.MaxValue;
        if (d <= -9223372036854775808.0) return long.MinValue;
        return (long)d;
    }

    /// <summary>
    /// Java Formatter %f 语义：对最短往返十进制表示做 HALF_UP（AwayFromZero）舍入，
    /// 而非对二进制精确值舍入（.NET "F" 的行为）。用 decimal（28 位精度足够 17 位有效数字）复刻。
    /// </summary>
    internal static string JavaF(double d, int digits)
    {
        if (double.IsNaN(d)) return "NaN";
        if (double.IsPositiveInfinity(d)) return "Infinity";
        if (double.IsNegativeInfinity(d)) return "-Infinity";
        try
        {
            string shortest = d.ToString("R", CultureInfo.InvariantCulture);
            decimal m = decimal.Parse(shortest, NumberStyles.Float, CultureInfo.InvariantCulture);
            decimal rounded = Math.Round(m, digits, MidpointRounding.AwayFromZero);
            string r = rounded.ToString("F" + digits, CultureInfo.InvariantCulture);
            // 负数舍入到 0 时 Java Formatter 保留负号（如 -1e-9 %.8f → "-0.00000000"）
            if (m < 0 && rounded == 0m) r = "-" + r;
            return r;
        }
        catch (OverflowException) // 次正规数（5E-324）在 decimal 下溢出 → 十进制上等同 0
        {
            return (0m).ToString("F" + digits, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// 转换为简单 OFD 元素（Java STBase.getElement(name)）。
    /// </summary>
    public OFDSimpleTypeElement GetElement(string name) => new(name, ToString());

    /// <summary>
    /// 字符串转 double，解析失败时按 Java STBase.num 语义"尽力解析"：
    /// 过滤仅保留 - + . 数字后重试，再失败返回 0。
    /// </summary>
    public static double ToDouble(string str)
    {
        var res = Num(str, s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture));
        return res ?? 0;
    }

    /// <summary>字符串转 int，尽力解析语义同 <see cref="ToDouble"/>，失败返回 0。</summary>
    public static int ToInt(string str)
    {
        var res = Num(str, s => int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture));
        return res ?? 0;
    }

    private static R? Num<R>(string str, Func<string, R> mapper) where R : struct
    {
        try
        {
            return mapper(str);
        }
        catch (Exception)
        {
            // 尽最大可能解析：过滤仅保留 - + . 与数字
            var sb = new System.Text.StringBuilder();
            foreach (char c in str)
            {
                if (c is '-' or '+' or '.')
                {
                    sb.Append(c);
                    continue;
                }
                if (c is >= '0' and <= '9')
                    sb.Append(c);
            }
            try
            {
                return mapper(sb.ToString());
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Java Double.toString(double) 语义复刻（区别于 .NET "R"：整数带 ".0"、
    /// 科学计数阈值 [1e-3, 1e7)、指数无符号位）。用于 ST_Array 六参构造等
    /// Java 侧走 Double.toString 的输出点。
    /// </summary>
    public static string JavaDoubleToString(double d)
    {
        if (double.IsNaN(d)) return "NaN";
        if (double.IsPositiveInfinity(d)) return "Infinity";
        if (double.IsNegativeInfinity(d)) return "-Infinity";
        if (d == 0) return 1 / d < 0 ? "-0.0" : "0.0";

        bool neg = d < 0;
        double m = Math.Abs(d);
        string mant = m.ToString("R", CultureInfo.InvariantCulture);

        string result;
        if (mant.Contains('E'))
        {
            // R 已给出科学计数（形如 1.2345E+20）→ 重排为 Java 形式 1.2345E20
            int eIdx = mant.IndexOf('E');
            string mm = mant[..eIdx];
            int exp = int.Parse(mant[(eIdx + 1)..], CultureInfo.InvariantCulture);
            if (!mm.Contains('.')) mm += ".0";
            result = mm + "E" + exp;
        }
        else if (m >= 1e-3 && m < 1e7)
        {
            if (!mant.Contains('.')) mant += ".0";
            result = mant;
        }
        else
        {
            // 十进制域外但 R 输出十进制形式 → 换算为 Java 科学计数
            int exp = 0;
            // 通过缩放得到 [1,10) 内的最短尾数：利用 R 的指数输出最稳定
            string scaled = (m * Math.Pow(10, -Math.Floor(Math.Log10(m)))).ToString("R", CultureInfo.InvariantCulture);
            exp = (int)Math.Floor(Math.Log10(m));
            if (scaled.Contains('E')) // 极端边界：缩放后仍是科学计数
                scaled = double.Parse(scaled, NumberStyles.Float, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);
            if (!scaled.Contains('.')) scaled += ".0";
            result = scaled + "E" + exp;
        }
        return neg ? "-" + result : result;
    }
}
