namespace Ofd.Crypto.Integrity;

/// <summary>
/// 完整性保护签名实现（Java ProtectSigner）：GM/T 0099 7.4.3 c) d) 步骤。
/// </summary>
public interface IProtectSigner
{
    /// <summary>计算待签名文件杂凑值并签名，返回符合 GB/T 35275 的签名值。</summary>
    byte[] DigestThenSign(string tbsPath);
}
