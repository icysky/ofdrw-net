using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 24：建议为颜色空间提供 ICC 配置文件（GB/T 42133-2022 6.2.6）。
/// </summary>
public class ColorProfileRule : IArchiveRule
{
    public const string RULE_NAME = "COLOR_PROFILE";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        var resMgt = reader.GetResMgt();
        if (resMgt == null) return violations;

        try
        {
            foreach (var cs in resMgt.GetColorSpaces())
            {
                if (cs.GetProfile() != null) continue;

                string id = cs.GetID()?.ToString() ?? "?";
                violations.Add(new ArchiveViolation(
                    RULE_NAME, ArchiveViolation.Severity.Info,
                    $"建议为颜色空间添加 ICC 颜色配置文件 (ID={id})",
                    ST_Loc.GetInstance("PublicRes.xml"), "缺失", "ICC Profile"));
            }
        }
        catch (Exception)
        {
            // 与 Java 一致：忽略
        }

        return violations;
    }
}
