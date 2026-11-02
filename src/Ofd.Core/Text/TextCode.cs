using System.Globalization;
using Ofd.Core.BasicType;
using Ofd.Core.PageDescription.Clips;

namespace Ofd.Core.Text;

/// <summary>
/// 文字定位（11.3 图 61 表 46）：TextCode 内容为字符编码串，
/// DeltaX/DeltaY 表示逐字符偏移（支持 "g" 重复压缩语法）。
/// </summary>
public class TextCode : OFDElement, IClipAble
{
    private const string G_FLAG = "g";

    public TextCode(Dom.DomElement proxy) : base(proxy) { }

    public TextCode() : base("TextCode") { }

    /// <summary>设置文字内容。</summary>
    public TextCode SetContent(string content)
    {
        SetText(content);
        return this;
    }

    /// <summary>获取文字内容。</summary>
    public string? GetContent() => GetText();

    /// <summary>设置坐标（X、Y）。</summary>
    public TextCode SetCoordinate(double? x, double? y) => SetX(x).SetY(y);

    /// <summary>【可选属性】设置 首字符 X 坐标（null 删除属性，沿用上一个 TextCode 的 X）。</summary>
    public TextCode SetX(double? x)
    {
        if (x == null)
        {
            RemoveAttr("X");
            return this;
        }
        return (TextCode)AddAttribute("X", STBase.Fmt(x.Value));
    }

    /// <summary>【可选属性】获取 首字符 X 坐标，null 表示沿用上一个 TextCode。</summary>
    public double? GetX() => GetDoubleOrNull("X");

    /// <summary>【可选属性】设置 首字符 Y 坐标（null 删除属性）。</summary>
    public TextCode SetY(double? y)
    {
        if (y == null)
        {
            RemoveAttr("Y");
            return this;
        }
        return (TextCode)AddAttribute("Y", STBase.Fmt(y.Value));
    }

    /// <summary>【可选属性】获取 首字符 Y 坐标，null 表示沿用上一个 TextCode。</summary>
    public double? GetY() => GetDoubleOrNull("Y");

    /// <summary>【可选属性】设置 逐字符 X 偏移（null 删除属性）。</summary>
    public TextCode SetDeltaX(ST_Array? deltaX)
    {
        if (deltaX == null)
        {
            RemoveAttr("DeltaX");
            return this;
        }
        return (TextCode)AddAttribute("DeltaX", deltaX.ToString());
    }

    /// <summary>【可选属性】设置 逐字符 X 偏移（数值形式）。</summary>
    public TextCode SetDeltaX(params double[] arr) => SetDeltaX(new ST_Array(arr.Cast<object?>().ToArray()));

    /// <summary>【可选属性】获取 逐字符 X 偏移（解析 g 压缩语法），null 表示不偏移。</summary>
    public ST_Array? GetDeltaX()
    {
        var str = AttributeValue("DeltaX");
        if (str == null || str.Trim().Length == 0) return null;
        return ST_Array.GetInstance(DeltaFormatter(str));
    }

    /// <summary>【可选属性】设置 逐字符 Y 偏移（null 删除属性）。</summary>
    public TextCode SetDeltaY(ST_Array? deltaY)
    {
        if (deltaY == null)
        {
            RemoveAttr("DeltaY");
            return this;
        }
        return (TextCode)AddAttribute("DeltaY", deltaY.ToString());
    }

    /// <summary>【可选属性】设置 逐字符 Y 偏移（数值形式）。</summary>
    public TextCode SetDeltaY(params double[] arr) => SetDeltaY(new ST_Array(arr.Cast<object?>().ToArray()));

    /// <summary>【可选属性】获取 逐字符 Y 偏移（解析 g 压缩语法），null 表示不偏移。</summary>
    public ST_Array? GetDeltaY()
    {
        var str = AttributeValue("DeltaY");
        if (str == null || str.Trim().Length == 0) return null;
        return ST_Array.GetInstance(DeltaFormatter(str));
    }

    /// <summary>
    /// 解析 delta 值的 "g" 压缩语法：形如 "1 g 3 2" 表示数值 2 重复 3 次。
    /// </summary>
    private static string DeltaFormatter(string delta)
    {
        if (!delta.Contains(G_FLAG)) return delta;

        var tempList = delta.Split(' ').ToList();
        bool gFlag = false, gProcessing = false;
        int gItemCount = 0;
        var floatList = new List<string>();

        foreach (var s in tempList)
        {
            if (G_FLAG.Equals(s))
            {
                gFlag = true;
                continue;
            }
            if (s == null || s.Trim().Length == 0) continue;

            if (gFlag)
            {
                gItemCount = int.Parse(s.Trim(), CultureInfo.InvariantCulture);
                gProcessing = true;
                gFlag = false;
            }
            else if (gProcessing)
            {
                for (int j = 0; j < gItemCount; j++)
                    floatList.Add(s);
                gProcessing = false;
            }
            else
            {
                floatList.Add(s);
            }
        }

        var sb = new System.Text.StringBuilder();
        foreach (var item in floatList)
            sb.Append(' ').Append(item);
        return sb.ToString().Trim();
    }

    private double? GetDoubleOrNull(string name)
    {
        var str = AttributeValue(name);
        if (str == null || str.Trim().Length == 0) return null;
        return double.Parse(str.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
