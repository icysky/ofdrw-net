using Ofd.Core.Crypto;
using Ofd.Core.Crypto.Encryt;
using Ofd.Gm.Sm2Strut;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ofd.Crypto.Decryptor;

/// <summary>
/// OFD 用户证书解密器（Java UserCertDecryptor）：
/// 用 SM2 私钥解密 EncryptedWK（ASN.1 SM2Cipher → C1C3C2）恢复 FEK。
/// </summary>
public class UserCertDecryptor : IUserFekDecryptor
{
    private readonly string? _username;
    private readonly ECPrivateKeyParameters _privateKeyParams;

    public UserCertDecryptor(string? username, ECPrivateKeyParameters privateKey)
    {
        _username = username;
        _privateKeyParams = privateKey ?? throw new ArgumentException("SM2私钥(privateKey)不能为空");
    }

    /// <summary>不指定用户名，匹配第一个 UserInfo。</summary>
    public UserCertDecryptor(ECPrivateKeyParameters privateKey) : this(null, privateKey) { }

    /// <summary>从 UserInfo 解密出 FEK 与 IV。</summary>
    public DecryptResult Decrypt(UserInfo userInfo)
    {
        byte[] encryptedWk = userInfo.GetEncryptedWK();
        byte[] iv = userInfo.GetIVValue();

        try
        {
            var sm2Cipher = SM2Cipher.GetInstance(Asn1Object.FromByteArray(encryptedWk))
                            ?? throw new CryptoException("EncryptedWK 不是有效的 SM2Cipher");
            byte[] c1c3c2 = sm2Cipher.ConvertC1C3C2();

            var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
            engine.Init(false, _privateKeyParams);
            byte[] fek = engine.ProcessBlock(c1c3c2, 0, c1c3c2.Length);

            return new DecryptResult(fek, iv);
        }
        catch (CryptoException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new CryptoException("证书解密失败：密钥不匹配或数据损坏", e);
        }
    }

    public string EncryptCaseId() => ProtectionCaseID.EncryptGMCert.ToIdValue();

    public string? GetUsername() => _username;
}
