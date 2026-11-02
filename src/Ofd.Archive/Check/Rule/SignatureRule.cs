using Ofd.Core.BasicType;
using Ofd.Core.Signatures;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check.Rule;

/// <summary>
/// 签名去技术化检查（GB/T 42133-2022 6.13）：存在数字签名或签章时提示需去技术化。
/// </summary>
public class SignatureRule : IArchiveRule
{
    public const string RULE_NAME = "SIGNATURE";

    public List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir)
    {
        var violations = new List<ArchiveViolation>();

        try
        {
            var signatures = reader.GetDefaultSignatures();
            if (signatures == null) return violations;

            var loc = ST_Loc.GetInstance("Signs/Signatures.xml");

            foreach (var sig in signatures.GetSignatures())
            {
                var type = sig.GetType();
                string sigId = sig.GetId();

                if (type == SigType.Sign)
                {
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        $"存在数字签名(ID={sigId})，OFD-A 需去技术化: 提取签名信息→注释参数→删除SignRef→从签名列表移除",
                        loc, "Sign", "去技术化"));
                }
                else
                {
                    violations.Add(new ArchiveViolation(
                        RULE_NAME, ArchiveViolation.Severity.Error,
                        $"存在签章(ID={sigId})，OFD-A 需去技术化: 外观→注释(BlendMode=Darken)→保留签章人/证书/时间/摘要→移除签名列表",
                        loc, "Seal", "去技术化"));
                }
            }
        }
        catch (Exception e)
        {
            violations.Add(new ArchiveViolation(RULE_NAME, ArchiveViolation.Severity.Error,
                "检查签名时异常: " + e.Message));
        }

        return violations;
    }
}
