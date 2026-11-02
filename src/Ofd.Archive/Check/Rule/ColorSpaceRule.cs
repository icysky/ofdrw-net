using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 9：颜色空间类型必须为 GRAY/RGB/CMYK（GB/T 42133-2022 6.2.6）。
/// </summary>
public class ColorSpaceRule : IArchiveRule
{
    public const string RULE_NAME = "COLOR_SPACE";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        var resMgt = reader.GetResMgt();
        if (resMgt == null) return violations;

        try
        {
            foreach (var cs in resMgt.GetColorSpaces())
            {
                // 非法类型在 GetType() 解析时抛异常，由外层 catch 统一报告
                var type = cs.GetType();
                if (type == null)
                {
                    string id = cs.GetID()?.ToString() ?? "?";
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Warn,
                        $"颜色空间类型未知 (ID={id})",
                        ST_Loc.GetInstance("PublicRes.xml"), "null", "GRAY/RGB/CMYK"));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(
                RULE_NAME, ArchiveViolation.Severity.Error,
                "颜色空间类型不支持: " + e.Message,
                ST_Loc.GetInstance("PublicRes.xml"), null, "GRAY/RGB/CMYK"));
        }

        return violations;
    }
}
