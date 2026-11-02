using System.Text;
using Ofd.Core.Crypto;
using Ofd.Core.Crypto.Encryt;
using Ofd.Gm.Support;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ofd.Crypto.Encryptor;

/// <summary>
/// OFD 用户口令加密器（Java UserPasswordEncryptor）：
/// 口令经 KDF（GB/T 32918.3 5.4.3，SM3）派生 16 字节 KEK，再用 SM4-CBC 加密 FEK。
/// </summary>
public class UserPasswordEncryptor : IUserFekEncryptor
{
    private readonly string _username;
    private readonly string? _userType;

    /// <summary>KDF 派生的加密密钥（用于加密 FEK）。</summary>
    private readonly byte[] _fKek;

    public UserPasswordEncryptor(string username, string? userType, string password)
    {
        if (string.IsNullOrEmpty(username)) throw new ArgumentException("用户名称(username)为空");
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("加密口令(password)为空");

        _username = username;
        _userType = userType;
        _fKek = ExtendKey(password);
    }

    /// <summary>用户类型默认 User。</summary>
    public UserPasswordEncryptor(string username, string password) : this(username, null, password) { }

    /// <summary>口令派生 KEK（SM3 + KDF，16 字节）。</summary>
    private static byte[] ExtendKey(string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("加密口令不能为空");
        return KDF.Extend(new SM3Digest(), Encoding.UTF8.GetBytes(password), 16);
    }

    /// <summary>加密 FEK 并封装为用户信息。</summary>
    public UserInfo Encrypt(byte[] fek, byte[] iv)
    {
        var blockCipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new SM4Engine()), new Pkcs7Padding());
        blockCipher.Init(true, new ParametersWithIV(new KeyParameter(_fKek), iv));

        var buffOut = new byte[64];
        int n = blockCipher.ProcessBytes(fek, 0, _fKek.Length, buffOut, 0);
        n += blockCipher.DoFinal(buffOut, n);

        var wk = new byte[n];
        Array.Copy(buffOut, 0, wk, 0, n);

        var userInfo = new UserInfo().SetUserName(_username);
        if (_userType != null) userInfo.SetUserType(_userType);
        userInfo.SetIVValue(iv);
        userInfo.SetEncryptedWK(wk);
        return userInfo;
    }

    /// <summary>口令加密不使用证书。</summary>
    public byte[]? UserCert() => null;

    /// <summary>加密保护方案标识：口令加密 1.1.1。</summary>
    public string EncryptCaseId() => ProtectionCaseID.EncryptGMPassword.ToIdValue();
}
