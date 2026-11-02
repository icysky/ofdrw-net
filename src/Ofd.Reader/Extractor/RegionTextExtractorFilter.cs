using System.Drawing;
using System.Globalization;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Text;

namespace Ofd.Reader.Extractor;

/// <summary>
/// 矩形框文本提取过滤器（Java RegionTextExtractorFilter）：
/// 按 TextCode 的起始坐标 + 逐字符 Delta 偏移（可叠加 CTM）判断字符是否落在矩形内。
/// </summary>
public class RegionTextExtractorFilter : IExtractorFilter
{
    /// <summary>文本提取矩形框。</summary>
    private readonly RectangleF _rectangle;

    public RegionTextExtractorFilter(RectangleF rectangle) => _rectangle = rectangle;

    /// <summary>提取 TextCode 在矩形区域内的文本；全部落在区域外返回 null。</summary>
    public string? GetAllowText(TextObject textObject, TextCode textCode)
    {
        string? content = textCode.GetContent();
        string? allowText = null;
        if (string.IsNullOrEmpty(content)) return null;

        var deltaX = DeltaTool.GetDelta(textCode.GetDeltaX(), content.Length);
        var deltaY = DeltaTool.GetDelta(textCode.GetDeltaY(), content.Length);
        EmptyDeltaHandle(content, deltaX, deltaY);

        var boundary = textObject.GetBoundary();
        double topLeftX = boundary?.TopLeftX ?? 0d;
        double topLeftY = boundary?.TopLeftY ?? 0d;

        double[]? matrix = GetMatrix(textObject.GetCTM());

        double sx = textCode.GetX() ?? 0d;
        double sy = textCode.GetY() ?? 0d;

        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < content.Length; i++)
        {
            char ch = content[i];
            if (matrix != null)
            {
                var transform = Transform(matrix, sx, sy);
                if (_rectangle.Contains((float)(topLeftX + transform.X), (float)(topLeftY + transform.Y)))
                    builder.Append(ch);
            }
            else
            {
                if (_rectangle.Contains((float)(topLeftX + sx), (float)(topLeftY + sy)))
                    builder.Append(ch);
            }
            sx += deltaX[i];
            sy += deltaY[i];
        }

        if (builder.Length != 0) allowText = builder.ToString();
        return allowText;
    }

    /// <summary>坐标变换：x' = m0*x + m2*y + m4，y' = m1*x + m3*y + m5。</summary>
    private static (double X, double Y) Transform(double[] matrix, double sx, double sy) =>
        (matrix[0] * sx + matrix[2] * sy + matrix[4],
         matrix[1] * sx + matrix[3] * sy + matrix[5]);

    private static double[]? GetMatrix(ST_Array? ctm)
    {
        if (ctm == null) return null;

        var array = ctm.GetArray();
        var matrix = new double[array.Count];
        for (int i = 0; i < array.Count; i++)
            matrix[i] = double.Parse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture);
        return matrix;
    }

    /// <summary>空 Delta 处理：以 0 填充到文本长度。</summary>
    private static void EmptyDeltaHandle(string content, List<float> deltaX, List<float> deltaY)
    {
        if (deltaX.Count == 0)
            for (int i = 0; i < content.Length; i++) deltaX.Add(0f);

        if (deltaY.Count == 0)
            for (int i = 0; i < content.Length; i++) deltaY.Add(0f);
    }
}
