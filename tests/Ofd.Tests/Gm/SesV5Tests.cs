using Ofd.Gm.Ses.Parse;
using Ofd.Gm.Ses.V5;
using Org.BouncyCastle.Asn1;
using Xunit;

namespace Ofd.Tests.Gm;

/// <summary>SES v5（GM/T 0031-2025）结构编解码与版本识别。</summary>
public class SesV5Tests
{
    private static SES_ESPropertyInfo BuildProperty() => new SES_ESPropertyInfo()
        .SetType(SES_ESPropertyInfo.CertListType)
        .SetName(new DerUtf8String("v5 印章"))
        .SetCertListType(SES_ESPropertyInfo.CertListType)
        .SetCertList(new SES_CertList(new CertInfoList().Add(new DerOctetString(new byte[] { 1, 2, 3 }))))
        .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
        .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
        .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

    private static SESeal BuildSeal(bool withTimeStamp)
    {
        var seal = new SESeal(
            new SES_SealInfo(
                new SES_Header().SetVersion(5).SetVid("V5-VID"),
                new DerIA5String("ES-V5-001"),
                BuildProperty(),
                new SES_ESPictrueInfo()
                    .SetType("SVG")
                    .SetData(new byte[] { 1, 2, 3, 4, 5 })
                    .SetWidth(42)
                    .SetHeight(42),
                null),
            new DerOctetString(new byte[] { 7 }),
            new DerObjectIdentifier("1.2.156.10197.1.501"),
            new DerBitString(new byte[] { 6, 6 }),
            withTimeStamp ? new DerBitString(new byte[] { 9, 9 }) : null);
        return seal;
    }

    [Fact]
    public void V5_seal_round_trip_with_and_without_timestamp()
    {
        // 带时间戳：5 字段 → v5
        var sealWithTs = BuildSeal(true);
        byte[] encoded5 = sealWithTs.GetEncoded();
        Assert.Equal(5, Asn1Sequence.GetInstance(Asn1Object.FromByteArray(encoded5)).Count);

        var holder5 = VersionParser.ParseSesSealVersion(encoded5);
        Assert.Equal(SESVersion.V5, holder5.GetVersion());
        var parsed5 = holder5.GetInstanceV5Seal()!;
        Assert.Equal(new byte[] { 9, 9 }, parsed5.GetTimeStamp()!.GetBytes());
        Assert.Equal("SVG", parsed5.GeteSealInfo()!.GetPicture()!.GetType()!.GetString());

        // 无时间戳：4 字段 + header.version=5 → 仍判定 v5
        var sealNoTs = BuildSeal(false);
        byte[] encoded4 = sealNoTs.GetEncoded();
        Assert.Equal(4, Asn1Sequence.GetInstance(Asn1Object.FromByteArray(encoded4)).Count);

        var holder4 = VersionParser.ParseSesSealVersion(encoded4);
        Assert.Equal(SESVersion.V5, holder4.GetVersion());
        Assert.Null(holder4.GetInstanceV5Seal()!.GetTimeStamp());

        // DER 稳定
        Assert.Equal(encoded4, holder4.GetInstanceV5Seal()!.GetEncoded());
    }

    [Fact]
    public void V5_signature_round_trip_and_picture_extraction()
    {
        var tbs = new TBS_Sign()
            .SetVersion(new DerInteger(5))
            .SetEseal(BuildSeal(true))
            .SetTimeInfo(new DerGeneralizedTime("20261021000000Z"))
            .SetDataHash(new DerBitString(new byte[] { 4, 4, 4 }))
            .SetPropertyInfo("v5-prop");

        var sig = new SES_Signature(
            tbs,
            new DerOctetString(new byte[] { 5 }),
            new DerObjectIdentifier("1.2.156.10197.1.501"),
            new DerBitString(new byte[] { 8, 8, 8 }),
            new DerBitString(new byte[] { 9 }));

        byte[] encoded = sig.GetEncoded();
        var holder = VersionParser.ParseSesSignatureVersion(encoded);
        Assert.Equal(SESVersion.V5, holder.GetVersion());

        var parsed = holder.GetInstanceV5()!;
        Assert.Equal(new byte[] { 8, 8, 8 }, parsed.GetSignatureValue()!.GetBytes());
        Assert.Equal("v5-prop", parsed.GetToSign()!.GetPropertyInfo()!.GetString());
        Assert.Equal(new byte[] { 4, 4, 4 }, parsed.GetToSign()!.GetDataHash()!.GetBytes());
        Assert.Equal(5, parsed.GetToSign()!.GetVersion()!.IntValueExact);
        Assert.Equal(5, parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetHeader()!.GetVersion().IntValueExact);

        var picture = parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetPicture()!;
        Assert.Equal("SVG", picture.GetType()!.GetString());
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, picture.GetData()!.GetOctets());
        Assert.Equal(42, picture.GetWidth()!.IntValueExact);

        Assert.Equal(encoded, parsed.GetEncoded());
    }

    [Fact]
    public void V5_cert_digest_list_variant()
    {
        var property = new SES_ESPropertyInfo()
            .SetType(SES_ESPropertyInfo.CertDigestListType)
            .SetName(new DerUtf8String("杂凑值章"))
            .SetCertListType(SES_ESPropertyInfo.CertDigestListType)
            .SetCertList(new SES_CertList(new CertDigestList()
                .Add(new CertDigestObj().SetType("SM3").SetValue(new byte[] { 1, 1 }))))
            .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

        var encoded = property.GetEncoded();
        var parsed = SES_ESPropertyInfo.GetInstance(encoded)!;

        Assert.Equal(2, parsed.GetCertListType()!.IntValueExact);
        Assert.Null(parsed.GetCertList()!.GetCerts());
        Assert.Single(parsed.GetCertList()!.GetCertDigestList()!);
        Assert.Equal("SM3", parsed.GetCertList()!.GetCertDigestList()!.Get(0).GetType()!.GetString());
    }
}
