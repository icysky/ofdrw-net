using System.IO.Compression;
using Ofd.Core.Dom;
using Ofd.Reader;
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.StampPos;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Xunit;
using SigSignature = Ofd.Core.Signatures.Sig.Signature;

namespace Ofd.Tests.Sign;

/// <summary>骑缝章定位（RidingStampPos / CuttingRideStampPos）与签名清理（SignCleaner）。</summary>
public class StampPosAndCleanTests : IDisposable
{
    private readonly string _root;

    public StampPosAndCleanTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_stamppos_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string Sign(string plain, IStampAppearance pos, string name)
    {
        var (priv, cert) = SignTestKit.NewSm2Cert(71);
        var seal = BuildSeal(cert, priv);

        string output = Path.Combine(_root, name);
        using var reader = new OFDReader(plain);
        using var signer = new OFDSigner(reader, output);
        signer.SetSignContainer(new SESV4Container(priv, seal, cert)).AddApPos(pos).ExeSign();
        return output;
    }

    private static Ofd.Gm.Ses.V4.SESeal BuildSeal(Org.BouncyCastle.X509.X509Certificate cert,
        Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters priv)
    {
        var property = new Ofd.Gm.Ses.V4.SES_ESPropertyInfo()
            .SetType(new Org.BouncyCastle.Asn1.DerInteger(1))
            .SetName(new Org.BouncyCastle.Asn1.DerUtf8String("骑缝测试"))
            .SetCertListType(Ofd.Gm.Ses.V4.SES_ESPropertyInfo.CertListType)
            .SetCertList(new Ofd.Gm.Ses.V4.SES_CertList(
                new Ofd.Gm.Ses.V4.CertInfoList().Add(new Org.BouncyCastle.Asn1.DerOctetString(cert.GetEncoded()))))
            .SetCreateDate(new Org.BouncyCastle.Asn1.DerGeneralizedTime("20260101000000Z"))
            .SetValidStart(new Org.BouncyCastle.Asn1.DerGeneralizedTime("20260101000000Z"))
            .SetValidEnd(new Org.BouncyCastle.Asn1.DerGeneralizedTime("20270101000000Z"));

        var sealInfo = new Ofd.Gm.Ses.V4.SES_SealInfo()
            .SetHeader(new Ofd.Gm.Ses.V1.SES_Header().SetVersion(4).SetVid("R"))
            .SetEsID("ES-RIDING")
            .SetProperty(property)
            .SetPicture(new Ofd.Gm.Ses.V1.SES_ESPictrueInfo()
                .SetType("PNG").SetData(new byte[] { 1, 2, 3 }).SetWidth(40).SetHeight(40));

        return new Ofd.Gm.Ses.V4.SESeal(sealInfo, new Org.BouncyCastle.Asn1.DerOctetString(cert.GetEncoded()),
            Org.BouncyCastle.Asn1.GM.GMObjectIdentifiers.sm2sign_with_sm3, new Org.BouncyCastle.Asn1.DerBitString(new byte[] { 1 }));
    }

    private static SigSignature ReadSignature(string ofd, string entry = "Doc_0/Signs/Sign_0/Signature.xml")
    {
        using var zip = ZipFile.OpenRead(ofd);
        var e = zip.Entries.First(x => x.FullName == entry);
        using var s = e.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        ms.Position = 0;
        return new SigSignature(DomParser.Parse(ms));
    }

    [Fact]
    public void Riding_stamp_right_side_splits_by_page_count()
    {
        string plain = SignTestKit.BuildSampleOfd(_root, pages: 3);
        string signed = Sign(plain, new RidingStampPos(Side.Right, 40, 40), "riding.ofd");

        var annots = ReadSignature(signed).GetSignedInfo().GetStampAnnots();
        Assert.Equal(3, annots.Count);

        // 每页均有裁剪区域，且裁剪宽度 = 章宽 / 页数
        double itemWidth = 40d / 3;
        for (int i = 0; i < annots.Count; i++)
        {
            var clip = annots[i].GetClip();
            Assert.NotNull(clip);
            Assert.Equal(itemWidth, clip!.Width, 3);

            var boundary = annots[i].GetBoundary()!;
            Assert.Equal(40, boundary.Width);
            Assert.Equal(40, boundary.Height);
        }

        // 右侧骑缝：x 逐页左移一个裁剪宽度
        var xs = annots.Select(a => a.GetBoundary()!.TopLeftX).ToList();
        for (int i = 1; i < xs.Count; i++)
            Assert.Equal(itemWidth, xs[i - 1] - xs[i], 2); // 序列化保留 3 位小数，容差取 2 位

        // 页码引用递增且无重复
        var pageRefs = annots.Select(a => a.GetPageRef().GetRefId()!.Id).ToList();
        Assert.Equal(pageRefs.Count, pageRefs.Distinct().Count());

        // 签章本身可验证
        using var validator = new OFDValidator(new OFDReader(signed));
        validator.SetValidator(new SESV4ValidateContainer());
        validator.ExeValidate();
    }

    [Fact]
    public void Riding_stamp_with_clip_number_repeats_pattern()
    {
        string plain = SignTestKit.BuildSampleOfd(_root, pages: 5);
        var pos = new RidingStampPos(Side.Right, null, 2, 40, 40, 0);

        string signed = Sign(plain, pos, "riding-clip.ofd");
        var annots = ReadSignature(signed).GetSignedInfo().GetStampAnnots();

        Assert.Equal(5, annots.Count);
        Assert.All(annots, a => Assert.NotNull(a.GetClip()));
    }

    [Fact]
    public void Cutting_ride_stamp_right_side_puts_halves_on_adjacent_pages()
    {
        string plain = SignTestKit.BuildSampleOfd(_root, pages: 3);
        var pos = new CuttingRideStampPos(Side.Right, null, 40, 40, 0, null);

        string signed = Sign(plain, pos, "cutting.ofd");
        var annots = ReadSignature(signed).GetSignedInfo().GetStampAnnots();

        // 首页 1 个 + 中间页 2 个 + 末页 1 个
        Assert.Equal(4, annots.Count);
        var grouped = annots.GroupBy(a => a.GetPageRef().GetRefId()!.Id).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(3, grouped.Count);
        Assert.Contains(grouped.Values, v => v == 2);
    }

    [Fact]
    public void SignCleaner_removes_all_signatures()
    {
        string plain = SignTestKit.BuildSampleOfd(_root, pages: 2);
        string signed = Sign(plain, new NormalStampPos(1, 10, 10, 40, 40), "to-clean.ofd");

        string cleaned = Path.Combine(_root, "cleaned.ofd");
        using (var reader = new OFDReader(signed))
        {
            new SignCleaner(reader, cleaned).Clean();
        }

        // 签名相关文件全部消失
        using var zip = ZipFile.OpenRead(cleaned);
        var names = zip.Entries.Select(e => e.FullName).ToList();
        Assert.DoesNotContain(names, n => n.Contains("Signatures.xml"));
        Assert.DoesNotContain(names, n => n.Contains("Sign_0"));
        Assert.DoesNotContain(names, n => n.Contains("SignedValue.dat"));

        // OFD.xml 不再引用签名列表
        var ofdEntry = zip.Entries.First(e => e.FullName == "OFD.xml");
        using var s = ofdEntry.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        string ofdXml = System.Text.Encoding.UTF8.GetString(ms.ToArray());
        Assert.DoesNotContain("Signatures", ofdXml);
    }
}
