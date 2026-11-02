using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 1：DocType 必须为 "OFD-A"（GB/T 42133-2022 6.2.1a）。
/// </summary>
public class DocTypeRule : IArchiveRule
{
    public const string RULE_NAME = "DOC_TYPE";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var ofd = ofdDir.GetOfd();
            string? docType = ofd.GetDocType();

            if (docType != "OFD-A")
            {
                return new List<ArchiveViolation>
                {
                    new(RULE_NAME, ArchiveViolation.Severity.Error,
                        "DocType 必须为 \"OFD-A\"，当前为: " + (docType == null ? "缺失" : $"\"{docType}\""),
                        ST_Loc.GetInstance("OFD.xml"), docType, "OFD-A"),
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
