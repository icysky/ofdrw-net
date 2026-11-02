using Ofd.Core.BasicStructure.Res;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 规则 12：禁止音频视频资源（GB/T 42133-2022 6.2.6g）。
/// </summary>
public class AudioVideoRule : IArchiveRule
{
    public const string RULE_NAME = "AUDIO_VIDEO";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        var resMgt = reader.GetResMgt();
        if (resMgt == null) return violations;

        try
        {
            foreach (var media in resMgt.GetMultiMedias())
            {
                var type = media.GetType();
                if (type != MediaType.Audio && type != MediaType.Video) continue;

                string id = media.GetID()?.ToString() ?? "?";
                violations.Add(new ArchiveViolation(
                    RULE_NAME, ArchiveViolation.Severity.Error,
                    $"OFD-A 不允许{type}资源 (ID={id})",
                    media.GetMediaFile(), type.ToString(), "删除"));
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查音视频资源时异常: " + e.Message));
        }

        return violations;
    }
}
