using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>
/// 电子印章数据（Java v5 SESeal）：五字段平铺
/// （印章信息 + 制章者证书 + 签名算法标识 + 签名值 + [0] 时间戳可选）。
/// </summary>
public class SESeal : Asn1Encodable
{
    private SES_SealInfo? _eSealInfo;
    private Asn1OctetString? _cert;
    private DerObjectIdentifier? _signatureAlgID;
    private DerBitString? _signatureValue;
    private DerBitString? _timeStamp;

    public SESeal() { }

    public SESeal(SES_SealInfo eSealInfo, Asn1OctetString cert, DerObjectIdentifier signatureAlgID,
        DerBitString signatureValue, DerBitString? timeStamp)
    {
        _eSealInfo = eSealInfo;
        _cert = cert;
        _signatureAlgID = signatureAlgID;
        _signatureValue = signatureValue;
        _timeStamp = timeStamp;
    }

    public SESeal(Asn1Sequence seq)
    {
        _eSealInfo = SES_SealInfo.GetInstance(seq[0]);
        _cert = Asn1OctetString.GetInstance(seq[1]);
        _signatureAlgID = DerObjectIdentifier.GetInstance(seq[2]);
        _signatureValue = DerBitString.GetInstance(seq[3]);
        if (seq.Count > 4 && seq[4] is Asn1TaggedObject tagged)
            _timeStamp = DerBitString.GetInstance(tagged.GetExplicitBaseObject());
    }

    public static SESeal? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SESeal ?? new SESeal(Asn1Sequence.GetInstance(o));
    }

    public SES_SealInfo? GeteSealInfo() => _eSealInfo;

    public SESeal SeteSealInfo(SES_SealInfo eSealInfo)
    {
        _eSealInfo = eSealInfo;
        return this;
    }

    public Asn1OctetString? GetCert() => _cert;

    public SESeal SetCert(Asn1OctetString cert)
    {
        _cert = cert;
        return this;
    }

    /// <summary>设置制章者证书（使用证书 DER 编码）。</summary>
    public SESeal SetCert(BcX509Certificate cert)
    {
        _cert = new DerOctetString(cert.GetEncoded());
        return this;
    }

    public DerObjectIdentifier? GetSignatureAlgID() => _signatureAlgID;

    public SESeal SetSignatureAlgID(DerObjectIdentifier signatureAlgID)
    {
        _signatureAlgID = signatureAlgID;
        return this;
    }

    public DerBitString? GetSignatureValue() => _signatureValue;

    public SESeal SetSignatureValue(DerBitString signatureValue)
    {
        _signatureValue = signatureValue;
        return this;
    }

    public SESeal SetSignatureValue(byte[] signatureValue)
    {
        _signatureValue = new DerBitString(signatureValue);
        return this;
    }

    public DerBitString? GetTimeStamp() => _timeStamp;

    public SESeal SetTimeStamp(DerBitString timeStamp)
    {
        _timeStamp = timeStamp;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(5);
        if (_eSealInfo != null) v.Add(_eSealInfo);
        if (_cert != null) v.Add(_cert);
        if (_signatureAlgID != null) v.Add(_signatureAlgID);
        if (_signatureValue != null) v.Add(_signatureValue);
        if (_timeStamp != null) v.Add(new DerTaggedObject(true, 0, _timeStamp));
        return new DerSequence(v);
    }
}
