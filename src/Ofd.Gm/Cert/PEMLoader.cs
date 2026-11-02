using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Gm.Cert;

/// <summary>PEM 文件加载工具（Java PEMLoader）。</summary>
public static class PEMLoader
{
    /// <summary>从 PEM 文件加载 SM2 私钥（支持 PKCS#8 与密钥对格式）。</summary>
    public static AsymmetricKeyParameter LoadPrivateKey(string path)
    {
        using var reader = new StreamReader(path);
        var pem = new PemReader(reader);
        object? obj = pem.ReadObject();

        return obj switch
        {
            AsymmetricCipherKeyPair kp => kp.Private,
            AsymmetricKeyParameter akp => akp,
            PrivateKeyInfo pki => PrivateKeyFactory.CreateKey(pki),
            _ => throw new IOException("无法解析 PEM 私钥"),
        };
    }

    /// <summary>从 PEM 文件加载国密 SM2 证书。</summary>
    public static BcX509Certificate LoadCert(string path)
    {
        using var reader = new StreamReader(path);
        var pem = new PemReader(reader);
        object? obj = pem.ReadObject();

        if (obj is BcX509Certificate cert) return cert;
        throw new IOException("无法解析 PEM 证书");
    }
}
