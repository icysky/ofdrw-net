using System.IO.Compression;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Ofd.Reader;
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.StampPos;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Ofd.Sign.Verify.Exceptions;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.X509;
using Xunit;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;
using V1 = Ofd.Gm.Ses.V1;
using V4 = Ofd.Gm.Ses.V4;

namespace Ofd.Tests.Sign;

/// <summary>验签：通过场景 + 篡改检测（文件内容 / 签名描述 / 印章文件 / 未签名）。</summary>
public class ValidateTests : IDisposable
{
    private readonly string _root;

    public ValidateTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_verify_" + Guid.NewGuid().ToString("N")[..8]);
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

        var pageTree = new Pages()
            .AddPage(new PageNode().SetID(new ST_ID(1)).SetBaseLoc(new ST_Loc("Pages/Page_0/Content.xml")));

        doc.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(pageTree));

        doc.ObtainPages().NewPageDir().SetContent(new PageObjType());
        ofdDir.SetOfd(new OFD().AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml"))));
        ofdDir.Flush();

        string file = Path.Combine(_root, "plain.ofd");
        ofdDir.Jar(file);
        return file;
    }

    private static (ECPrivateKeyParameters Priv, BcX509Certificate Cert) NewSm2Cert()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new Org.BouncyCastle.Security.SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(41));
        certGen.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Verify-Test"));
        certGen.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Verify-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory("SM3withSM2", kp.Private));
        return ((ECPrivateKeyParameters)kp.Private, cert);
    }

    private static V4.SESeal BuildSealV4(BcX509Certificate cert, ECPrivateKeyParameters priv)
    {
        var property = new V4.SES_ESPropertyInfo()
            .SetType(new DerInteger(1))
            .SetName(new DerUtf8String("验证测试印章"))
            .SetCertListType(V4.SES_ESPropertyInfo.CertListType)
            .SetCertList(new V4.SES_CertList(
                new V4.CertInfoList().Add(new DerOctetString(cert.GetEncoded()))))
            .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

        var sealInfo = new V4.SES_SealInfo()
            .SetHeader(new V1.SES_Header().SetVersion(4).SetVid("TEST-VID"))
            .SetEsID("ES-TEST-VERIFY")
            .SetProperty(property)
            .SetPicture(new V1.SES_ESPictrueInfo()
                .SetType("PNG").SetData(new byte[] { 1, 2, 3, 4 }).SetWidth(40).SetHeight(40));

        byte[] tbs = sealInfo.GetEncoded();
        var signer = new SM2Signer();
        signer.Init(true, priv);
        signer.BlockUpdate(tbs, 0, tbs.Length);

        return new V4.SESeal(sealInfo, new DerOctetString(cert.GetEncoded()),
            Org.BouncyCastle.Asn1.GM.GMObjectIdentifiers.sm2sign_with_sm3,
            new DerBitString(signer.GenerateSignature()));
    }

    /// <summary>签章一份文档并返回输出路径。</summary>
    private string Sign(string plain, ECPrivateKeyParameters priv, BcX509Certificate cert, V4.SESeal seal, string name)
    {
        string output = Path.Combine(_root, name);
        using var reader = new OFDReader(plain);
        using var signer = new OFDSigner(reader, output);
        signer.SetSignContainer(new SESV4Container(priv, seal, cert))
            .AddApPos(new NormalStampPos(1, 10, 10, 40, 40))
            .ExeSign();
        return output;
    }

    /// <summary>解压 → 修改 → 重新打包。</summary>
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
    public void Valid_signed_ofd_passes_validation()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        string signed = Sign(plain, priv, cert, BuildSealV4(cert, priv), "valid.ofd");

        using var validator = new OFDValidator(new OFDReader(signed));
        validator.SetValidator(new SESV4ValidateContainer());
        validator.ExeValidate(); // 不抛异常即通过
    }

    [Fact]
    public void Tampered_protected_file_fails_integrity()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        string signed = Sign(plain, priv, cert, BuildSealV4(cert, priv), "valid2.ofd");

        string tampered = Repack(signed, "tampered-content", dir =>
        {
            string pageContent = Path.Combine(dir, "Doc_0", "Pages", "Page_0", "Content.xml");
            File.WriteAllText(pageContent, File.ReadAllText(pageContent).Replace("Page", "Page "));
        });

        using var validator = new OFDValidator(new OFDReader(tampered));
        validator.SetValidator(new SESV4ValidateContainer());
        var ex = Assert.Throws<FileIntegrityException>(() => validator.ExeValidate());
        Assert.Contains("Content.xml", ex.GetFileAbsPath()!.Loc);
        Assert.NotNull(ex.GetExceptDataHash());
        Assert.NotNull(ex.GetActualDataHash());
    }

    [Fact]
    public void Tampered_signature_xml_fails_signed_value_check()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        string signed = Sign(plain, priv, cert, BuildSealV4(cert, priv), "valid3.ofd");

        // 修改 Signature.xml 中的时间（其摘要记录在签章数据的 DataHash 中）
        string tampered = Repack(signed, "tampered-signature", dir =>
        {
            string p = Path.Combine(dir, "Doc_0", "Signs", "Sign_0", "Signature.xml");
            File.WriteAllText(p, File.ReadAllText(p).Replace("<ofd:SignatureDateTime>", "<ofd:SignatureDateTime>1999-01-01 "));
        });

        using var validator = new OFDValidator(new OFDReader(tampered));
        validator.SetValidator(new SESV4ValidateContainer());
        var ex = Assert.Throws<InvalidSignedValueException>(() => validator.ExeValidate());
        Assert.Contains("被篡改", ex.Message);
    }

    [Fact]
    public void Seal_file_mismatch_is_detected()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        string signed = Sign(plain, priv, cert, BuildSealV4(cert, priv), "valid4.ofd");

        string tampered = Repack(signed, "seal-mismatch", dir =>
        {
            string p = Path.Combine(dir, "Doc_0", "Signs", "Sign_0", "Seal.esl");
            byte[] data = File.ReadAllBytes(p);
            data[^1] ^= 0xFF;
            File.WriteAllBytes(p, data);
        });

        using var validator = new OFDValidator(new OFDReader(tampered));
        validator.SetValidator(new SESV4ValidateContainer());
        // 印章文件改动后，其摘要先被 Integrity 检查捕获（Seal.esl 在保护范围内）
        Assert.ThrowsAny<OFDVerifyException>(() => validator.ExeValidate());
    }

    [Fact]
    public void Unsigned_ofd_throws_doc_not_sign()
    {
        string plain = BuildSampleOfd();

        using var validator = new OFDValidator(new OFDReader(plain));
        validator.SetValidator(new SESV4ValidateContainer());
        Assert.Throws<DocNotSignException>(() => validator.ExeValidate());
    }

    [Fact]
    public void Validator_requires_container()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        string signed = Sign(plain, priv, cert, BuildSealV4(cert, priv), "valid5.ofd");

        using var validator = new OFDValidator(new OFDReader(signed));
        Assert.Throws<ArgumentException>(() => validator.ExeValidate());
    }
}
