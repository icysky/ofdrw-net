using System.Text;
using Ofd.Gm.Cert;
using Ofd.Gm.Support;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Tests.Gm;

/// <summary>证书工具（PEM/PKCS#12）与 KDF 密钥派生。</summary>
public class CertAndKdfTests : IDisposable
{
    private readonly string _root;

    public CertAndKdfTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_gm_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }
        catch
        {
            // ignore
        }
    }

    private static (AsymmetricCipherKeyPair KeyPair, BcX509Certificate Cert) NewSm2KeyAndCert()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(BigInteger.ValueOf(7));
        certGen.SetIssuerDN(new X509Name("CN=GM-Test"));
        certGen.SetSubjectDN(new X509Name("CN=GM-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(new Asn1SignatureFactory("SM3withSM2", kp.Private));
        return (kp, cert);
    }

    [Fact]
    public void KDF_derives_expected_length_and_matches_single_block_hash()
    {
        byte[] z = Encoding.UTF8.GetBytes("shared-secret-z");

        // klen <= v：K 应等于 Hv(Z || 00000001)
        var md = new SM3Digest();
        byte[] k32 = KDF.Extend(md, z, 32);

        var refMd = new SM3Digest();
        refMd.BlockUpdate(z, 0, z.Length);
        refMd.BlockUpdate(new byte[] { 0, 0, 0, 1 }, 0, 4);
        var expected = new byte[32];
        refMd.DoFinal(expected, 0);
        Assert.Equal(expected, k32);

        // klen 非整块：长度正确，且前缀与单块一致
        byte[] k40 = KDF.Extend(md, z, 40);
        Assert.Equal(40, k40.Length);
        Assert.Equal(k32, k40[..32]);

        // klen 为 1 字节
        Assert.Single(KDF.Extend(md, z, 1));

        // 确定性
        Assert.Equal(k40, KDF.Extend(md, z, 40));
    }

    [Fact]
    public void PEMLoader_loads_private_key_and_certificate()
    {
        var (kp, cert) = NewSm2KeyAndCert();

        string keyPath = Path.Combine(_root, "key.pem");
        using (var writer = new StreamWriter(keyPath))
        {
            var pem = new PemWriter(writer);
            pem.WriteObject(kp.Private);
        }

        string certPath = Path.Combine(_root, "cert.pem");
        using (var writer = new StreamWriter(certPath))
        {
            var pem = new PemWriter(writer);
            pem.WriteObject(cert);
        }

        var priv = PEMLoader.LoadPrivateKey(keyPath);
        var pub = PEMLoader.LoadCert(certPath).GetPublicKey();

        // 私钥/公钥可配成密钥对（D 值一致）
        var privEc = Assert.IsType<ECPrivateKeyParameters>(priv);
        var pubEc = Assert.IsType<ECPublicKeyParameters>(pub);
        // 公钥应为 G * d
        var domain = pubEc.Parameters;
        Assert.Equal(domain.G.Multiply(privEc.D).Normalize(), pubEc.Q.Normalize());

        // 签名可被证书公钥验证（证明公私钥匹配）
        var sm2 = new Org.BouncyCastle.Crypto.Signers.SM2Signer();
        sm2.Init(true, priv);
        var msg = Encoding.UTF8.GetBytes("pem-key-test");
        sm2.BlockUpdate(msg, 0, msg.Length);
        byte[] sig = sm2.GenerateSignature();

        var verifier = new Org.BouncyCastle.Crypto.Signers.SM2Signer();
        verifier.Init(false, pub);
        verifier.BlockUpdate(msg, 0, msg.Length);
        Assert.True(verifier.VerifySignature(sig));

        Assert.Throws<IOException>(() => PEMLoader.LoadCert(keyPath));
    }

    [Fact]
    public void PKCGenerate_issues_certificate_and_saves_p12()
    {
        // CA 根证书（自签）
        var caKp = PKCGenerate.GenerateKeyPair();
        var caGen = new X509V3CertificateGenerator();
        caGen.SetSerialNumber(BigInteger.ValueOf(100));
        caGen.SetIssuerDN(new X509Name("CN=Root CA"));
        caGen.SetSubjectDN(new X509Name("CN=Root CA"));
        caGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        caGen.SetNotAfter(DateTime.UtcNow.AddYears(3));
        caGen.SetPublicKey(caKp.Public);
        var caCert = caGen.Generate(new Asn1SignatureFactory("SM3withSM2", caKp.Private));

        // 用户密钥与 PKCS#10 请求
        var userKp = PKCGenerate.GenerateKeyPair();
        var p10 = PKCGenerate.CertRequest(userKp, PKCGenerate.TestND());
        p10.Verify(userKp.Public); // 验签失败会抛异常

        // 签发用户证书
        var userCert = PKCGenerate.GenCert(p10, caCert, caKp.Private);
        Assert.True(userCert.SubjectDN.Equivalent(p10.GetCertificationRequestInfo().Subject));
        Assert.True(userCert.IssuerDN.Equivalent(caCert.SubjectDN));
        userCert.Verify(caCert.GetPublicKey()); // 由 CA 签名（失败会抛异常）
        Assert.Equal(-1, userCert.GetBasicConstraints()); // 非 CA 证书

        // P12 存储与读取
        string p12 = Path.Combine(_root, "gen.p12");
        PKCGenerate.SaveToPKCS12(userKp, new[] { userCert, caCert }, "pwd123", p12);

        var key = PKCS12Tools.ReadPrvKey(p12, "pwd123");
        Assert.Equal(((ECPrivateKeyParameters)userKp.Private).D, ((ECPrivateKeyParameters)key).D);

        var chain = PKCS12Tools.ReadCertChain(File.OpenRead(p12), "pwd123");
        Assert.Equal(2, chain.Length);
        Assert.Equal(userCert.SerialNumber, chain[0].SerialNumber);
    }

    [Fact]
    public void PKCS12Tools_reads_key_and_cert_chain()
    {
        var (kp, cert) = NewSm2KeyAndCert();
        string p12Path = Path.Combine(_root, "user.p12");
        const string pwd = "123456";

        // 构造 P12
        var store = new Pkcs12StoreBuilder().Build();
        store.SetKeyEntry("private",
            new AsymmetricKeyEntry(kp.Private),
            new[] { new X509CertificateEntry(cert) });
        using (var fs = File.Create(p12Path))
        {
            store.Save(fs, pwd.ToCharArray(), new SecureRandom());
        }

        // 私钥
        var key = PKCS12Tools.ReadPrvKey(p12Path, pwd);
        Assert.IsType<ECPrivateKeyParameters>(key);

        // 别名读取
        var keyByAlias = PKCS12Tools.ReadPrvKey(p12Path, "private", pwd);
        Assert.Equal(
            ((ECPrivateKeyParameters)key).D,
            ((ECPrivateKeyParameters)keyByAlias).D);

        // 证书链与用户证书
        using (var fs = File.OpenRead(p12Path))
        {
            var chain = PKCS12Tools.ReadCertChain(fs, pwd);
            Assert.Single(chain);
            Assert.Equal(cert.SerialNumber, chain[0].SerialNumber);
        }

        var user = PKCS12Tools.ReadUserCert(p12Path, pwd);
        Assert.Equal(cert.SerialNumber, user.SerialNumber);
        Assert.Equal("CN=GM-Test", user.SubjectDN.ToString());
    }
}
