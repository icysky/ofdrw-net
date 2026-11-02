using System.Globalization;

namespace Ofd.Core.BasicType;

/// <summary>
/// 点坐标，以空格分割，前者为 x值，后者为 y值，可以是整数或浮点数。
/// 示例：0 0。——《GB/T 33190-2016》 表 2 基本数据类型
/// （Java org.ofdrw.core.basicType.ST_Pos）
/// </summary>
public class ST_Pos : STBase
{
    /// <summary>X坐标，从左到右。</summary>
    public double X { get; private set; } = 0d;

    /// <summary>Y坐标，从上到下。</summary>
    public double Y { get; private set; } = 0d;

    public ST_Pos(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static ST_Pos GetInstance(double x, double y) => new(x, y);

    /// <summary>获取 ST_Pos 实例，如果参数非法则返还 null。</summary>
    public static ST_Pos? GetInstance(string? arrStr)
    {
        if (arrStr == null || arrStr.Trim().Length == 0) return null;
        // Java split(" ")：连续空格会产生空串元素导致 length != 2 → null，此处保持一致
        string[] values = arrStr.Trim().Split(' ');
        if (values.Length != 2) return null;
        return new ST_Pos(
            double.Parse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture),
            double.Parse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture));
    }

    public ST_Pos SetX(double x)
    {
        X = x;
        return this;
    }

    public ST_Pos SetY(double y)
    {
        Y = y;
        return this;
    }

    public override string ToString() => Fmt(X) + " " + Fmt(Y);
}
