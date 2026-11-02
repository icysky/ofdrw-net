using Ofd.Archive.Check;
using Ofd.Archive.Check.Rule;
using Ofd.Archive.Convert;
using Ofd.Reader;
using Xunit;

namespace Ofd.Tests.Integration;

/// <summary>
/// 真实样本（ofdrw 测试资源）转换为 OFD-A 的健壮性：
/// 不追求逐字节一致，只验证「能转、转完能读、页数不变、DocType 合规」。
/// </summary>
public class RealSampleConversionTests : IDisposable
{
    private readonly string _root;

    public RealSampleConversionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_real_" + Guid.NewGuid().ToString("N")[..8]);
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

    public static TheoryData<string> Samples => new()
    {
        "real-helloworld.ofd",
        "real-namespace_no_std.ofd",
        "real-signedFile.ofd",
        "real-allprotected.ofd",
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public void Real_sample_converts_and_stays_readable(string sample)
    {
        string src = Path.Combine(AppContext.BaseDirectory, "TestData", sample);
        Assert.True(File.Exists(src), "缺少真实样本: " + src);

        int srcPages;
        using (var reader = new OFDReader(src)) srcPages = reader.GetNumberOfPages();
        Assert.True(srcPages > 0, "源文档应有页面");

        string dst = Path.Combine(_root, "out-" + sample);
        new OFDArchiveConverter().Convert(src, dst);

        Assert.True(File.Exists(dst));

        using (var reader = new OFDReader(dst))
        {
            Assert.Equal(srcPages, reader.GetNumberOfPages());
            Assert.Equal("OFD-A", reader.GetOFDDir().GetOfd().GetDocType());
        }

        // 转换后不应再有 DOC_TYPE 违规；签名类违规也应消失（去技术化）
        var violations = new OFDArchiveChecker().Check(dst);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == DocTypeRule.RULE_NAME);
        Assert.DoesNotContain(violations, v => v.GetRuleName() == SignatureRule.RULE_NAME);
    }

    [Fact]
    public void Sealed_real_sample_loses_signature_on_conversion()
    {
        string src = Path.Combine(AppContext.BaseDirectory, "TestData", "V4RideRight.ofd");
        Assert.True(File.Exists(src), "缺少真实签章样本");

        using (var reader = new OFDReader(src))
            Assert.NotNull(reader.GetDefaultSignatures());

        string dst = Path.Combine(_root, "sealed-ofda.ofd");
        new OFDArchiveConverter().Convert(src, dst);

        using (var reader = new OFDReader(dst))
        {
            var signatures = reader.GetDefaultSignatures();
            Assert.True(signatures == null || signatures.GetSignatures().Count == 0, "签章应被移除");
            Assert.Equal("OFD-A", reader.GetOFDDir().GetOfd().GetDocType());
        }
    }
}
