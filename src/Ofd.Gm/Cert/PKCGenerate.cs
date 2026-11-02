using Org.BouncyCastle.Asn1.Misc;
using Org.BouncyCastle.Asn1.X500;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Gm.Cert;

/// <summary>
/// 生成测试用 SM2 公私钥对与证书（Java PKCGenerate）：密钥对、PKCS#10 请求、签发证书、P12 存储。
/// </summary>
public static class PKCGenerate
{
    private const string SignatureAlgorithm = "SM3withSM2";
    private const string Sm2Curve = "sm2p256v1";

    /// <summary>测试证书的 Subject（DN）。</summary>
    public static X509Name TestND()
    {
        // 注：BC C# 2.x 无 X500NameBuilder，直接用 DN 字符串构造（RDN 顺序与 Java 版一致）
        return new X509Name("C=CN,O=OFD R&W,ST=Zhejiang,L=Hangzhou,CN=Test Certificate");
    }

    /// <summary>生成 SM2 密钥对。</summary>
    public static AsymmetricCipherKeyPair GenerateKeyPair()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName(Sm2Curve);
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        var kpg = new ECKeyPairGenerator();
        kpg.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
        return kpg.GenerateKeyPair();
    }

    /// <summary>生成 SM2 密钥对的证书请求（PKCS#10）。</summary>
    public static Pkcs10CertificationRequest CertRequest(AsymmetricCipherKeyPair kp, X509Name subject) =>
        new(new Asn1SignatureFactory(SignatureAlgorithm, kp.Private), subject, kp.Public, null);

    /// <summary>签发证书（由 CA 根证书与 CA 私钥签名）。</summary>
    public static BcX509Certificate GenCert(Pkcs10CertificationRequest p10Obj, BcX509Certificate root,
        AsymmetricKeyParameter privateKey)
    {
        var subject = p10Obj.GetCertificationRequestInfo().Subject;
        // 签发者使用根证书的 Subject
        var issuer = new X509CertificateParser().ReadCertificate(root.GetEncoded()).SubjectDN;

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(BigInteger.ValueOf(DateTime.UtcNow.Ticks));
        certGen.SetIssuerDN(issuer);
        certGen.SetNotBefore(DateTime.UtcNow);
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(2));
        certGen.SetSubjectDN(subject);
        certGen.SetPublicKey(p10Obj.GetPublicKey());

        // 密钥用法：数字签名 | 不可否认 | 签发证书
        certGen.AddExtension(X509Extensions.KeyUsage, false,
            new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation | KeyUsage.KeyCertSign));
        // 扩展密钥用法：客户端身份认证
        certGen.AddExtension(X509Extensions.ExtendedKeyUsage, false,
            new ExtendedKeyUsage(KeyPurposeID.IdKPClientAuth));
        // 基础约束：实体证书
        certGen.AddExtension(X509Extensions.BasicConstraints, false, new BasicConstraints(false));
        // Netscape 证书类型：SSL 客户端
        certGen.AddExtension(MiscObjectIdentifiers.NetscapeCertType, false,
            new NetscapeCertType(NetscapeCertType.SslClient));

        return certGen.Generate(new Asn1SignatureFactory(SignatureAlgorithm, privateKey));
    }

    /// <summary>生成 P12 存储文件（保存公私钥对与证书链）。</summary>
    public static void SaveToPKCS12(AsymmetricCipherKeyPair pk, BcX509Certificate[] certs, string pwd, string outPath)
    {
        var store = new Pkcs12StoreBuilder().Build();

        var chain = new X509CertificateEntry[certs.Length];
        for (int i = 0; i < certs.Length; i++)
            chain[i] = new X509CertificateEntry(certs[i]);

        store.SetKeyEntry("private", new AsymmetricKeyEntry(pk.Private), chain);

        using var output = File.Create(outPath);
        store.Save(output, pwd.ToCharArray(), new SecureRandom());
    }
}
