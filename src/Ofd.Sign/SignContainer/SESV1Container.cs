using System.Text;
using Ofd.Core.Signatures;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using SesSignatureV1 = Ofd.Gm.Ses.V1.SES_Signature;
using TbsSignV1 = Ofd.Gm.Ses.V1.TBS_Sign;

namespace Ofd.Sign.SignContainer;

/// <summary>
/// GM/T 0031-2014 电子签章数据生成容器（Java SESV1Container）。
/// <para>注意：仅用于测试，生产签章请使用符合国家标准、具有型号证书的设备。</para>
/// </summary>
public class SESV1Container : IExtendSignatureContainer
{
    private readonly ECPrivateKeyParameters _privateKey;
    private readonly Ofd.Gm.Ses.V1.SESeal _seal;
    private readonly BcX509Certificate _certificate;

    public SESV1Container(ECPrivateKeyParameters privateKey, Ofd.Gm.Ses.V1.SESeal seal, BcX509Certificate signCert)
    {
        _privateKey = privateKey;
        _seal = seal;
        _certificate = signCert;
    }

    public IDigest GetDigestFnc() => new SM3Digest();

    public DerObjectIdentifier GetSignAlgOID() => GMObjectIdentifiers.sm2sign_with_sm3;

    public byte[] Sign(Stream inData, string propertyInfo)
    {
        // Signature.xml 的 SM3
        byte[] digest = Sm2SignUtil.Sm3OfStream(inData);

        // v1 的时间信息为比特串（文本时间）
        byte[] signUtcTime = Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        var tbsSign = new TbsSignV1()
            .SetVersion(new DerInteger(1))
            .SetEseal(_seal)
            .SetTimeInfo(new DerBitString(signUtcTime))
            .SetDataHash(new DerBitString(digest))
            .SetPropertyInfo(new DerIA5String(propertyInfo))
            .SetCert(new DerOctetString(_certificate.GetEncoded()))
            .SetSignatureAlgorithm(GMObjectIdentifiers.sm2sign_with_sm3);

        byte[] sign = Sm2SignUtil.Sign(tbsSign.GetEncoded(), _privateKey);
        return new SesSignatureV1(tbsSign, new DerBitString(sign)).GetEncoded();
    }

    public byte[]? GetSeal() => _seal.GetEncoded();

    public SigType GetSignType() => SigType.Seal;
}
