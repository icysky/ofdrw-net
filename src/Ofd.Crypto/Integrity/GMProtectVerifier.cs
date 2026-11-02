using Ofd.Gm.Sm2Strut;

namespace Ofd.Crypto.Integrity;

/// <summary>国密 SM2/SM3 完整性保护签名验证（Java GMProtectVerifier）。</summary>
public class GMProtectVerifier : IProtectVerifier
{
    /// <summary>计算防夹带文件杂凑值并验证 GB/T 35275 签名值。</summary>
    public bool DigestThenVerify(string ofdEntriesXmlPath, byte[] signedValue)
    {
        byte[] tbs = File.ReadAllBytes(ofdEntriesXmlPath);
        var verifyInfo = GBT35275Validate.Validate("SM3withSM2", tbs, signedValue);
        return verifyInfo.Result;
    }
}
