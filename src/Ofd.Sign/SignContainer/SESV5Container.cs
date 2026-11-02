using Ofd.Core.Signatures;
using Ofd.Gm.Ses.V5;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using SesSignatureV5 = Ofd.Gm.Ses.V5.SES_Signature;
using TbsSignV5 = Ofd.Gm.Ses.V5.TBS_Sign;

namespace Ofd.Sign.SignContainer;

/// <summary>
/// V5 电子签章数据生成容器（Java SESV5Container）：签名范围 = Sign(TBS_Sign)。
/// </summary>
public class SESV5Container : IExtendSignatureContainer
{
    private readonly ECPrivateKeyParameters _privateKey;
    private readonly Ofd.Gm.Ses.V5.SESeal _seal;
    private readonly BcX509Certificate _certificate;
    private TimeStampHook? _timeStampHook;

    public SESV5Container(ECPrivateKeyParameters privateKey, Ofd.Gm.Ses.V5.SESeal seal, BcX509Certificate signCert)
    {
        _privateKey = privateKey;
        _seal = seal;
        _certificate = signCert;
    }

    public SESV5Container(ECPrivateKeyParameters privateKey, Ofd.Gm.Ses.V5.SESeal seal, BcX509Certificate signCert,
        TimeStampHook timeStampHook) : this(privateKey, seal, signCert)
    {
        _timeStampHook = timeStampHook;
    }

    public void SetTimeStampHook(TimeStampHook? timeStampHook) => _timeStampHook = timeStampHook;

    public IDigest GetDigestFnc() => new SM3Digest();

    public DerObjectIdentifier GetSignAlgOID() => GMObjectIdentifiers.sm2sign_with_sm3;

    public byte[] Sign(Stream inData, string propertyInfo)
    {
        byte[] dataHash = Sm2SignUtil.Sm3OfStream(inData);

        var toSign = new TbsSignV5()
            .SetVersion(SES_Header.V5)
            .SetEseal(_seal)
            .SetTimeInfo(new DerGeneralizedTime(DateTime.Now))
            .SetDataHash(dataHash)
            .SetPropertyInfo(propertyInfo);

        byte[] sigVal = Sm2SignUtil.Sign(toSign.GetEncoded(), _privateKey);

        var signature = new SesSignatureV5()
            .SetToSign(toSign)
            .SetCert(_certificate)
            .SetSignatureAlgID(GMObjectIdentifiers.sm2sign_with_sm3)
            .SetSignatureValue(sigVal);

        if (_timeStampHook != null)
        {
            var timeStamp = _timeStampHook(sigVal);
            if (timeStamp != null) signature.SetTimeStamp(new DerBitString(timeStamp));
        }

        return signature.GetEncoded();
    }

    public byte[]? GetSeal() => _seal.GetEncoded();

    public SigType GetSignType() => SigType.Seal;
}
