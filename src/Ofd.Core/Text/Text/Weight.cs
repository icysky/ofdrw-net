using System.Globalization;

namespace Ofd.Core.Text.Text;

/// <summary>文字对象的粗细值（11.3 表 45）：100~900，默认 400。</summary>
public enum Weight
{
    W_100 = 100,
    W_200 = 200,
    W_300 = 300,
    W_400 = 400,
    W_500 = 500,
    W_600 = 600,
    W_700 = 700,
    W_800 = 800,
    W_900 = 900,
}

public static class WeightExtensions
{
    /// <summary>获取粗细值，空值默认 400，非法值抛异常（与 Java 一致）。</summary>
    public static Weight GetInstance(string? weight)
    {
        if (weight == null || weight.Trim().Length == 0) weight = "400";
        return weight switch
        {
            "100" => Weight.W_100,
            "200" => Weight.W_200,
            "300" => Weight.W_300,
            "400" => Weight.W_400,
            "500" => Weight.W_500,
            "600" => Weight.W_600,
            "700" => Weight.W_700,
            "800" => Weight.W_800,
            "900" => Weight.W_900,
            _ => throw new ArgumentException("错误的文字对象的粗细值：" + weight),
        };
    }

    public static Weight GetInstance(int weight) => GetInstance(weight.ToString(CultureInfo.InvariantCulture));

    /// <summary>XML 取值（数字字符串）。</summary>
    public static string ToXmlValue(this Weight weight) => ((int)weight).ToString(CultureInfo.InvariantCulture);
}
