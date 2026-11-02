using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 4：去除权限声明（GB/T 42133-2022 6.2.2）。
/// </summary>
public class PermissionRule : IArchiveRule
{
    public const string RULE_NAME = "PERMISSION";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var document = reader.GetDoc(0);
            if (document?.GetPermission() != null)
            {
                return new List<ArchiveViolation>
                {
                    new(RULE_NAME, ArchiveViolation.Severity.Warn,
                        "文档包含权限声明，OFD-A 建议去除",
                        ST_Loc.GetInstance("Document.xml"), "存在", "无"),
                };
            }
        }
        catch (Exception e)
        {
            return new List<ArchiveViolation>
            {
                new(RULE_NAME, ArchiveViolation.Severity.Error,
                    "无法读取文档: " + e.Message, ST_Loc.GetInstance("Document.xml")),
            };
        }

        return new List<ArchiveViolation>();
    }
}
