using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace Ofd.Gm.Sm2Strut;

/// <summary>
/// GB/T 35275-2017 6.7 IssuerAndSerialNumber：证书颁发者可识别名 + 序列号。
/// </summary>
public class IssuerAndSerialNumber : Asn1Encodable
{
    private X509Name? _name;
    private DerInteger? _certSerialNumber;

    public IssuerAndSerialNumber() { }

    public IssuerAndSerialNumber(X509Name name, DerInteger certSerialNumber)
    {
        _name = name;
        _certSerialNumber = certSerialNumber;
    }

    public IssuerAndSerialNumber(Asn1Sequence seq)
    {
        _name = X509Name.GetInstance(seq[0]);
        _certSerialNumber = DerInteger.GetInstance(seq[1]);
    }

    public static IssuerAndSerialNumber? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as IssuerAndSerialNumber ?? new IssuerAndSerialNumber(Asn1Sequence.GetInstance(o));
    }

    public X509Name? GetName() => _name;

    public IssuerAndSerialNumber SetName(X509Name name)
    {
        _name = name;
        return this;
    }

    public DerInteger? GetCertSerialNumber() => _certSerialNumber;

    public IssuerAndSerialNumber SetCertSerialNumber(DerInteger certSerialNumber)
    {
        _certSerialNumber = certSerialNumber;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(2);
        if (_name != null) v.Add(_name);
        if (_certSerialNumber != null) v.Add(_certSerialNumber);
        return new DerSequence(v);
    }
}
