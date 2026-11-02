using System.IO.Compression;
using Ofd.Reader;
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Ofd.Sign.Verify.Exceptions;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Xunit;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using V1 = Ofd.Gm.Ses.V1;
using V5 = Ofd.Gm.Ses.V5;

namespace Ofd.Tests.Sign;

/// <summary>SES v1 / v5 电子签章：签章 → 验签 → 篡改检测。</summary>
public class SesV1V5SignTests : IDisposable
{
    private readonly string _root;

    public SesV1V5SignTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_ses15_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static byte[] Sm2Sign(byte[] data, ECPrivateKeyParameters priv)
    {
        var signer = new SM2Signer();
        signer.Init(true, priv);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    private static V1.SESeal BuildSealV1(BcX509Certificate cert, ECPrivateKeyParameters priv)
    {
        var property = new V1.SES_ESPropertyInfo()
            .SetType(new DerInteger(1))
            .SetName("v1 测试印章")
            .SetCertList(new DerSequence(new DerOctetString(cert.GetEncoded())))
            .SetCreateDate(new DerUtcTime("260101000000Z"))
            .SetValidStart(new DerUtcTime("260101000000Z"))
            .SetValidEnd(new DerUtcTime("270101000000Z"));

        var sealInfo = new V1.SES_SealInfo()
            .SetHeader(new V1.SES_Header().SetVersion(1).SetVid("V1-VID"))
            .SetEsID("ES-V1-001")
            .SetProperty(property)
            .SetPicture(new V1.SES_ESPictrueInfo().SetType("GIF")
                .SetData(new byte[] { 1, 2, 3 }).SetWidth(40).SetHeight(40));

        var signInfo = new V1.SES_SignInfo(
            new DerOctetString(cert.GetEncoded()),
            GMObjectIdentifiers.sm2sign_with_sm3,
            new DerBitString(Sm2Sign(sealInfo.GetEncoded(), priv)));

        return new V1.SESeal(sealInfo, signInfo);
    }

    private static V5.SESeal BuildSealV5(BcX509Certificate cert, ECPrivateKeyParameters priv)
    {
        var property = new V5.SES_ESPropertyInfo()
            .SetType(new DerInteger(1))
            .SetName(new DerUtf8String("v5 测试印章"))
            .SetCertListType(V5.SES_ESPropertyInfo.CertListType)
            .SetCertList(new V5.SES_CertList(
                new V5.CertInfoList().Add(new DerOctetString(cert.GetEncoded()))))
            .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

        var sealInfo = new V5.SES_SealInfo()
            .SetHeader(new V5.SES_Header().SetVersion(5).SetVid("V5-VID"))
            .SetEsID("ES-V5-001")
            .SetProperty(property)
            .SetPicture(new V5.SES_ESPictrueInfo().SetType("PNG")
                .SetData(new byte[] { 4, 5, 6 }).SetWidth(40).SetHeight(40));

        return new V5.SESeal(sealInfo, new DerOctetString(cert.GetEncoded()),
            GMObjectIdentifiers.sm2sign_with_sm3,
            new DerBitString(Sm2Sign(sealInfo.GetEncoded(), priv)), null);
    }

    private string Sign(string plain, IExtendSignatureContainer container, string name)
    {
        string output = Path.Combine(_root, name);
        using var reader = new OFDReader(plain);
        using var signer = new OFDSigner(reader, output);
        signer.SetSignContainer(container).ExeSign();
        return output;
    }

    private static void Validate(string ofd, IValidateContainer container)
    {
        using var validator = new OFDValidator(new OFDReader(ofd));
        validator.SetValidator(container);
        validator.ExeValidate();
    }

    private string Tamper(string ofd, string tag, Action<string> mutate)
    {
        string dir = Path.Combine(_root, "repack-" + tag);
        ZipFile.ExtractToDirectory(ofd, dir);
        mutate(dir);
        string outPath = Path.Combine(_root, tag + ".ofd");
        ZipFile.CreateFromDirectory(dir, outPath);
        return outPath;
    }

    [Fact]
    public void Ses_v1_signs_and_validates()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(61);

        string signed = Sign(plain, new SESV1Container(priv, BuildSealV1(cert, priv), cert), "v1.ofd");
        Validate(signed, new SESV1ValidateContainer()); // 含 OFDValidator 的 v1 印章匹配分支
    }

    [Fact]
    public void Ses_v5_signs_and_validates_with_timestamp()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(62);

        var container = new SESV5Container(priv, BuildSealV5(cert, priv), cert,
            _ => new byte[] { 0xAA, 0xBB });

        string signed = Sign(plain, container, "v5.ofd");
        Validate(signed, new SESV5ValidateContainer()); // 含 OFDValidator 的 v5 印章匹配分支

        // 时间戳存在 → SignedValue 顶层 5 个字段
        using var zip = ZipFile.OpenRead(signed);
        var entry = zip.Entries.First(e => e.FullName.EndsWith("SignedValue.dat"));
        using var s = entry.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        var seq = Asn1Sequence.GetInstance(Asn1Object.FromByteArray(ms.ToArray()));
        Assert.Equal(5, seq.Count);
    }

    [Fact]
    public void Ses_v1_tampered_signature_xml_fails()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(63);
        string signed = Sign(plain, new SESV1Container(priv, BuildSealV1(cert, priv), cert), "v1t.ofd");

        string tampered = Tamper(signed, "v1-tampered", dir =>
        {
            string p = Path.Combine(dir, "Doc_0", "Signs", "Sign_0", "Signature.xml");
            File.WriteAllText(p, File.ReadAllText(p).Replace("<ofd:SignatureDateTime>", "<ofd:SignatureDateTime>1999-01-01 "));
        });

        Assert.Throws<InvalidSignedValueException>(() => Validate(tampered, new SESV1ValidateContainer()));
    }

    [Fact]
    public void Ses_v5_tampered_signature_xml_fails()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(64);
        string signed = Sign(plain, new SESV5Container(priv, BuildSealV5(cert, priv), cert), "v5t.ofd");

        string tampered = Tamper(signed, "v5-tampered", dir =>
        {
            string p = Path.Combine(dir, "Doc_0", "Signs", "Sign_0", "Signature.xml");
            File.WriteAllText(p, File.ReadAllText(p).Replace("<ofd:SignatureDateTime>", "<ofd:SignatureDateTime>1999-01-01 "));
        });

        Assert.Throws<InvalidSignedValueException>(() => Validate(tampered, new SESV5ValidateContainer()));
    }

    [Fact]
    public void Cross_version_validator_rejects_other_version()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(65);
        string signed = Sign(plain, new SESV1Container(priv, BuildSealV1(cert, priv), cert), "cross.ofd");

        // 用 v5 验证器验证 v1 数据：结构不同应失败（而非误判通过）
        Assert.ThrowsAny<Exception>(() => Validate(signed, new SESV5ValidateContainer()));
    }
}
