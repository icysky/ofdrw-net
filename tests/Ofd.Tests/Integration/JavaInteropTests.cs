using System.IO.Compression;
using Ofd.Reader;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Xunit;

namespace Ofd.Tests.Integration;

/// <summary>
/// 跨实现互操作：Java（ofdrw）签名/签章的产物，由 .NET 实现验签。
/// <para>
/// 夹具由 <c>tools/javaprobe/Probe.java</c> 生成后复制到 TestData：
/// <list type="bullet">
/// <item><c>java-signed.ofd</c>：GB/T 35275 数字签名（非签章）</item>
/// <item><c>java-seal.ofd</c>：SES V4 电子签章</item>
/// </list>
/// </para>
/// </summary>
public class JavaInteropTests
{
    private static string Fixture(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", name);
        Assert.True(File.Exists(path), "缺少互操作夹具: " + path);
        return path;
    }

    [Fact]
    public void Java_gbt35275_signed_document_validates_in_csharp()
    {
        string ofd = Fixture("java-signed.ofd");

        using var reader = new OFDReader(ofd);

        // 签名列表存在且类型为 Sign
        var signatures = reader.GetDefaultSignatures();
        Assert.NotNull(signatures);

        var sig = Assert.Single(signatures!.GetSignatures());
        Assert.Equal("Sign", sig.GetType().ToString());

        // 验签通过（失败会抛异常）
        using var validator = new OFDValidator(reader);
        validator.SetValidator(new GBT35275ValidateContainer());
        validator.ExeValidate();
    }

    [Fact]
    public void Java_ses_v4_seal_document_validates_in_csharp()
    {
        string ofd = Fixture("java-seal.ofd");

        using (var zip = ZipFile.OpenRead(ofd))
        {
            Assert.Contains(zip.Entries, e => e.FullName == "Doc_0/Signs/Sign_0/Seal.esl");
        }

        using var reader = new OFDReader(ofd);

        var signatures = reader.GetDefaultSignatures();
        Assert.NotNull(signatures);

        var sig = Assert.Single(signatures!.GetSignatures());
        Assert.Equal("Seal", sig.GetType().ToString());

        using var validator = new OFDValidator(reader);
        validator.SetValidator(new SESV4ValidateContainer());
        validator.ExeValidate();
    }

    [Fact]
    public void Wrong_container_rejects_java_products()
    {
        // 纯数字签名不能被「签章」验签器接受
        using (var reader = new OFDReader(Fixture("java-signed.ofd")))
        using (var validator = new OFDValidator(reader))
        {
            validator.SetValidator(new SESV4ValidateContainer());
            Assert.ThrowsAny<Exception>(() => validator.ExeValidate());
        }

        // 签章也不能被 GB/T 35275 验签器接受
        using (var reader = new OFDReader(Fixture("java-seal.ofd")))
        using (var validator = new OFDValidator(reader))
        {
            validator.SetValidator(new GBT35275ValidateContainer());
            Assert.ThrowsAny<Exception>(() => validator.ExeValidate());
        }
    }
}
