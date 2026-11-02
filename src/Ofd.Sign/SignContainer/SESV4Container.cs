using Ofd.Core.Signatures;
using Ofd.Gm.Ses.V1;
using Ofd.Gm.Sm2Strut;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using SesSignatureV4 = Ofd.Gm.Ses.V4.SES_Signature;
using TbsSignV4 = Ofd.Gm.Ses.V4.TBS_Sign;

namespace Ofd.Sign.SignContainer;

/// <summary>
/// GB/T 38540-2020 电子签章数据生成容器（Java SESV4Container）。
/// <para>注意：该容器仅用于测试，生产电子签章请使用符合国家规范、具有型号证书的设备。</para>
/// </summary>
public class SESV4Container : IExtendSignatureContainer
{
    /// <summary>签名使用的私钥。</summary>
    private readonly ECPrivateKeyParameters _privateKey;

    /// <summary>电子印章。</summary>
    private readonly Ofd.Gm.Ses.V4.SESeal _seal;

    /// <summary>签章使用的证书。</summary>
    private readonly BcX509Certificate _certificate;

    /// <summary>时间戳 hook。</summary>
    private TimeStampHook? _timeStampHook;

    public SESV4Container(ECPrivateKeyParameters privateKey, Ofd.Gm.Ses.V4.SESeal seal, BcX509Certificate signCert)
    {
        _privateKey = privateKey;
        _seal = seal;
        _certificate = signCert;
    }

    public SESV4Container(ECPrivateKeyParameters privateKey, Ofd.Gm.Ses.V4.SESeal seal, BcX509Certificate signCert,
        TimeStampHook timeStampHook) : this(privateKey, seal, signCert)
    {
        _timeStampHook = timeStampHook;
    }

    /// <summary>设置时间戳 hook。</summary>
    public void SetTimeStampHook(TimeStampHook? timeStampHook) => _timeStampHook = timeStampHook;

    /// <summary>摘要算法：SM3。</summary>
    public IDigest GetDigestFnc() => new SM3Digest();

    /// <summary>签名算法：SM3withSM2（1.2.156.10197.1.501）。</summary>
    public DerObjectIdentifier GetSignAlgOID() => GMObjectIdentifiers.sm2sign_with_sm3;

    /// <summary>对待签名数据（Signature.xml 流）生成电子签章数据。</summary>
    public byte[] Sign(Stream inData, string propertyInfo)
    {
        // 签名原文杂凑值：Signature.xml 的 SM3
        var md = GetDigestFnc();
        var buffer = new byte[4096];
        int len;
        while ((len = inData.Read(buffer, 0, buffer.Length)) > 0)
            md.BlockUpdate(buffer, 0, len);

        var dataHash = new byte[md.GetDigestSize()];
        md.DoFinal(dataHash, 0);

        var toSign = new TbsSignV4()
            .SetVersion(SES_Header.V4)
            .SetEseal(_seal)
            .SetTimeInfo(new DerGeneralizedTime(DateTime.Now))
            .SetDataHash(dataHash)
            .SetPropertyInfo(propertyInfo);

        // 对 TBS_Sign 的 DER 编码签名
        byte[] tbs = toSign.GetEncoded();
        var signer = new SM2Signer();
        signer.Init(true, _privateKey);
        signer.BlockUpdate(tbs, 0, tbs.Length);
        byte[] sigVal = signer.GenerateSignature();

        var signature = new SesSignatureV4()
            .SetToSign(toSign)
            .SetCert(_certificate)
            .SetSignatureAlgID(GetSignAlgOID())
            .SetSignature(sigVal);

        if (_timeStampHook != null)
        {
            var timeStamp = _timeStampHook(sigVal);
            if (timeStamp != null) signature.SetTimeStamp(new DerBitString(timeStamp));
        }

        return signature.GetEncoded();
    }

    /// <summary>电子印章二进制编码。</summary>
    public byte[]? GetSeal() => _seal.GetEncoded();

    /// <summary>签章类型。</summary>
    public SigType GetSignType() => SigType.Seal;
}
