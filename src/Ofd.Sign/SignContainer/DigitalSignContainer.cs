using Ofd.Core.Signatures;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;

namespace Ofd.Sign.SignContainer;

/// <summary>
/// 国密 SM2withSM3 数字签名实现容器（Java DigitalSignContainer）。
/// </summary>
[Obsolete("OFD 的数字签名应符合 GB/T 35275，请使用 GBT35275DSContainer")]
public class DigitalSignContainer : IExtendSignatureContainer
{
    /// <summary>签名私钥。</summary>
    private readonly ECPrivateKeyParameters _prvKey;

    public DigitalSignContainer(ECPrivateKeyParameters prvKey)
    {
        _prvKey = prvKey ?? throw new ArgumentException("签名使用私钥（prvKey）不能为空");
    }

    public IDigest GetDigestFnc() => new SM3Digest();

    public DerObjectIdentifier GetSignAlgOID() => GMObjectIdentifiers.sm2sign_with_sm3;

    /// <summary>对整段数据直接签名（非摘要）。</summary>
    public byte[] Sign(Stream inData, string propertyInfo) =>
        Sm2SignUtil.Sign(Sm2SignUtil.ReadAll(inData), _prvKey);

    /// <summary>电子签名不提供印章。</summary>
    public byte[]? GetSeal() => null;

    public SigType GetSignType() => SigType.Sign;
}
