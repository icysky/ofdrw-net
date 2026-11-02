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
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Xunit;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;
using V1 = Ofd.Gm.Ses.V1;
using V4 = Ofd.Gm.Ses.V4;

namespace Ofd.Tests.Sign;

/// <summary>签章（SES v4）：生成电子印章 → 签章写入 → Reader 读回印章图片。</summary>
public class SignTests : IDisposable
{
    private readonly string _root;

    public SignTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_sign_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string BuildSampleOfd(int pages = 2)
    {
        var ofdDir = new OFDDir(Path.Combine(_root, "plain"));
        var doc = ofdDir.NewDoc();

        var pageTree = new Pages();
        for (int i = 0; i < pages; i++)
            pageTree.AddPage(new PageNode().SetID(new ST_ID(i + 1))
                .SetBaseLoc(new ST_Loc($"Pages/Page_{i}/Content.xml")));

        doc.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(pageTree));

        for (int i = 0; i < pages; i++)
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

    private static (ECPrivateKeyParameters Priv, BcX509Certificate Cert) NewSm2Cert()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new Org.BouncyCastle.Security.SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(31));
        certGen.SetIssuerDN(new X509Name("CN=Sign-Test"));
        certGen.SetSubjectDN(new X509Name("CN=Sign-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory("SM3withSM2", kp.Private));
        return ((ECPrivateKeyParameters)kp.Private, cert);
    }

    private static readonly byte[] PngBytes =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
    };

    /// <summary>构造 SES v4 电子印章（含印章图片与制章人签名）。</summary>
    internal static V4.SESeal BuildSealV4(BcX509Certificate cert, ECPrivateKeyParameters priv)
    {
        var property = new V4.SES_ESPropertyInfo()
            .SetType(new DerInteger(1))
            .SetName(new DerUtf8String("测试印章"))
            .SetCertListType(V4.SES_ESPropertyInfo.CertListType)
            .SetCertList(new V4.SES_CertList(
                new V4.CertInfoList().Add(new DerOctetString(cert.GetEncoded()))))
            .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

        var sealInfo = new V4.SES_SealInfo()
            .SetHeader(new V1.SES_Header().SetVersion(4).SetVid("TEST-VID"))
            .SetEsID("ES-TEST-001")
            .SetProperty(property)
            .SetPicture(new V1.SES_ESPictrueInfo()
                .SetType("PNG")
                .SetData(PngBytes)
                .SetWidth(40)
                .SetHeight(40));

        // 制章人对印章信息签名
        byte[] tbs = sealInfo.GetEncoded();
        var signer = new SM2Signer();
        signer.Init(true, priv);
        signer.BlockUpdate(tbs, 0, tbs.Length);
        byte[] sealSig = signer.GenerateSignature();

        return new V4.SESeal(sealInfo, new DerOctetString(cert.GetEncoded()),
            Org.BouncyCastle.Asn1.GM.GMObjectIdentifiers.sm2sign_with_sm3, new DerBitString(sealSig));
    }

    [Fact]
    public void Csharp_seal_self_signature_is_valid_after_roundtrip()
    {
        var (priv, cert) = NewSm2Cert();
        var seal = BuildSealV4(cert, priv);

        // 序列化 → 重新解析（模拟写入 Seal.esl 后由阅读器读取）
        var parsed = V4.SESeal.GetInstance(seal.GetEncoded());
        Assert.NotNull(parsed);

        var sealInfo = parsed!.GeteSealInfo();
        Assert.NotNull(sealInfo);

        byte[] tbs = sealInfo!.GetEncoded();
        var verifier = new SM2Signer();
        verifier.Init(false, cert.GetPublicKey());
        verifier.BlockUpdate(tbs, 0, tbs.Length);

        byte[] sig = parsed.GetSignedValue()!.GetBytes();
        if (sig.Length == 65) sig = sig[1..];

        // 制章人对印章信息的签名必须自洽，否则阅读器会报「签章无效」
        Assert.True(verifier.VerifySignature(sig), "印章自签名校验失败");
    }

    [Fact]
    public void Sign_with_ses_v4_produces_signature_and_seal_files()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        var seal = BuildSealV4(cert, priv);

        string signedOfd = Path.Combine(_root, "signed.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, signedOfd))
        {
            signer.SetSignContainer(new SESV4Container(priv, seal, cert))
                .AddApPos(new NormalStampPos(1, 10, 10, 40, 40))
                .ExeSign();
        }

        var entries = ReadEntries(signedOfd);
        Assert.Contains("Doc_0/Signs/Signatures.xml", entries.Keys);
        Assert.Contains("Doc_0/Signs/Sign_0/Signature.xml", entries.Keys);
        Assert.Contains("Doc_0/Signs/Sign_0/SignedValue.dat", entries.Keys);
        Assert.Contains("Doc_0/Signs/Sign_0/Seal.esl", entries.Keys);

        // Signatures.xml：最大 ID（签名 s001 + 签章外观 s002）与签名记录
        string signaturesXml = System.Text.Encoding.UTF8.GetString(entries["Doc_0/Signs/Signatures.xml"]);
        Assert.Contains("<ofd:MaxSignId>s002</ofd:MaxSignId>", signaturesXml);
        Assert.Contains("Type=\"Seal\"", signaturesXml);
        Assert.Contains("ID=\"s001\"", signaturesXml);

        // Signature.xml：提供者/算法/保护范围/签章外观
        string signatureXml = System.Text.Encoding.UTF8.GetString(entries["Doc_0/Signs/Sign_0/Signature.xml"]);
        Assert.Contains("ofdrw-sign", signatureXml);
        Assert.Contains("1.2.156.10197.1.501", signatureXml);
        Assert.Contains("CheckMethod=\"SM3\"", signatureXml);
        Assert.Contains("StampAnnot", signatureXml);
        Assert.Contains("Seal.esl", signatureXml);
        Assert.Contains("SignedValue.dat", signatureXml);

        // SignedValue.dat 是 SES v4（4~5 字段 ASN.1）
        var seq = Asn1Sequence.GetInstance(Asn1Object.FromByteArray(entries["Doc_0/Signs/Sign_0/SignedValue.dat"]));
        Assert.InRange(seq.Count, 4, 5);
    }

    [Fact]
    public void Signed_ofd_can_be_read_back_with_seal_image()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        var seal = BuildSealV4(cert, priv);

        string signedOfd = Path.Combine(_root, "signed2.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, signedOfd))
        {
            signer.SetSignContainer(new SESV4Container(priv, seal, cert))
                .AddApPos(new NormalStampPos(2, 20, 30, 40, 40))
                .ExeSign();
        }

        // 重新打开签章后的文档，读取印章图片与签章外观
        using var reopened = new OFDReader(signedOfd);
        Assert.True(reopened.HasSignature());

        var stamps = reopened.GetStampAnnots();
        var stamp = Assert.Single(stamps);
        Assert.Equal(PngBytes, stamp.GetImageByte());
        Assert.Equal("PNG", stamp.GetImgType());

        var annots = stamp.GetStampAnnots();
        var annot = Assert.Single(annots);
        Assert.Equal(2, annot.GetPageRef().GetRefId()!.Id);
        Assert.Equal(20, annot.GetBoundary()!.TopLeftX);
        Assert.Equal(30, annot.GetBoundary()!.TopLeftY);
    }

    [Fact]
    public void Signing_twice_requires_continue_mode()
    {
        string plain = BuildSampleOfd();
        var (priv, cert) = NewSm2Cert();
        var seal = BuildSealV4(cert, priv);

        // 1) 默认 WholeProtected 模式：Signatures.xml 被保护，不允许再次追加签名
        string wholeProtected = Path.Combine(_root, "whole.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, wholeProtected))
        {
            signer.SetSignContainer(new SESV4Container(priv, seal, cert)).ExeSign();
        }

        Assert.Throws<SignatureTerminateException>(() =>
        {
            using var reader2 = new OFDReader(wholeProtected);
            using var signer2 = new OFDSigner(reader2, Path.Combine(_root, "never.ofd"));
        });

        // 2) 首次即采用 ContinueSign：Signatures.xml 不进入保护范围，可继续追加
        string first = Path.Combine(_root, "continue1.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, first))
        {
            signer.SetSignMode(SignMode.ContinueSign)
                .SetSignContainer(new SESV4Container(priv, seal, cert))
                .ExeSign();
        }

        string second = Path.Combine(_root, "continue2.ofd");
        using (var reader = new OFDReader(first))
        using (var signer = new OFDSigner(reader, second))
        {
            signer.SetSignContainer(new SESV4Container(priv, seal, cert)).ExeSign();
        }

        var entries = ReadEntries(second);
        string signaturesXml = System.Text.Encoding.UTF8.GetString(entries["Doc_0/Signs/Signatures.xml"]);
        Assert.Contains("<ofd:MaxSignId>s002</ofd:MaxSignId>", signaturesXml);
        Assert.Contains("Doc_0/Signs/Sign_1/Signature.xml", entries.Keys);
        Assert.Contains("Doc_0/Signs/Sign_0/Signature.xml", entries.Keys);
    }

    [Fact]
    public void Signer_requires_container_and_sign_before_close()
    {
        string plain = BuildSampleOfd();

        var reader = new OFDReader(plain);
        var signer = new OFDSigner(reader, Path.Combine(_root, "x.ofd"));
        // 未设置容器
        Assert.Throws<ArgumentException>(() => signer.ExeSign());

        // 未执行签名就关闭
        Assert.Throws<InvalidOperationException>(() => signer.Dispose());
        reader.Close();
    }
}
