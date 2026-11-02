using Ofd.Gm.Ses.Parse;
using Ofd.Gm.Ses.V1;
using Org.BouncyCastle.Asn1;
using Xunit;

namespace Ofd.Tests.Gm;

/// <summary>SES v1（GM/T 0031-2014）结构编解码与版本识别。</summary>
public class SesV1Tests
{
    private static SES_ESPropertyInfo BuildProperty() => new SES_ESPropertyInfo()
        .SetType(SES_ESPropertyInfo.OrgType)
        .SetName("v1 印章")
        .SetCertList(new DerSequence(new DerOctetString(new byte[] { 1, 2 })))
        .SetCreateDate(new DerUtcTime("260101000000Z"))
        .SetValidStart(new DerUtcTime("260101000000Z"))
        .SetValidEnd(new DerUtcTime("270101000000Z"));

    private static SESeal BuildSeal() => new SESeal(
        new SES_SealInfo(
            new SES_Header().SetVersion(1).SetVid("V1-VID"),
            new DerIA5String("ES-V1-001"),
            BuildProperty(),
            new SES_ESPictrueInfo()
                .SetType("GIF")
                .SetData(new byte[] { 0x47, 0x49, 0x46, 0x38 })
                .SetWidth(40)
                .SetHeight(40),
            null),
        new SES_SignInfo(
            new DerOctetString(new byte[] { 9 }),
            new DerObjectIdentifier("1.2.156.10197.1.501"),
            new DerBitString(new byte[] { 8, 8 })));

    [Fact]
    public void V1_seal_round_trip_and_version_detection()
    {
        byte[] encoded = BuildSeal().GetEncoded();

        // 顶层 2 字段 → v1
        var holder = VersionParser.ParseSesSealVersion(encoded);
        Assert.Equal(SESVersion.V1, holder.GetVersion());

        var seal = holder.GetInstanceV1Seal();
        Assert.NotNull(seal);

        var sealInfo = seal!.GetEsealInfo()!;
        Assert.Equal("ES", sealInfo.GetHeader()!.GetId().GetString());
        Assert.Equal(1, sealInfo.GetHeader()!.GetVersion().IntValueExact);
        Assert.Equal("V1-VID", sealInfo.GetHeader()!.GetVid().GetString());
        Assert.Equal("ES-V1-001", sealInfo.GetEsID()!.GetString());
        Assert.Equal("v1 印章", sealInfo.GetProperty()!.GetName()!.GetString());
        Assert.Equal("GIF", sealInfo.GetPicture()!.GetType()!.GetString());
        Assert.Equal(new byte[] { 0x47, 0x49, 0x46, 0x38 }, sealInfo.GetPicture()!.GetData()!.GetOctets());
        Assert.Equal("260101000000Z", sealInfo.GetProperty()!.GetCreateDate()!.TimeString);

        // 制章人签名信息
        Assert.NotNull(seal.GetSignInfo());
        Assert.Equal(new byte[] { 9 }, seal.GetSignInfo()!.GetCert()!.GetOctets());
        Assert.Equal(new byte[] { 8, 8 }, seal.GetSignInfo()!.GetSignData()!.GetBytes());

        // DER 稳定
        Assert.Equal(encoded, seal.GetEncoded());
    }

    [Fact]
    public void V1_signature_round_trip_and_picture_extraction()
    {
        var tbs = new TBS_Sign()
            .SetVersion(new DerInteger(1))
            .SetEseal(BuildSeal())
            .SetTimeInfo(new DerBitString(new byte[] { 3, 3 }))
            .SetDataHash(new DerBitString(new byte[] { 4, 4, 4 }))
            .SetPropertyInfo(new DerIA5String("prop"))
            .SetCert(new DerOctetString(new byte[] { 5 }))
            .SetSignatureAlgorithm(new DerObjectIdentifier("1.2.156.10197.1.501"));

        var sig = new SES_Signature(tbs, new DerBitString(new byte[] { 6, 6 }));
        byte[] encoded = sig.GetEncoded();

        var holder = VersionParser.ParseSesSignatureVersion(encoded);
        Assert.Equal(SESVersion.V1, holder.GetVersion());

        var parsed = holder.GetInstanceV1()!;
        Assert.Equal(new byte[] { 6, 6 }, parsed.GetSignature()!.GetBytes());
        Assert.Equal(new byte[] { 4, 4, 4 }, parsed.GetToSign()!.GetDataHash()!.GetBytes());
        Assert.Equal("prop", parsed.GetToSign()!.GetPropertyInfo()!.GetString());

        // 印章图片可层层取到（Reader 的 v1 图片提取路径）
        var picture = parsed.GetToSign()!.GetEseal()!.GetEsealInfo()!.GetPicture()!;
        Assert.Equal("GIF", picture.GetType()!.GetString());
        Assert.True(picture.GetData()!.GetOctets().Length > 0);

        Assert.Equal(encoded, parsed.GetEncoded());
    }

    [Fact]
    public void V1_property_accepts_generalized_time_for_compatibility()
    {
        // 非标签章可能用 GeneralizedTime 表示日期，解析时应兼容为 UTC 时间
        var seq = new DerSequence(new Asn1Encodable[]
        {
            new DerInteger(1),
            new DerUtf8String("兼容章"),
            new DerSequence(new DerOctetString(new byte[] { 1 })),
            new DerGeneralizedTime("20260101000000Z"),
            new DerGeneralizedTime("20260101000000Z"),
            new DerGeneralizedTime("20270101000000Z"),
        });

        var property = new SES_ESPropertyInfo(seq);
        Assert.Equal("兼容章", property.GetName()!.GetString());
        Assert.NotNull(property.GetCreateDate());
        Assert.NotNull(property.GetValidEnd());
    }
}
