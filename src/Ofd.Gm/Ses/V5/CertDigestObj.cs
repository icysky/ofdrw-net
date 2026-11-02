using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>签章者证书杂凑值（Java v5 CertDigestObj）。</summary>
public class CertDigestObj : Asn1Encodable
{
    private DerPrintableString? _type;
    private Asn1OctetString? _value;

    public CertDigestObj() { }

    public CertDigestObj(DerPrintableString type, Asn1OctetString value)
    {
        _type = type;
        _value = value;
    }

    public CertDigestObj(Asn1Sequence seq)
    {
        _type = DerPrintableString.GetInstance(seq[0]);
        _value = Asn1OctetString.GetInstance(seq[1]);
    }

    public static CertDigestObj? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as CertDigestObj ?? new CertDigestObj(Asn1Sequence.GetInstance(o));
    }

    public DerPrintableString? GetType() => _type;

    public CertDigestObj SetType(DerPrintableString type)
    {
        _type = type;
        return this;
    }

    public CertDigestObj SetType(string type)
    {
        _type = new DerPrintableString(type);
        return this;
    }

    public Asn1OctetString? GetValue() => _value;

    public CertDigestObj SetValue(Asn1OctetString value)
    {
        _value = value;
        return this;
    }

    public CertDigestObj SetValue(byte[] value)
    {
        _value = new DerOctetString(value);
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(2);
        if (_type != null) v.Add(_type);
        if (_value != null) v.Add(_value);
        return new DerSequence(v);
    }
}
