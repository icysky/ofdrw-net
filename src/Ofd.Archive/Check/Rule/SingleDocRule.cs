using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 2：OFD 文件只能包含一个文档（GB/T 42133-2022 6.2.1c）。
/// </summary>
public class SingleDocRule : IArchiveRule
{
    public const string RULE_NAME = "SINGLE_DOC";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var ofd = ofdDir.GetOfd();
            var docBodies = ofd.GetDocBodies();

            if (docBodies.Count > 1)
            {
                return new List<ArchiveViolation>
                {
                    new(RULE_NAME, ArchiveViolation.Severity.Error,
                        $"OFD 文件包含 {docBodies.Count} 个文档体，OFD-A 只允许单个文档",
                        ST_Loc.GetInstance("OFD.xml"),
                        docBodies.Count.ToString(), "1"),
                };
            }

            return new List<ArchiveViolation>();
        }
        catch (Exception e)
        {
            return new List<ArchiveViolation>
            {
                new(RULE_NAME, ArchiveViolation.Severity.Error, "无法读取 OFD.xml: " + e.Message),
            };
        }
    }
}
