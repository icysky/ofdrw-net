using System.Globalization;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 25：纯横向缩放应使用 HScale 而非 CTM（GB/T 42133-2022 6.6）。
/// </summary>
public class TextHScaleRule : IArchiveRule
{
    public const string RULE_NAME = "TEXT_HSCALE";

    /// <summary>CTM 矩阵元素数。</summary>
    private const int CTM_ELEMENTS = 6;

    /// <summary>浮点比较容差。</summary>
    private const double EPSILON = 1e-6;

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                var pageLoc = reader.GetPageAbsLoc(i);

                foreach (var textObj in DomWalker.Descendants(page.GetProxy(), "TextObject"))
                {
                    string? ctmStr = textObj.Attr("CTM");
                    if (string.IsNullOrEmpty(ctmStr)) continue;

                    var ctm = ParseCtm(ctmStr);
                    if (ctm == null) continue;

                    // 纯横向缩放：b≈0、c≈0、d≈1、a≠1
                    bool pureHorizontal =
                        Math.Abs(ctm[1]) < EPSILON &&
                        Math.Abs(ctm[2]) < EPSILON &&
                        Math.Abs(ctm[3] - 1) < EPSILON &&
                        Math.Abs(ctm[0] - 1) > EPSILON;

                    if (!pureHorizontal) continue;

                    string? hScale = textObj.Attr("HScale");
                    if (!string.IsNullOrEmpty(hScale)) continue;

                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Warn,
                        $"TextObject 的 CTM 为纯横向缩放 (a={ctm[0].ToString("0.000", CultureInfo.InvariantCulture)})，建议使用 HScale 属性代替",
                        pageLoc,
                        "CTM: " + ctmStr,
                        "HScale=" + ctm[0].ToString("0.000", CultureInfo.InvariantCulture)));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查 HScale 时异常: " + e.Message));
        }

        return violations;
    }

    /// <summary>解析 CTM 字符串（6 个空格分隔的数字），失败返回 null。</summary>
    private static double[]? ParseCtm(string ctmStr)
    {
        string[] parts = ctmStr.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != CTM_ELEMENTS) return null;

        var result = new double[CTM_ELEMENTS];
        for (int i = 0; i < CTM_ELEMENTS; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                return null;
        }
        return result;
    }
}
