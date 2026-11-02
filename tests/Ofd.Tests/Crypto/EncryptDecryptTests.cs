using System.IO.Compression;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Crypto;
using Ofd.Crypto.Decryptor;
using Ofd.Crypto.Encryptor;
using Ofd.Pkg.Container;
using Org.BouncyCastle.Crypto;
using Xunit;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.Crypto;

/// <summary>OFD 加密/解密（口令方案）：加密 → 解密 → 与原文逐文件字节一致。</summary>
public class EncryptDecryptTests : IDisposable
{
    private const string Password = "123456";
    private const string UserName = "张三";

    private readonly string _root;

    public EncryptDecryptTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_crypto_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string BuildSampleOfd()
    {
        var ofdDir = new OFDDir(Path.Combine(_root, "plain"));
        var doc = ofdDir.NewDoc();

        doc.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(new Pages().AddPage(
                new PageNode().SetID(new ST_ID(1)).SetBaseLoc(new ST_Loc("Pages/Page_0/Content.xml")))));

        doc.ObtainPages().NewPageDir().SetContent(new PageObjType());
        ofdDir.SetOfd(new OFD().AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml"))));
        ofdDir.Flush();

        string file = Path.Combine(_root, "plain.ofd");
        ofdDir.Jar(file);
        return file;
    }

    private static Dictionary<string, byte[]> ReadEntries(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var res = new Dictionary<string, byte[]>();
        foreach (var e in zip.Entries)
        {
            if (string.IsNullOrEmpty(e.Name)) continue; // 目录条目
            using var s = e.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            res[e.FullName] = ms.ToArray();
        }
        return res;
    }

    [Fact]
    public void Encrypt_then_decrypt_restores_every_file_byte_identical()
    {
        string plain = BuildSampleOfd();
        var original = ReadEntries(plain);
        Assert.NotEmpty(original);

        // 加密
        string enc = Path.Combine(_root, "enc.ofd");
        using (var encryptor = new OFDEncryptor(plain, enc))
        {
            encryptor.AddUser(new UserPasswordEncryptor(UserName, Password));
            encryptor.Encrypt();
        }

        var encEntries = ReadEntries(enc);
        Assert.Contains("Encryptions.xml", encEntries.Keys);
        Assert.Contains("decryptseed.dat", encEntries.Keys);
        Assert.Contains("entriesmap.dat", encEntries.Keys);
        // 原文文件已被密文替换
        Assert.DoesNotContain("OFD.xml", encEntries.Keys);
        Assert.DoesNotContain("Doc_0/Document.xml", encEntries.Keys);
        Assert.Contains("ofd.dat", encEntries.Keys); // 原 OFD.xml → 小写 + .dat

        // 解密
        string dec = Path.Combine(_root, "dec.ofd");
        using (var decryptor = new OFDDecryptor(enc, dec))
        {
            decryptor.AddUser(new UserPasswordDecryptor(UserName, Password));
            decryptor.Decrypt();
        }

        var restored = ReadEntries(dec);
        Assert.DoesNotContain("Encryptions.xml", restored.Keys);
        Assert.DoesNotContain("decryptseed.dat", restored.Keys);
        Assert.DoesNotContain("entriesmap.dat", restored.Keys);

        foreach (var kv in original)
        {
            Assert.True(restored.ContainsKey(kv.Key), "解密后缺少文件: " + kv.Key);
            Assert.Equal(kv.Value, restored[kv.Key]);
        }
    }

    [Fact]
    public void Wrong_password_throws_crypto_exception()
    {
        string plain = BuildSampleOfd();
        string enc = Path.Combine(_root, "enc2.ofd");
        using (var encryptor = new OFDEncryptor(plain, enc))
        {
            encryptor.AddUser(new UserPasswordEncryptor(UserName, Password));
            encryptor.Encrypt();
        }

        string dec = Path.Combine(_root, "dec2.ofd");
        using var decryptor = new OFDDecryptor(enc, dec);
        decryptor.AddUser(new UserPasswordDecryptor(UserName, "wrong-password"));
        Assert.Throws<CryptoException>(() => decryptor.Decrypt());
    }

    [Fact]
    public void Unencrypted_ofd_is_repacked_unchanged()
    {
        string plain = BuildSampleOfd();
        var original = ReadEntries(plain);

        string dec = Path.Combine(_root, "pass-through.ofd");
        using (var decryptor = new OFDDecryptor(plain, dec))
        {
            decryptor.Decrypt();
        }

        var restored = ReadEntries(dec);
        foreach (var kv in original)
            Assert.Equal(kv.Value, restored[kv.Key]);
    }

    private static (AsymmetricCipherKeyPair KeyPair, Org.BouncyCastle.X509.X509Certificate Cert) NewSm2Cert()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        gen.Init(new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(domain,
            new Org.BouncyCastle.Security.SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new Org.BouncyCastle.X509.X509V3CertificateGenerator();
        certGen.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(11));
        certGen.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Crypto-Test"));
        certGen.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Crypto-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory("SM3withSM2", kp.Private));
        return (kp, cert);
    }

    [Fact]
    public void Cert_based_encrypt_then_decrypt_restores_files()
    {
        string plain = BuildSampleOfd();
        var original = ReadEntries(plain);
        var (kp, cert) = NewSm2Cert();

        string enc = Path.Combine(_root, "enc-cert.ofd");
        using (var encryptor = new OFDEncryptor(plain, enc))
        {
            encryptor.AddUser(new UserCertEncryptor("李四", cert));
            encryptor.Encrypt();
        }

        // 密钥描述文件中应含用户证书（UserCert）
        var encEntries = ReadEntries(enc);
        string seed = System.Text.Encoding.UTF8.GetString(encEntries["decryptseed.dat"]);
        Assert.Contains("UserCert", seed);
        Assert.Contains("1.1.2", seed); // 证书加密方案标识

        string dec = Path.Combine(_root, "dec-cert.ofd");
        using (var decryptor = new OFDDecryptor(enc, dec))
        {
            decryptor.AddUser(new UserCertDecryptor((Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)kp.Private));
            decryptor.Decrypt();
        }

        var restored = ReadEntries(dec);
        foreach (var kv in original)
        {
            Assert.True(restored.ContainsKey(kv.Key), "解密后缺少文件: " + kv.Key);
            Assert.Equal(kv.Value, restored[kv.Key]);
        }
    }

    [Fact]
    public void ContainerPath_naming_rules()
    {
        string dir = Path.Combine(_root, "cp");
        var cp = ContainerPath.NewDatFile("/Doc_0/Pages/Page_0/Content.xml", dir);

        Assert.Equal("/Doc_0/Pages/Page_0/content.dat", cp.GetPath());
        Assert.True(File.Exists(cp.GetAbs()));

        // 重名追加序号
        var cp2 = ContainerPath.NewDatFile("/Doc_0/Pages/Page_0/Content.xml", dir);
        Assert.Equal("/Doc_0/Pages/Page_0/content_1.dat", cp2.GetPath());
        Assert.True(File.Exists(cp2.GetAbs()));
    }
}
