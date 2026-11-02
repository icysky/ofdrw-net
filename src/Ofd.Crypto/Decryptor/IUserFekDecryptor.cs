using Ofd.Core.Crypto.Encryt;

namespace Ofd.Crypto.Decryptor;

/// <summary>用户文件加密密钥解密器（Java UserFEKDecryptor）：从 UserInfo 恢复 FEK 与 IV。</summary>
public interface IUserFekDecryptor
{
    /// <summary>从 UserInfo 中解密出 FEK 与 IV。</summary>
    DecryptResult Decrypt(UserInfo userInfo);

    /// <summary>加密保护方案标识（需与加密时一致）。</summary>
    string EncryptCaseId();

    /// <summary>目标用户名（null 表示匹配第一个 UserInfo）。</summary>
    string? GetUsername();
}
