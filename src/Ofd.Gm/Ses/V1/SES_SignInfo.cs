using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V1;

/// <summary>印章签名信息（Java SES_SignInfo）：制章人证书 + 算法标识 + 签名值。</summary>
public class SES_SignInfo : Asn1Encodable
{
    private Asn1OctetString? _cert;
    private DerObjectIdentifier? _signatureAlgorithm;
    private DerBitString? _signData;

    public SES_SignInfo() { }

    public SES_SignInfo(Asn1OctetString cert, DerObjectIdentifier signatureAlgorithm, DerBitString signData)
    {
        _cert = cert;
        _signatureAlgorithm = signatureAlgorithm;
        _signData = signData;
    }

    public SES_SignInfo(Asn1Sequence seq)
    {
        _cert = Asn1OctetString.GetInstance(seq[0]);
        _signatureAlgorithm = DerObjectIdentifier.GetInstance(seq[1]);
        _signData = DerBitString.GetInstance(seq[2]);
    }

    public static SES_SignInfo? GetInstance(object? o)
    {
        switch (o)
        {
            case null:
                return null;
            case SES_SignInfo info:
                return info;
            case DerBitString bits:
                return new SES_SignInfo(Asn1Sequence.GetInstance(bits.GetBytes()));
            default:
                return new SES_SignInfo(Asn1Sequence.GetInstance(o));
        }
    }

    public Asn1OctetString? GetCert() => _cert;

    public SES_SignInfo SetCert(Asn1OctetString cert)
    {
        _cert = cert;
        return this;
    }

    public DerObjectIdentifier? GetSignatureAlgorithm() => _signatureAlgorithm;

    public SES_SignInfo SetSignatureAlgorithm(DerObjectIdentifier signatureAlgorithm)
    {
        _signatureAlgorithm = signatureAlgorithm;
        return this;
    }

    public DerBitString? GetSignData() => _signData;

    public SES_SignInfo SetSignData(DerBitString signData)
    {
        _signData = signData;
        return this;
    }

    public SES_SignInfo SetSignData(byte[] signData)
    {
        _signData = new DerBitString(signData);
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(3);
        if (_cert != null) v.Add(_cert);
        if (_signatureAlgorithm != null) v.Add(_signatureAlgorithm);
        if (_signData != null) v.Add(_signData);
        return new DerSequence(v);
    }
}
