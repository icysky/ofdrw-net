using Ofd.Core.Crypto.Encryt;

namespace Ofd.Crypto.Encryptor;

/// <summary>
/// 用户文件加密密钥加密器（Java UserFEKEncryptor）：
/// 加密 FEK 并封装为 <see cref="UserInfo"/>。
/// </summary>
public interface IUserFekEncryptor
{
    /// <summary>加密文件加密密钥。</summary>
    UserInfo Encrypt(byte[] fek, byte[] iv);

    /// <summary>用户加密使用的证书（DER），口令加密返回 null。</summary>
    byte[]? UserCert();

    /// <summary>加密保护方案标识（见 ProtectionCaseID）。</summary>
    string EncryptCaseId();
}
