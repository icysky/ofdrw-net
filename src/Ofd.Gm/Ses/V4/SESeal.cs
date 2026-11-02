using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V4;

/// <summary>电子印章数据（Java SESeal）：印章信息 + 制章人证书 + 算法标识 + 签名值。</summary>
public class SESeal : Asn1Encodable
{
    private SES_SealInfo? _eSealInfo;
    private Asn1OctetString? _cert;
    private DerObjectIdentifier? _signAlgID;
    private DerBitString? _signedValue;

    public SESeal() { }

    public SESeal(SES_SealInfo eSealInfo, Asn1OctetString cert, DerObjectIdentifier signAlgID, DerBitString signedValue)
    {
        _eSealInfo = eSealInfo;
        _cert = cert;
        _signAlgID = signAlgID;
        _signedValue = signedValue;
    }

    public SESeal(Asn1Sequence seq)
    {
        _eSealInfo = SES_SealInfo.GetInstance(seq[0]);
        _cert = Asn1OctetString.GetInstance(seq[1]);
        _signAlgID = DerObjectIdentifier.GetInstance(seq[2]);
        _signedValue = DerBitString.GetInstance(seq[3]);
    }

    public static SESeal? GetInstance(object? o)
    {
        switch (o)
        {
            case null:
                return null;
            case SESeal seal:
                return seal;
            case byte[] bytes:
                try
                {
                    return new SESeal(Asn1Sequence.GetInstance(Asn1Object.FromByteArray(bytes)));
                }
                catch (Exception e)
                {
                    throw new ArgumentException("电子印章数据v4 无法解析", e);
                }
            default:
                return new SESeal(Asn1Sequence.GetInstance(o));
        }
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

    /// <summary>设置制章人证书（使用证书 DER 编码）。</summary>
    public SESeal SetCert(BcX509Certificate cert)
    {
        _cert = new DerOctetString(cert.GetEncoded());
        return this;
    }

    public DerObjectIdentifier? GetSignAlgID() => _signAlgID;

    public SESeal SetSignAlgID(DerObjectIdentifier signAlgID)
    {
        _signAlgID = signAlgID;
        return this;
    }

    public DerBitString? GetSignedValue() => _signedValue;

    public SESeal SetSignedValue(DerBitString signedValue)
    {
        _signedValue = signedValue;
        return this;
    }

    public SESeal SetSignedValue(byte[] signedValue)
    {
        _signedValue = new DerBitString(signedValue);
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(4);
        if (_eSealInfo != null) v.Add(_eSealInfo);
        if (_cert != null) v.Add(_cert);
        if (_signAlgID != null) v.Add(_signAlgID);
        if (_signedValue != null) v.Add(_signedValue);
        return new DerSequence(v);
    }
}
