using Ofd.Core.Crypto;
using Ofd.Core.Crypto.Encryt;
using Ofd.Gm.Sm2Strut;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Crypto.Encryptor;

/// <summary>
/// OFD 用户证书加密器（Java UserCertEncryptor）：
/// 用证书公钥以 SM2（C1C3C2）加密 FEK，EncryptedWK 存 ASN.1 SM2Cipher。
/// </summary>
public class UserCertEncryptor : IUserFekEncryptor
{
    private readonly BcX509Certificate _certificate;
    private readonly string _username;
    private readonly string? _userType;

    public UserCertEncryptor(string username, string? userType, BcX509Certificate certificate)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentException("用户名称(username)为空");
        if (certificate == null) throw new ArgumentException("用户数字证书(certificate)为空");

        _username = username;
        _userType = userType;
        _certificate = certificate;
    }

    /// <summary>用户类型默认 User。</summary>
    public UserCertEncryptor(string username, BcX509Certificate certificate) : this(username, null, certificate) { }

    /// <summary>加密 FEK 并封装为用户信息。</summary>
    public UserInfo Encrypt(byte[] fek, byte[] iv)
    {
        var publicKeyParameters = (ECPublicKeyParameters)_certificate.GetPublicKey();

        var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
        engine.Init(true, new ParametersWithRandom(publicKeyParameters, new SecureRandom()));
        byte[] c1c3c2 = engine.ProcessBlock(fek, 0, fek.Length);

        var sm2Cipher = SM2Cipher.FromC1C3C2(c1c3c2);

        var userInfo = new UserInfo().SetUserName(_username);
        if (_userType != null) userInfo.SetUserType(_userType);
        userInfo.SetIVValue(iv);
        userInfo.SetEncryptedWK(sm2Cipher.GetEncoded());
        return userInfo;
    }

    /// <summary>证书 DER 编码（写入密钥描述文件的 UserCert）。</summary>
    public byte[]? UserCert() => _certificate.GetEncoded();

    /// <summary>加密保护方案标识：证书加密 1.1.2。</summary>
    public string EncryptCaseId() => ProtectionCaseID.EncryptGMCert.ToIdValue();
}
