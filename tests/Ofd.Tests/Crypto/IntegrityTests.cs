using System.IO.Compression;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Crypto.Integrity;
using Ofd.Pkg.Container;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Xunit;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.Crypto;

/// <summary>GM/T 0099 完整性保护（防夹带）：签名 → 校验 → 夹带/篡改检测。</summary>
public class IntegrityTests : IDisposable
{
    private readonly string _root;

    public IntegrityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_integrity_" + Guid.NewGuid().ToString("N")[..8]);
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
            if (string.IsNullOrEmpty(e.Name)) continue;
            using var s = e.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            res[e.FullName] = ms.ToArray();
        }
        return res;
    }

    private static (AsymmetricCipherKeyPair KeyPair, Org.BouncyCastle.X509.X509Certificate Cert) NewSm2Cert()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new Org.BouncyCastle.Security.SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new Org.BouncyCastle.X509.X509V3CertificateGenerator();
        certGen.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(21));
        certGen.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Integrity-Test"));
        certGen.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Integrity-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory("SM3withSM2", kp.Private));
        return (kp, cert);
    }

    /// <summary>把 OFD 解压到目录后执行修改，再重新打包。</summary>
    private string Repack(string srcOfd, string tag, Action<string> mutate)
    {
        string dir = Path.Combine(_root, "repack-" + tag);
        ZipFile.ExtractToDirectory(srcOfd, dir);
        mutate(dir);
        string outPath = Path.Combine(_root, tag + ".ofd");
        if (File.Exists(outPath)) File.Delete(outPath);
        ZipFile.CreateFromDirectory(dir, outPath);
        return outPath;
    }

    [Fact]
    public void Protect_then_verify_passes()
    {
        string plain = BuildSampleOfd();
        var (kp, cert) = NewSm2Cert();

        string protectedOfd = Path.Combine(_root, "protected.ofd");
        using (var integrity = new OFDIntegrity(plain, protectedOfd))
        {
            integrity.Protect(new GMProtectSigner((ECPrivateKeyParameters)kp.Private, cert));
        }

        var entries = ReadEntries(protectedOfd);
        Assert.Contains("OFDEntries.xml", entries.Keys);
        Assert.Contains("signedvalue.dat", entries.Keys);

        // 清单里应当登记了每个文件
        string entriesXml = System.Text.Encoding.UTF8.GetString(entries["OFDEntries.xml"]);
        Assert.Contains("FileList", entriesXml);
        Assert.Contains("/OFD.xml", entriesXml);

        var verifier = new OFDIntegrityVerifier();
        Assert.True(verifier.Integrity(protectedOfd, new GMProtectVerifier()));
    }

    [Fact]
    public void Extra_file_is_detected()
    {
        string plain = BuildSampleOfd();
        var (kp, cert) = NewSm2Cert();

        string protectedOfd = Path.Combine(_root, "protected2.ofd");
        using (var integrity = new OFDIntegrity(plain, protectedOfd))
        {
            integrity.Protect(new GMProtectSigner((ECPrivateKeyParameters)kp.Private, cert));
        }

        string tainted = Repack(protectedOfd, "tainted", dir =>
            File.WriteAllText(Path.Combine(dir, "evil.txt"), "夹带内容"));

        var verifier = new OFDIntegrityVerifier();
        Assert.False(verifier.Integrity(tainted, new GMProtectVerifier()));
    }

    [Fact]
    public void Tampered_entries_xml_fails_signature()
    {
        string plain = BuildSampleOfd();
        var (kp, cert) = NewSm2Cert();

        string protectedOfd = Path.Combine(_root, "protected3.ofd");
        using (var integrity = new OFDIntegrity(plain, protectedOfd))
        {
            integrity.Protect(new GMProtectSigner((ECPrivateKeyParameters)kp.Private, cert));
        }

        string tampered = Repack(protectedOfd, "tampered", dir =>
        {
            string p = Path.Combine(dir, "OFDEntries.xml");
            string xml = File.ReadAllText(p);
            // 篡改清单内容（改创建者名），签名应失效
            File.WriteAllText(p, xml.Replace("ofdrw-crypto", "hacked-crypto"));
        });

        var verifier = new OFDIntegrityVerifier();
        Assert.False(verifier.Integrity(tampered, new GMProtectVerifier()));

        // 直接篡改签名值同样应失败
        string tamperedSig = Repack(protectedOfd, "tampered-sig", dir =>
        {
            string p = Path.Combine(dir, "signedvalue.dat");
            byte[] sig = File.ReadAllBytes(p);
            sig[^1] ^= 0xFF;
            File.WriteAllBytes(p, sig);
        });
        Assert.False(verifier.Integrity(tamperedSig, new GMProtectVerifier()));
    }

    [Fact]
    public void Missing_entries_file_throws()
    {
        string plain = BuildSampleOfd();
        var verifier = new OFDIntegrityVerifier();
        Assert.Throws<FileNotFoundException>(() => verifier.Integrity(plain, new GMProtectVerifier()));
    }
}
