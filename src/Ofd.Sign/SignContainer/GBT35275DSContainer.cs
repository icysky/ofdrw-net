using System.Text;
using Ofd.Core.Signatures;
using Ofd.Gm.Sm2Strut;
using Ofd.Gm.Sm2Strut.Builder;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Sign.SignContainer;

/// <summary>
/// GM/T 0099-2020 7.2.2：数字签名且算法为 SM2 时，签名值遵循 GB/T 35275
/// （Java GBT35275DSContainer）。
/// </summary>
public class GBT35275DSContainer : IExtendSignatureContainer
{
    private readonly ECPrivateKeyParameters _prvKey;
    private readonly BcX509Certificate _cert;

    /// <summary>是否对文件摘要值做 Base64 编码（兼容部分阅读器）。</summary>
    private bool _enableFileHashBase64;

    public GBT35275DSContainer(BcX509Certificate cert, ECPrivateKeyParameters prvKey)
    {
        _cert = cert ?? throw new ArgumentException("签名使用证书（cert）不能为空");
        _prvKey = prvKey ?? throw new ArgumentException("签名使用私钥（prvKey）不能为空");
    }

    public IDigest GetDigestFnc() => new SM3Digest();

    public DerObjectIdentifier GetSignAlgOID() => GMObjectIdentifiers.sm2sign_with_sm3;

    public byte[] Sign(Stream inData, string propertyInfo)
    {
        // d) 计算签名文件杂凑值
        byte[] plaintext = Sm2SignUtil.Sm3OfStream(inData);
        if (_enableFileHashBase64)
            plaintext = Encoding.ASCII.GetBytes(Convert.ToBase64String(plaintext));

        // e) 使用私钥对杂凑值签名
        byte[] signature = Sm2SignUtil.Sign(plaintext, _prvKey);

        var signedData = SignedDataBuilder.SignedData(plaintext, signature, _cert);
        return new ContentInfo(OIDs.SignedData, signedData).GetEncoded();
    }

    /// <summary>数字签名不提供印章。</summary>
    public byte[]? GetSeal() => null;

    public SigType GetSignType() => SigType.Sign;

    /// <summary>是否对文件摘要值进行 Base64 编码（Base64 后的内容作为待签名原文）。</summary>
    public void SetEnableFileHashBase64(bool state) => _enableFileHashBase64 = state;
}
