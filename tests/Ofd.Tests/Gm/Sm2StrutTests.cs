using System.Text;
using Ofd.Gm.Cert;
using Ofd.Gm.Sm2Strut;
using Ofd.Gm.Sm2Strut.Builder;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Tests.Gm;

/// <summary>sm2strut（GB/T 35275 SignedData）：结构往返、SM2Cipher 格式转换、签名→验签闭环。</summary>
public class Sm2StrutTests
{
    private static byte[] Sm3(byte[] data)
    {
        var md = new SM3Digest();
        md.BlockUpdate(data, 0, data.Length);
        var output = new byte[md.GetDigestSize()];
        md.DoFinal(output, 0);
        return output;
    }

    /// <summary>生成 SM2 自签证书与私钥。</summary>
    private static (BcX509Certificate Cert, ECPrivateKeyParameters Priv) NewSm2Cert()
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

        var gen = new ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new SecureRandom()));
        var kp = gen.GenerateKeyPair();
        var priv = (ECPrivateKeyParameters)kp.Private;
        var pub = (ECPublicKeyParameters)kp.Public;

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(BigInteger.ValueOf(1));
        certGen.SetIssuerDN(new X509Name("CN=OFD-Test"));
        certGen.SetSubjectDN(new X509Name("CN=OFD-Test"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(pub);

        var cert = certGen.Generate(new Asn1SignatureFactory("SM3withSM2", priv));
        return (cert, priv);
    }

    private static byte[] Sign(byte[] data, ECPrivateKeyParameters priv)
    {
        var signer = new SM2Signer();
        signer.Init(true, priv);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    [Fact]
    public void ContentInfo_round_trip_and_invalid_tag()
    {
        var ci = new ContentInfo(OIDs.Data, new DerOctetString(new byte[] { 1, 2, 3 }));
        var parsed = ContentInfo.GetInstance(ci.GetEncoded())!;

        Assert.Equal(OIDs.Data, parsed.GetContentType());
        Assert.Equal(new byte[] { 1, 2, 3 }, Asn1OctetString.GetInstance(parsed.GetContent()!).GetOctets());

        // 无内容
        var empty = ContentInfo.GetInstance(new ContentInfo(OIDs.SignedData, null).GetEncoded())!;
        Assert.Null(empty.GetContent());

        // 非法标签（[1] 而非 [0]）应拒绝
        var bad = new DerSequence(new Asn1Encodable[]
        {
            OIDs.Data,
            new DerTaggedObject(true, 1, new DerOctetString(new byte[] { 1 })),
        });
        Assert.Throws<ArgumentException>(() => new ContentInfo(bad));
    }

    [Fact]
    public void SignedData_and_SignerInfo_round_trip()
    {
        var hash = Sm3(Encoding.UTF8.GetBytes("hello"));
        var iaSn = new IssuerAndSerialNumber(new X509Name("CN=Signer"), new DerInteger(BigInteger.ValueOf(42)));

        var signerInfo = new SignerInfo(
            iaSn,
            new AlgorithmIdentifier(OIDs.Sm3),
            new AlgorithmIdentifier(OIDs.Sm2Sign),
            new DerOctetString(new byte[] { 7, 7, 7 }));

        var signedData = new SignedData(
            new DerSet(new AlgorithmIdentifier(OIDs.Sm3)),
            new ContentInfo(OIDs.Data, new DerOctetString(hash)),
            new DerSet(new Asn1EncodableVector()),
            new DerSet(new Asn1EncodableVector { signerInfo }));

        byte[] encoded = signedData.GetEncoded();
        var parsed = SignedData.GetInstance(encoded)!;

        Assert.Equal(1, parsed.GetVersion().IntValueExact);
        Assert.Equal(OIDs.Data, parsed.GetContentInfo()!.GetContentType());

        var parsedSigner = SignerInfo.GetInstance(parsed.GetSignerInfos()![0])!;
        Assert.Equal(42, parsedSigner.GetIssuerAngSerialNumber()!.GetCertSerialNumber()!.IntValueExact);
        Assert.Equal("CN=Signer", parsedSigner.GetIssuerAngSerialNumber()!.GetName()!.ToString());
        Assert.Equal(new byte[] { 7, 7, 7 }, parsedSigner.GetEncryptedDigest()!.GetOctets());
        Assert.Equal(OIDs.Sm2Sign, parsedSigner.GetDigestEncryptionAlgorithm()!.Algorithm);

        Assert.Equal(encoded, parsed.GetEncoded());
    }

    [Fact]
    public void SM2Cipher_c1c3c2_conversion_round_trip()
    {
        var cipher = new SM2Cipher
        {
        };
        cipher.SetxCoordinate(new DerInteger(new BigInteger("1234567890ABCDEF", 16)));
        cipher.SetyCoordinate(new DerInteger(new BigInteger("FEDCBA0987654321", 16)));
        cipher.SetHash(new DerOctetString(new byte[32]));
        cipher.SetCipherText(new DerOctetString(new byte[] { 9, 9, 9, 9 }));

        byte[] c1c3c2 = cipher.ConvertC1C3C2();
        Assert.Equal(1 + 32 + 32 + 32 + 4, c1c3c2.Length);
        Assert.Equal(0x04, c1c3c2[0]);

        var back = SM2Cipher.FromC1C3C2(c1c3c2);
        Assert.Equal(cipher.GetxCoordinate()!.Value, back.GetxCoordinate()!.Value);
        Assert.Equal(cipher.GetyCoordinate()!.Value, back.GetyCoordinate()!.Value);
        Assert.Equal(new byte[32], back.GetHash()!.GetOctets());
        Assert.Equal(new byte[] { 9, 9, 9, 9 }, back.GetCipherText()!.GetOctets());

        // ASN.1 往返
        var parsed = SM2Cipher.GetInstance(back.GetEncoded())!;
        Assert.Equal(cipher.GetxCoordinate()!.Value, parsed.GetxCoordinate()!.Value);
    }

    [Fact]
    public void Sign_then_validate_end_to_end()
    {
        var (cert, priv) = NewSm2Cert();
        byte[] content = Encoding.UTF8.GetBytes("OFD 签章原文-测试");
        byte[] digest = Sm3(content);
        byte[] signature = Sign(digest, priv);

        var signedData = SignedDataBuilder.SignedData(digest, signature, cert);
        var contentInfo = new ContentInfo(OIDs.SignedData, signedData);
        byte[] signedValue = contentInfo.GetEncoded();

        // 验证通过
        var ok = GBT35275Validate.Validate("SM3withSM2", content, signedValue);
        Assert.True(ok.Result, ok.Hit);

        // 原文被篡改 → 摘要不符
        var tampered = GBT35275Validate.Validate("SM3withSM2", Encoding.UTF8.GetBytes("篡改"), signedValue);
        Assert.False(tampered.Result);
        Assert.Equal("待签名原文不符", tampered.Hit);

        // 证书查找（颁发者 + 序列号）
        var parsedSignedData = SignedData.GetInstance(signedData.GetEncoded())!;
        var signerInfo = SignerInfo.GetInstance(parsedSignedData.GetSignerInfos()![0])!;
        var found = parsedSignedData.GetSignCertX509(signerInfo.GetIssuerAngSerialNumber());
        Assert.NotNull(found);
        Assert.Equal(cert.SerialNumber, found!.SerialNumber);
    }

    [Fact]
    public void PKCS9_builder_produces_message_digest_attribute_and_validates()
    {
        var (cert, priv) = NewSm2Cert();
        byte[] content = Encoding.UTF8.GetBytes("PKCS9 模式原文");
        byte[] digest = Sm3(content);

        // 签名函数：对输入（authenticatedAttributes 编码）做 SM2 签名
        var signedData = PKCS9SignedDataBuilder.SignedData(content, data => Sign(data, priv), cert, null);

        // authenticatedAttributes 内含 messageDigest = SM3(原文)
        var signerInfo = SignerInfo.GetInstance(signedData.GetSignerInfos()![0])!;
        var attrs = signerInfo.GetAuthenticatedAttributes();
        Assert.NotNull(attrs);
        var table = new Org.BouncyCastle.Asn1.Cms.AttributeTable(attrs!);
        var mdAttr = table[Org.BouncyCastle.Asn1.Cms.CmsAttributes.MessageDigest]!;
        Assert.Equal(digest, ((Asn1OctetString)mdAttr.AttrValues[0]).GetOctets());
        Assert.NotNull(table[Org.BouncyCastle.Asn1.Cms.CmsAttributes.SigningTime]);

        // 端到端验签（PKCS#9 分支：签名内容为 attributes 编码）
        byte[] signedValue = new ContentInfo(OIDs.SignedData, signedData).GetEncoded();
        var ok = GBT35275Validate.Validate("SM3withSM2", content, signedValue);
        Assert.True(ok.Result, ok.Hit);
    }

    [Fact]
    public void Validate_rejects_wrong_content_type_and_missing_cert()
    {
        // 非 signedData 类型
        byte[] notSigned = new ContentInfo(OIDs.Data, new DerOctetString(new byte[] { 1 })).GetEncoded();
        Assert.Throws<ArgumentException>(() => GBT35275Validate.Validate("SM3withSM2", new byte[] { 1 }, notSigned));

        // signedData 但无证书 → 找不到匹配证书
        var hash = Sm3(new byte[] { 1, 2 });
        var signerInfo = new SignerInfo(
            new IssuerAndSerialNumber(new X509Name("CN=Ghost"), new DerInteger(BigInteger.ValueOf(9))),
            new AlgorithmIdentifier(OIDs.Sm3),
            new AlgorithmIdentifier(OIDs.Sm2Sign),
            new DerOctetString(new byte[] { 1 }));

        var signedData = new SignedData(
            new DerSet(new AlgorithmIdentifier(OIDs.Sm3)),
            new ContentInfo(OIDs.Data, new DerOctetString(hash)),
            new DerSet(new Asn1EncodableVector()),
            new DerSet(new Asn1EncodableVector { signerInfo }));

        var res = GBT35275Validate.Validate("SM3withSM2", new byte[] { 1, 2 },
            new ContentInfo(OIDs.SignedData, signedData).GetEncoded());
        Assert.False(res.Result);
        Assert.Equal("没有找到匹配的证书无法验证签名", res.Hit);
    }

    [Fact]
    public void CertTools_converts_between_certificate_and_asn1()
    {
        var (cert, _) = NewSm2Cert();
        var asn1 = CertTools.Asn1(cert);

        var back = CertTools.Obj(asn1);
        Assert.Equal(cert.SerialNumber, back.SerialNumber);
        Assert.Equal(cert.SubjectDN.ToString(), back.SubjectDN.ToString());

        // SignedDataBuilder 生成的证书集合可被 TbsCertificateStructure 解析
        var tbs = TbsCertificateStructure.GetInstance(asn1[0]);
        Assert.Equal(1, tbs.SerialNumber.IntValueExact);
    }
}
