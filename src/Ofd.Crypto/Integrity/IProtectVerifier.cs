namespace Ofd.Crypto.Integrity;

/// <summary>
/// 完整性保护签名验证实现（Java ProtectVerifier）：GM/T 0099 7.4.6 b) c) 步骤。
/// </summary>
public interface IProtectVerifier
{
    /// <summary>计算防夹带文件杂凑值并验证签名值。</summary>
    bool DigestThenVerify(string ofdEntriesXmlPath, byte[] signedValue);
}
