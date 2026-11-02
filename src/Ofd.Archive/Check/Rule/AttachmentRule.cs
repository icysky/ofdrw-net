using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 附件合规（GB/T 42133-2022 6.15）：仅保留文本格式（TXT/XML）附件。
/// </summary>
public class AttachmentRule : IArchiveRule
{
    public const string RULE_NAME = "ATTACHMENT";

    /// <summary>可保留的文本类附件格式。</summary>
    private static readonly HashSet<string> KeepFormats = new(StringComparer.OrdinalIgnoreCase) { "TXT", "XML" };

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var attachments = reader.GetAttachmentList();
            if (attachments == null || attachments.Count == 0) return violations;

            foreach (var att in attachments)
            {
                string? format = att.GetFormat();
                string name = att.GetAttachmentName();

                if (format != null && KeepFormats.Contains(format)) continue;

                violations.Add(new ArchiveViolation(
                    RULE_NAME, ArchiveViolation.Severity.Error,
                    $"附件 \"{name}\" 格式为 {format ?? "未知"}，OFD-A 建议移出并以摘要信息替代",
                    att.GetFileLoc(), format, "TXT/XML"));
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查附件时异常: " + e.Message));
        }

        return violations;
    }
}
