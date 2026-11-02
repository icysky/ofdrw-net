using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 11：字体必须嵌入字型文件（GB/T 42133-2022 6.2.6b）。
/// <para>Phase 1 仅检查字体文件是否嵌入与存在，子集化验证留待后续。</para>
/// </summary>
public class FontSubsetRule : IArchiveRule
{
    public const string RULE_NAME = "FONT_SUBSET";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        var resMgt = reader.GetResMgt();
        if (resMgt == null) return violations;

        try
        {
            foreach (var font in resMgt.GetFonts())
            {
                var fontFile = font.GetFontFile();
                if (fontFile == null)
                {
                    string id = font.GetID()?.ToString() ?? "?";
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        $"字体未嵌入字型文件 (ID={id})",
                        ST_Loc.GetInstance("PublicRes.xml"), "缺失", "嵌入字型文件"));
                    continue;
                }

                string path = fontFile.ToString();
                if (path.StartsWith('/')) path = path[1..];

                string filePath = Path.Combine(reader.GetWorkDir(), path);
                if (!File.Exists(filePath))
                {
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        "字体文件不存在: " + path,
                        fontFile, "文件不存在", "有效字体文件"));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查字体嵌入时异常: " + e.Message));
        }

        return violations;
    }
}
