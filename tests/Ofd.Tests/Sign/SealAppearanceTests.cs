using System.IO.Compression;
using System.Text;
using V1 = Ofd.Gm.Ses.V1;
using V4 = Ofd.Gm.Ses.V4;
using SES_ESPropertyInfo = Ofd.Gm.Ses.V4.SES_ESPropertyInfo;
using SES_CertList = Ofd.Gm.Ses.V4.SES_CertList;
using CertInfoList = Ofd.Gm.Ses.V4.CertInfoList;
using SES_SealInfo = Ofd.Gm.Ses.V4.SES_SealInfo;
using Ofd.Layout;
using Ofd.Layout.Element;
using Ofd.Reader;
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.StampPos;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.X509;
using SkiaSharp;
using Xunit;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Tests.Sign;

/// <summary>
/// 电子签章「可见性」回归：这三个条件缺一不可
/// <list type="number">
/// <item><c>AddApPos</c> 写入 <c>StampAnnot</c>（否则阅读器不知道画在哪）</item>
/// <item>印章图片声明尺寸与 <c>StampAnnot.Boundary</c> 一致</item>
/// <item>印章图片为不透明 RGB PNG（颜色类型 2），带 alpha 时部分阅读器解码失败</item>
/// </list>
/// </summary>
public class SealAppearanceTests : IDisposable
{
    private readonly string _root;

    public SealAppearanceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_seal_" + Guid.NewGuid().ToString("N")[..8]);
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

    /// <summary>生成不透明 RGB PNG（颜色类型 2）。</summary>
    private static byte[] OpaquePng(int size = 64)
    {
        var info = new SKImageInfo(size, size, SKColorType.Rgb888x, SKAlphaType.Opaque);
        using var bitmap = new SKBitmap(info);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { Color = new SKColor(200, 0, 0) };
            canvas.DrawCircle(size / 2f, size / 2f, size / 3f, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static (ECPrivateKeyParameters, BcX509Certificate) NewSm2Cert()
    {
        var curve = ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new Org.BouncyCastle.Security.SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(201));
        certGen.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Seal-Test"));
        certGen.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Seal-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(
            new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory("SM3withSM2", kp.Private));

        return ((ECPrivateKeyParameters)kp.Private, cert);
    }

    private static V4.SESeal BuildSeal(BcX509Certificate cert, ECPrivateKeyParameters priv, byte[] png, int mm)
    {
        var property = new SES_ESPropertyInfo()
            .SetType(new DerInteger(1))
            .SetName(new DerUtf8String("可见性测试章"))
            .SetCertListType(SES_ESPropertyInfo.CertListType)
            .SetCertList(new SES_CertList(new CertInfoList().Add(new DerOctetString(cert.GetEncoded()))))
            .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

        var sealInfo = new SES_SealInfo()
            .SetHeader(new V1.SES_Header().SetVersion(4).SetVid("SEAL-TEST"))
            .SetEsID("ES-SEAL-TEST")
            .SetProperty(property)
            .SetPicture(new V1.SES_ESPictrueInfo()
                .SetType("PNG").SetData(png).SetWidth(new DerInteger(mm)).SetHeight(new DerInteger(mm)));

        byte[] tbs = sealInfo.GetEncoded();
        var signer = new SM2Signer();
        signer.Init(true, priv);
        signer.BlockUpdate(tbs, 0, tbs.Length);

        return new V4.SESeal(sealInfo, new DerOctetString(cert.GetEncoded()),
            GMObjectIdentifiers.sm2sign_with_sm3, new DerBitString(signer.GenerateSignature()));
    }

    private static Dictionary<string, byte[]> ReadEntries(string ofd)
    {
        using var zip = ZipFile.OpenRead(ofd);
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var e in zip.Entries)
        {
            if (string.IsNullOrEmpty(e.Name)) continue;
            using var s = e.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            result[e.FullName] = ms.ToArray();
        }

        return result;
    }

    [Fact]
    public void Seal_writes_stamp_annot_with_matching_opaque_picture()
    {
        const int mm = 40;

        string plain = Path.Combine(_root, "p.ofd");
        using (var doc = new OFDDoc(plain)) doc.Add(new Paragraph("签章可见性", 5));

        byte[] png = OpaquePng();
        var (priv, cert) = NewSm2Cert();
        var seal = BuildSeal(cert, priv, png, mm);

        string signed = Path.Combine(_root, "s.ofd");
        using (var reader = new OFDReader(plain))
        using (var signer = new OFDSigner(reader, signed))
        {
            signer.SetSignContainer(new SESV4Container(priv, seal, cert))
                .AddApPos(new NormalStampPos(1, 100, 120, mm, mm))
                .ExeSign();
        }

        var entries = ReadEntries(signed);

        // 1) 必须有 StampAnnot，且 Boundary 与图片尺寸一致
        string signatureXml = Encoding.UTF8.GetString(entries["Doc_0/Signs/Sign_0/Signature.xml"]);
        Assert.Contains("<ofd:Seal>", signatureXml);
        Assert.Contains("Seal.esl", signatureXml);
        Assert.Contains($"Boundary=\"100 120 {mm} {mm}\"", signatureXml);
        Assert.Contains("PageRef=\"1\"", signatureXml);

        // 2) Seal.esl 内图片：PNG、尺寸 = mm、且为不透明（颜色类型 2）
        var sealFromFile = V4.SESeal.GetInstance(entries["Doc_0/Signs/Sign_0/Seal.esl"]);
        Assert.NotNull(sealFromFile);

        var picture = sealFromFile!.GeteSealInfo()!.GetPicture();
        Assert.NotNull(picture);
        Assert.Equal("PNG", picture!.GetType()?.GetString());
        Assert.Equal(mm, picture.GetWidth()!.Value.IntValue);
        Assert.Equal(mm, picture.GetHeight()!.Value.IntValue);

        byte[] pictureBytes = picture.GetData()!.GetOctets();
        Assert.Equal(0x89, pictureBytes[0]);           // PNG 魔数
        Assert.Equal(2, pictureBytes[25]);             // 颜色类型 2 = RGB（无 alpha）

        // 3) 签章验签通过
        using (var reader = new OFDReader(signed))
        using (var validator = new OFDValidator(reader))
        {
            validator.SetValidator(new SESV4ValidateContainer());
            validator.ExeValidate();
        }
    }
}
