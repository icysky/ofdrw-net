using System.IO.Compression;
using Ofd.Gm.Ses.Parse;
using Ofd.Gm.Ses.V4;
using Org.BouncyCastle.Asn1;
using Xunit;
// v1 与 v4 存在同名结构类，此处仅按需取用 v1 的印章图片与头信息
using SES_Header = Ofd.Gm.Ses.V1.SES_Header;
using SES_ESPictrueInfo = Ofd.Gm.Ses.V1.SES_ESPictrueInfo;

namespace Ofd.Tests.Gm;

/// <summary>
/// SES v4 golden：解析真实 OFD 样本中的 SignedValue.dat（Java 侧生成的电子签章数据），
/// 校验版本识别、印章图片与各字段；并做自建结构的编解码往返。
/// </summary>
public class SesV4GoldenTests
{
    private static readonly string SamplePath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "V4RideRight.ofd");

    private static byte[] LoadSignedValue()
    {
        Assert.True(File.Exists(SamplePath), "缺少 golden 样本 V4RideRight.ofd");

        using var zip = ZipFile.OpenRead(SamplePath);
        var entry = zip.Entries.FirstOrDefault(e =>
            e.FullName.EndsWith("SignedValue.dat", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(entry);

        using var s = entry!.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Parse_real_v4_signed_value_extracts_seal_picture()
    {
        byte[] bin = LoadSignedValue();
        Assert.True(bin.Length > 1000, $"SignedValue.dat 过小: {bin.Length}");

        // 版本识别：签章数据 v4
        var holder = VersionParser.ParseSesSignatureVersion(bin);
        Assert.Equal(SESVersion.V4, holder.GetVersion());

        var sig = holder.GetInstanceV4();
        Assert.NotNull(sig);

        var toSign = sig!.GetToSign();
        Assert.NotNull(toSign);

        // 原文杂凑值、签章时间、属性信息
        Assert.NotNull(toSign!.GetDataHash());
        Assert.True(toSign.GetDataHash()!.GetBytes().Length > 0);
        Assert.NotNull(toSign.GetTimeInfo());
        Assert.NotNull(toSign.GetPropertyInfo());

        // 电子印章：头部版本 4、印章标识、图片
        var eseal = toSign.GetEseal();
        Assert.NotNull(eseal);
        var sealInfo = eseal!.GeteSealInfo();
        Assert.NotNull(sealInfo);

        var header = sealInfo!.GetHeader();
        Assert.NotNull(header);
        Assert.Equal("ES", header!.GetId().GetString());
        Assert.Equal(4, header.GetVersion().IntValueExact);

        Assert.False(string.IsNullOrWhiteSpace(sealInfo.GetEsID()?.GetString()));

        var picture = sealInfo.GetPicture();
        Assert.NotNull(picture);
        string imgType = picture!.GetType()!.GetString();
        byte[] imgData = picture.GetData()!.GetOctets();
        Assert.False(string.IsNullOrWhiteSpace(imgType));
        Assert.True(imgData.Length > 100, $"印章图片过小: {imgData.Length}");
        Assert.True(picture.GetWidth()!.IntValueExact > 0);
        Assert.True(picture.GetHeight()!.IntValueExact > 0);

        // 签章者证书与签名值
        Assert.NotNull(sig.GetCert());
        Assert.True(sig.GetCert()!.GetOctets().Length > 0);
        Assert.NotNull(sig.GetSignatureAlgID());
        Assert.True(sig.GetSignature()!.GetBytes().Length > 0);

        // 印章属性：证书列表类型 1 或 2，且列表非空
        var property = sealInfo.GetProperty();
        Assert.NotNull(property);
        int certListType = property!.GetCertListType()!.IntValueExact;
        Assert.Contains(certListType, new[] { 1, 2 });
        var certList = property.GetCertList();
        Assert.NotNull(certList);
        if (certListType == 1)
        {
            Assert.NotNull(certList!.GetCerts());
            Assert.True(certList.GetCerts()!.Count > 0);
        }
        else
        {
            Assert.NotNull(certList!.GetCertDigestList());
            Assert.True(certList.GetCertDigestList()!.Count > 0);
        }

        // 印章自身签名值
        Assert.True(eseal.GetSignedValue()!.GetBytes().Length > 0);

        // ASN.1 结构字段数（4~5，含可选时间戳）
        var seq = Asn1Sequence.GetInstance(Asn1Object.FromByteArray(bin));
        Assert.InRange(seq.Count, 4, 5);
    }

    [Fact]
    public void V4_structure_encode_decode_round_trip()
    {
        var header = new SES_Header().SetVersion(4).SetVid("TEST");
        var picture = new SES_ESPictrueInfo()
            .SetType("PNG")
            .SetData(new byte[] { 1, 2, 3, 4 })
            .SetWidth(40)
            .SetHeight(40);
        var property = new SES_ESPropertyInfo()
            .SetType(new DerInteger(1))
            .SetName(new DerUtf8String("测试印章"))
            .SetCertListType(SES_ESPropertyInfo.CertListType)
            .SetCertList(new SES_CertList(new CertInfoList().Add(new DerOctetString(new byte[] { 9, 9 }))))
            .SetCreateDate(new DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new DerGeneralizedTime("20270101000000Z"));

        var sealInfo = new SES_SealInfo(header, new DerIA5String("ES-001"), property, picture, null);
        var eseal = new SESeal(sealInfo, new DerOctetString(new byte[] { 7 }), new DerObjectIdentifier("1.2.156.10197.1.501"), new DerBitString(new byte[] { 8 }));

        var tbs = new TBS_Sign()
            .SetVersion(new DerInteger(4))
            .SetEseal(eseal)
            .SetTimeInfo(new DerGeneralizedTime("20261021000000Z"))
            .SetDataHash(new byte[] { 5, 5 })
            .SetPropertyInfo("test");

        var sig = new SES_Signature(tbs, new DerOctetString(new byte[] { 6 }),
            new DerObjectIdentifier("1.2.156.10197.1.501"), new DerBitString(new byte[] { 4, 4 }), null);

        byte[] encoded = sig.GetEncoded();

        // 版本识别 + 反解
        var holder = VersionParser.ParseSesSignatureVersion(encoded);
        Assert.Equal(SESVersion.V4, holder.GetVersion());

        var parsed = holder.GetInstanceV4()!;
        Assert.Equal("ES", parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetHeader()!.GetId().GetString());
        Assert.Equal(4, parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetHeader()!.GetVersion().IntValueExact);
        Assert.Equal("测试印章", parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetProperty()!.GetName()!.GetString());
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetPicture()!.GetData()!.GetOctets());
        Assert.Equal("PNG", parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetPicture()!.GetType()!.GetString());
        Assert.Equal(40, parsed.GetToSign()!.GetEseal()!.GeteSealInfo()!.GetPicture()!.GetWidth()!.IntValueExact);
        Assert.Equal(new byte[] { 5, 5 }, parsed.GetToSign()!.GetDataHash()!.GetBytes());
        Assert.Equal("test", parsed.GetToSign()!.GetPropertyInfo()!.GetString());

        // 两次编码结果一致（DER 稳定）
        Assert.Equal(encoded, parsed.GetEncoded());
    }

    [Fact]
    public void OFDReader_extracts_stamp_annot_with_seal_image()
    {
        using var reader = new Ofd.Reader.OFDReader(SamplePath);

        Assert.True(reader.HasSignature());
        Assert.NotNull(reader.GetDefaultSignatures());
        Assert.Equal(1, reader.GetDefaultSignatures()!.GetSignatures().Count);

        var stamps = reader.GetStampAnnots();
        var stamp = Assert.Single(stamps);

        Assert.Equal(SESVersion.V4, stamp.GetHolder()!.GetVersion());
        Assert.True(stamp.GetImageByte().Length > 100, $"印章图片过小: {stamp.GetImageByte().Length}");
        Assert.False(string.IsNullOrWhiteSpace(stamp.GetImgType()));
        Assert.NotNull(stamp.GetSignedInfo());
    }

    [Fact]
    public void Version_parser_rejects_unknown_structure()
    {
        // 只有 3 个元素的序列不匹配任何已知版本
        var bad = new DerSequence(new Asn1Encodable[] { new DerInteger(1), new DerInteger(2), new DerInteger(3) });
        Assert.Throws<ArgumentException>(() => VersionParser.ParseSesSignatureVersion(bad.GetEncoded()));
        Assert.Throws<ArgumentException>(() => VersionParser.ParseSesSealVersion(bad.GetEncoded()));
    }
}
