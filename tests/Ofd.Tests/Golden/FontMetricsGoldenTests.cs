using System.Globalization;
using Ofd.Font;
using Xunit;

namespace Ofd.Tests.Golden;

/// <summary>
/// SkiaSharp 度量与 Java AWT FontMetrics 的 golden 对照。
/// 基准由 tools/probe 的 Java 探针生成（8 字体 × 360 字符 M 行）。
/// 实测 DirectWrite 与 AWT 同源 hmtx，逐字符相对偏差 0；阈值 0.02 为回归护栏。
/// </summary>
public class FontMetricsGoldenTests
{
    private static string GoldenPath => Path.Combine(AppContext.BaseDirectory, "TestData", "font-java.txt");

    /// <summary>
    /// Java 侧可按名解析、.NET 侧需别名/回退链支持的字体（P4 别名表的验收对象）：
    /// 中文名（宋体/微软雅黑——Skia 按名解析不到）与 Java 逻辑字体（Serif——AWT 聚合实现）。
    /// 这些行的宽度对照在 P4 引入别名表后纳入断言。
    /// </summary>
    private static bool NeedsAlias(string fontName) =>
        fontName.Any(static c => c > 127) || fontName is "Serif" or "SansSerif" or "Monospaced" or "Dialog" or "DialogInput";

    [Fact]
    public void MeasureWidth_matches_java_font_metrics()
    {
        Assert.True(File.Exists(GoldenPath), "缺少 golden 文件 font-java.txt");

        int total = 0, missFont = 0, pendingAlias = 0;
        double maxRel = 0;
        var diffs = new List<string>();
        var coveredFonts = new HashSet<string>();

        foreach (var line in File.ReadLines(GoldenPath))
        {
            if (!line.StartsWith("M\t")) continue;
            var p = line.Split('\t');
            string fontName = p[1];
            int cp = int.Parse(p[2], CultureInfo.InvariantCulture);
            double wJava = double.Parse(p[3], CultureInfo.InvariantCulture);

            if (NeedsAlias(fontName))
            {
                pendingAlias++;
                continue;
            }
            if (FontMetrics.Resolve(fontName) == null)
            {
                missFont++;
                continue;
            }

            string s = char.ConvertFromUtf32(cp);
            float wNet = FontMetrics.MeasureWidth(fontName, s);
            double rel = Math.Abs(wNet - wJava) / Math.Max(wJava, 1e-9);
            total++;
            coveredFonts.Add(fontName);
            maxRel = Math.Max(maxRel, rel);
            if (rel > 0.02 && diffs.Count < 20)
                diffs.Add($"{fontName} U+{cp:X4} '{s}' java={wJava:0.000000} skia={wNet:0.000000} rel={rel:P2}");
        }

        Assert.True(total >= 2500, $"物理字体度量采样数异常: {total}（应约 2880，或运行环境缺字体 missing={missFont}）");
        Assert.True(pendingAlias >= 1000, $"待别名 golden 行异常: {pendingAlias}（应约 1080：宋体/微软雅黑/Serif 各 360）");
        Assert.True(diffs.Count == 0,
            $"度量不一致 {diffs.Count}+（maxRel={maxRel:P4}），前 20 条:\n{string.Join("\n", diffs)}");
        Assert.True(coveredFonts.Count >= 5, $"可解析字体数异常: {coveredFonts.Count}");
    }

    [Fact]
    public void LineMetrics_are_positive_for_common_cjk_fonts()
    {
        foreach (var name in new[] { "SimSun", "Microsoft YaHei" })
        {
            var m = FontMetrics.GetLineMetrics(name);
            Assert.True(m.HasValue, $"{name} 解析失败");
            Assert.True(m.Value.Ascent > 0 && m.Value.Descent > 0 && m.Value.Height >= m.Value.Ascent + m.Value.Descent * 0.99,
                $"{name} 行高异常: ascent={m.Value.Ascent} descent={m.Value.Descent} height={m.Value.Height}");
        }
    }
}
