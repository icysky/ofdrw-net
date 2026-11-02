using System.Globalization;
using Ofd.Core.BasicType;
using Xunit;

namespace Ofd.Tests.Golden;

/// <summary>
/// STBase.Fmt 与 Java STBase.fmt 的 golden 全量对照。
/// 基准由 tools/probe 的 Java 探针在 JDK 21 / zh_CN 下生成（3049 个采样）。
/// </summary>
public class FmtGoldenTests
{
    private static string GoldenPath => Path.Combine(AppContext.BaseDirectory, "TestData", "fmt-java.txt");

    [Fact]
    public void Fmt_matches_java_output_for_all_golden_values()
    {
        Assert.True(File.Exists(GoldenPath), "缺少 golden 文件 fmt-java.txt");

        int total = 0;
        var diffs = new List<string>();
        foreach (var line in File.ReadLines(GoldenPath))
        {
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var p = line.Split('\t');
            if (p.Length < 4) continue;

            long bits = long.Parse(p[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            double d = BitConverter.Int64BitsToDouble(bits);
            string net = STBase.Fmt(d);
            total++;
            if (net != p[3])
                diffs.Add($"bits={p[1]} javaFmt={p[3]} net={net}");
        }

        Assert.True(total >= 3000, $"golden 采样数异常: {total}（应约 3049，文件可能损坏）");
        Assert.True(diffs.Count == 0,
            $"FMT 不一致 {diffs.Count}/{total}，前 20 条:\n{string.Join("\n", diffs.Take(20))}");
    }
}
