using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V1;

/// <summary>厂商自定义数据（Java ExtData）。</summary>
public class ExtData : Asn1Encodable
{
    private DerObjectIdentifier? _extnID;
    private DerBoolean _critical = DerBoolean.False;
    private Asn1OctetString? _extnValue;

    public ExtData() { }

    public ExtData(DerObjectIdentifier extnID, DerBoolean critical, Asn1OctetString extnValue)
    {
        _extnID = extnID;
        _critical = critical;
        _extnValue = extnValue;
    }

    public ExtData(Asn1Sequence seq)
    {
        _extnID = DerObjectIdentifier.GetInstance(seq[0]);
        _critical = DerBoolean.GetInstance(seq[1]);
        _extnValue = Asn1OctetString.GetInstance(seq[2]);
    }

    public static ExtData? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as ExtData ?? new ExtData(Asn1Sequence.GetInstance(o));
    }

    public DerObjectIdentifier? GetExtnID() => _extnID;

    public ExtData SetExtnID(DerObjectIdentifier extnID)
    {
        _extnID = extnID;
        return this;
    }

    public DerBoolean GetCritical() => _critical;

    public ExtData SetCritical(DerBoolean critical)
    {
        _critical = critical;
        return this;
    }

    public Asn1OctetString? GetExtnValue() => _extnValue;

    public ExtData SetExtnValue(Asn1OctetString extnValue)
    {
        _extnValue = extnValue;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(3);
        if (_extnID != null) v.Add(_extnID);
        v.Add(_critical);
        if (_extnValue != null) v.Add(_extnValue);
        return new DerSequence(v);
    }
}
