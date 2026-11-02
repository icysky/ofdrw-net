using System.Globalization;

namespace Ofd.Core.BasicType;

/// <summary>
/// 矩形区域，以空格分割，前两个值代表该矩形左上角坐标，后两个值依次为宽和高，
/// 可以是整数或浮点数，后两个值应大于 0。示例：10 10 50 50。
/// ——《GB/T 33190-2016》 表 2 基本数据类型
/// （Java org.ofdrw.core.basicType.ST_Box）
/// 注：Java 版的 varargs 构造器存在检查顺序错误（对任意 4 元数组必然抛异常，
/// 属上游死代码），未移植；调用方一律使用本四参构造器或 GetInstance。
/// </summary>
public class ST_Box : STBase
{
    /// <summary>左上角 x坐标，从左到右。</summary>
    public double TopLeftX { get; private set; } = 0d;

    /// <summary>左上角 y坐标，从上到下。</summary>
    public double TopLeftY { get; private set; } = 0d;

    /// <summary>宽度。</summary>
    public double Width { get; private set; } = 0d;

    /// <summary>高度。</summary>
    public double Height { get; private set; } = 0d;

    public ST_Box(double topLeftX, double topLeftY, double width, double height)
    {
        TopLeftX = topLeftX;
        TopLeftY = topLeftY;
        Width = width;
        Height = height;
    }

    /// <summary>获取 ST_Box 实例，如果参数非法则返还 null。</summary>
    public static ST_Box? GetInstance(string? arrStr)
    {
        if (arrStr == null || arrStr.Trim().Length == 0) return null;
        string[] values = arrStr.Trim().Split(' ');
        if (values.Length != 4) return null;
        return new ST_Box(ToDouble(values[0]), ToDouble(values[1]), ToDouble(values[2]), ToDouble(values[3]));
    }

    public ST_Box SetTopLeftX(double v)
    {
        TopLeftX = v;
        return this;
    }

    public ST_Box SetTopLeftY(double v)
    {
        TopLeftY = v;
        return this;
    }

    public ST_Box SetWidth(double v)
    {
        Width = v;
        return this;
    }

    public ST_Box SetHeight(double v)
    {
        Height = v;
        return this;
    }

    /// <summary>获取左上角坐标定点。</summary>
    public ST_Pos GetTopLeftPos() => new(TopLeftX, TopLeftY);

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", Fmt(TopLeftX), Fmt(TopLeftY), Fmt(Width), Fmt(Height));

    public ST_Box Clone() => new(TopLeftX, TopLeftY, Width, Height);
}
