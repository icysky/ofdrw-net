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
/// GM/T 0099-2020 与 PKCS#7 CMS 签名（Java GBT35275PKCS9DSContainer）：
/// 签名对象为 SignerInfo 的 authenticatedAttributes（PKCS#9 键值对）。
/// </summary>
public class GBT35275PKCS9DSContainer : IExtendSignatureContainer
{
    private readonly ECPrivateKeyParameters _prvKey;
    private readonly BcX509Certificate _cert;

    public GBT35275PKCS9DSContainer(BcX509Certificate cert, ECPrivateKeyParameters prvKey)
    {
        _cert = cert ?? throw new ArgumentException("签名使用证书（cert）不能为空");
        _prvKey = prvKey ?? throw new ArgumentException("签名使用私钥（prvKey）不能为空");
    }

    public IDigest GetDigestFnc() => new SM3Digest();

    public DerObjectIdentifier GetSignAlgOID() => GMObjectIdentifiers.sm2sign_with_sm3;

    public byte[] Sign(Stream inData, string propertyInfo)
    {
        byte[] plaintext = Sm2SignUtil.ReadAll(inData);

        var signedData = PKCS9SignedDataBuilder.SignedData(
            plaintext, data => Sm2SignUtil.Sign(data, _prvKey), _cert, null);

        return new ContentInfo(OIDs.SignedData, signedData).GetEncoded();
    }

    /// <summary>数字签名不提供印章。</summary>
    public byte[]? GetSeal() => null;

    public SigType GetSignType() => SigType.Sign;
}
