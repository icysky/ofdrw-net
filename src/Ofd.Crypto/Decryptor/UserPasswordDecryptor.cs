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

namespace Ofd.Crypto.Decryptor;

/// <summary>
/// OFD 用户口令解密器（Java UserPasswordDecryptor）：
/// 口令经 KDF 派生 KEK，SM4-CBC 解密 EncryptedWK 得到 FEK。
/// </summary>
public class UserPasswordDecryptor : IUserFekDecryptor
{
    private readonly string? _username;
    private readonly byte[] _kek;

    public UserPasswordDecryptor(string? username, string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("口令(password)不能为空");
        _username = username;
        _kek = KDF.Extend(new SM3Digest(), Encoding.UTF8.GetBytes(password), 16);
    }

    /// <summary>不指定用户名，匹配第一个 UserInfo。</summary>
    public UserPasswordDecryptor(string password) : this(null, password) { }

    /// <summary>从 UserInfo 解密出 FEK 与 IV。</summary>
    public DecryptResult Decrypt(UserInfo userInfo)
    {
        byte[] encryptedWk = userInfo.GetEncryptedWK();
        byte[] iv = userInfo.GetIVValue();

        var blockCipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new SM4Engine()), new Pkcs7Padding());
        blockCipher.Init(false, new ParametersWithIV(new KeyParameter(_kek), iv));

        var buffOut = new byte[encryptedWk.Length + blockCipher.GetBlockSize()];
        int n = blockCipher.ProcessBytes(encryptedWk, 0, encryptedWk.Length, buffOut, 0);
        try
        {
            n += blockCipher.DoFinal(buffOut, n);
        }
        catch (InvalidCipherTextException e)
        {
            throw new CryptoException("口令解密失败：口令错误或数据损坏", e);
        }

        var fek = new byte[n];
        Array.Copy(buffOut, 0, fek, 0, n);
        return new DecryptResult(fek, iv);
    }

    public string EncryptCaseId() => ProtectionCaseID.EncryptGMPassword.ToIdValue();

    public string? GetUsername() => _username;
}
