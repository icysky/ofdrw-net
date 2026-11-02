using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 8：去除扩展信息（GB/T 42133-2022 6.2.2e）。
/// </summary>
public class ExtensionRule : IArchiveRule
{
    public const string RULE_NAME = "EXTENSION";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var document = reader.GetDoc(0);
            if (document?.GetExtensions() != null)
            {
                violations.Add(new ArchiveViolation(
                    RULE_NAME, ArchiveViolation.Severity.Warn,
                    "文档包含扩展信息，OFD-A 要求去除",
                    ST_Loc.GetInstance("Document.xml"), "存在", "无"));
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "无法检查扩展信息: " + e.Message));
        }

        return violations;
    }
}
