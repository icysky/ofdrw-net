using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V4;

/// <summary>电子签章数据（Java SES_Signature）：签章信息 + 签章者证书 + 算法标识 + 签名值（+ 时间戳）。</summary>
public class SES_Signature : Asn1Encodable
{
    private TBS_Sign? _toSign;
    private Asn1OctetString? _cert;
    private DerObjectIdentifier? _signatureAlgID;
    private DerBitString? _signature;
    private DerBitString? _timeStamp;

    public SES_Signature() { }

    public SES_Signature(TBS_Sign toSign, Asn1OctetString cert, DerObjectIdentifier signatureAlgID,
        DerBitString signature, DerBitString? timeStamp)
    {
        _toSign = toSign;
        _cert = cert;
        _signatureAlgID = signatureAlgID;
        _signature = signature;
        _timeStamp = timeStamp;
    }

    public SES_Signature(Asn1Sequence seq)
    {
        _toSign = TBS_Sign.GetInstance(seq[0]);
        _cert = Asn1OctetString.GetInstance(seq[1]);
        _signatureAlgID = DerObjectIdentifier.GetInstance(seq[2]);
        _signature = DerBitString.GetInstance(seq[3]);
        if (seq.Count > 4 && seq[4] is Asn1TaggedObject tagged)
            _timeStamp = DerBitString.GetInstance(tagged.GetExplicitBaseObject());
    }

    public static SES_Signature? GetInstance(object? o)
    {
        switch (o)
        {
            case null:
                return null;
            case SES_Signature sig:
                return sig;
            case byte[] bytes:
                try
                {
                    return new SES_Signature(Asn1Sequence.GetInstance(Asn1Object.FromByteArray(bytes)));
                }
                catch (Exception e)
                {
                    throw new ArgumentException("电子签章数据v4 无法解析", e);
                }
            default:
                return new SES_Signature(Asn1Sequence.GetInstance(o));
        }
    }

    public TBS_Sign? GetToSign() => _toSign;

    public SES_Signature SetToSign(TBS_Sign toSign)
    {
        _toSign = toSign;
        return this;
    }

    public Asn1OctetString? GetCert() => _cert;

    public SES_Signature SetCert(Asn1OctetString cert)
    {
        _cert = cert;
        return this;
    }

    /// <summary>设置签章者证书（使用证书 DER 编码）。</summary>
    public SES_Signature SetCert(BcX509Certificate cert)
    {
        _cert = new DerOctetString(cert.GetEncoded());
        return this;
    }

    public DerObjectIdentifier? GetSignatureAlgID() => _signatureAlgID;

    public SES_Signature SetSignatureAlgID(DerObjectIdentifier signatureAlgID)
    {
        _signatureAlgID = signatureAlgID;
        return this;
    }

    public DerBitString? GetSignature() => _signature;

    public SES_Signature SetSignature(DerBitString signature)
    {
        _signature = signature;
        return this;
    }

    public SES_Signature SetSignature(byte[] signature)
    {
        _signature = new DerBitString(signature);
        return this;
    }

    public DerBitString? GetTimeStamp() => _timeStamp;

    public SES_Signature SetTimeStamp(DerBitString timeStamp)
    {
        _timeStamp = timeStamp;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(5);
        if (_toSign != null) v.Add(_toSign);
        if (_cert != null) v.Add(_cert);
        if (_signatureAlgID != null) v.Add(_signatureAlgID);
        if (_signature != null) v.Add(_signature);
        if (_timeStamp != null) v.Add(new DerTaggedObject(true, 0, _timeStamp));
        return new DerSequence(v);
    }
}
