using System.IO.Compression;
using Ofd.Reader;
using Ofd.Sign;
using Ofd.Sign.SignContainer;
using Ofd.Sign.Verify;
using Ofd.Sign.Verify.Container;
using Ofd.Sign.Verify.Exceptions;
using Xunit;

namespace Ofd.Tests.Sign;

/// <summary>GB/T 35275 签名值作为 SignedValue.dat：签名 → 验签（普通 / Base64 / PKCS#9）。</summary>
public class Gbt35275SignTests : IDisposable
{
    private readonly string _root;

    public Gbt35275SignTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "ofdnet_35275_" + Guid.NewGuid().ToString("N")[..8]);
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

    private string SignWith(string plain, IExtendSignatureContainer container, string name)
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

    [Fact]
    public void Gbt35275_signed_data_container_signs_and_validates()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert();

        string signed = SignWith(plain, new GBT35275DSContainer(cert, priv), "s35275.ofd");

        // 签名值为 ContentInfo(signedData) 结构，且无 Seal.esl（纯数字签名）
        var entries = ReadEntries(signed);
        Assert.Contains("Doc_0/Signs/Sign_0/SignedValue.dat", entries.Keys);
        Assert.DoesNotContain("Doc_0/Signs/Sign_0/Seal.esl", entries.Keys);

        byte[] signedValue = entries["Doc_0/Signs/Sign_0/SignedValue.dat"];
        var contentInfo = Ofd.Gm.Sm2Strut.ContentInfo.GetInstance(signedValue)!;
        Assert.Equal(Ofd.Gm.Sm2Strut.OIDs.SignedData, contentInfo.GetContentType());

        Validate(signed, new GBT35275ValidateContainer());
    }

    [Fact]
    public void Gbt35275_base64_file_hash_variant_validates()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(52);

        var container = new GBT35275DSContainer(cert, priv);
        container.SetEnableFileHashBase64(true);

        string signed = SignWith(plain, container, "s35275-b64.ofd");
        Validate(signed, new GBT35275ValidateContainer()); // 验证器含 Base64 兼容分支
    }

    [Fact]
    public void Pkcs9_signed_data_container_signs_and_validates()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(53);

        string signed = SignWith(plain, new GBT35275PKCS9DSContainer(cert, priv), "pkcs9.ofd");
        Validate(signed, new GBT35275ValidateContainer());
    }

    [Fact]
    public void Tampered_signature_value_fails_validation()
    {
        string plain = SignTestKit.BuildSampleOfd(_root);
        var (priv, cert) = SignTestKit.NewSm2Cert(54);

        string signed = SignWith(plain, new GBT35275DSContainer(cert, priv), "s35275-t.ofd");

        // 解压 → 改动签名值 → 重新打包
        string dir = Path.Combine(_root, "tamper");
        ZipFile.ExtractToDirectory(signed, dir);
        string sv = Path.Combine(dir, "Doc_0", "Signs", "Sign_0", "SignedValue.dat");
        byte[] data = File.ReadAllBytes(sv);
        data[^1] ^= 0xFF;
        File.WriteAllBytes(sv, data);

        string tampered = Path.Combine(_root, "tampered.ofd");
        ZipFile.CreateFromDirectory(dir, tampered);

        Assert.ThrowsAny<OFDVerifyException>(() => Validate(tampered, new GBT35275ValidateContainer()));
    }

    [Fact]
    public void Validate_container_rejects_seal_type()
    {
        var container = new GBT35275ValidateContainer();
        Assert.Throws<ArgumentException>(() =>
            container.Validate(Ofd.Core.Signatures.SigType.Seal, "1.2.156.10197.1.501", new byte[] { 1 }, new byte[] { 2 }));
    }
}
