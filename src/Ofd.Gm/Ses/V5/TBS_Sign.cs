using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>签章信息（Java v5 TBS_Sign）：version/eseal/timeInfo/dataHash/propertyInfo + [0] extDatas。</summary>
public class TBS_Sign : Asn1Encodable
{
    private DerInteger? _version;
    private SESeal? _eseal;
    private Asn1GeneralizedTime? _timeInfo;
    private DerBitString? _dataHash;
    private DerIA5String? _propertyInfo;
    private ExtensionDatas? _extDatas;

    public TBS_Sign() { }

    public TBS_Sign(DerInteger version, SESeal eseal, Asn1GeneralizedTime timeInfo,
        DerBitString dataHash, DerIA5String propertyInfo, ExtensionDatas? extDatas)
    {
        _version = version;
        _eseal = eseal;
        _timeInfo = timeInfo;
        _dataHash = dataHash;
        _propertyInfo = propertyInfo;
        _extDatas = extDatas;
    }

    public TBS_Sign(Asn1Sequence seq)
    {
        _version = DerInteger.GetInstance(seq[0]);
        _eseal = SESeal.GetInstance(seq[1]);
        _timeInfo = Asn1GeneralizedTime.GetInstance(seq[2]);
        _dataHash = DerBitString.GetInstance(seq[3]);
        _propertyInfo = DerIA5String.GetInstance(seq[4]);
        if (seq.Count > 5 && seq[5] is Asn1TaggedObject tagged)
            _extDatas = ExtensionDatas.GetInstance(tagged.GetExplicitBaseObject());
    }

    public static TBS_Sign? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as TBS_Sign ?? new TBS_Sign(Asn1Sequence.GetInstance(o));
    }

    public DerInteger? GetVersion() => _version;

    public TBS_Sign SetVersion(DerInteger version)
    {
        _version = version;
        return this;
    }

    public SESeal? GetEseal() => _eseal;

    public TBS_Sign SetEseal(SESeal eseal)
    {
        _eseal = eseal;
        return this;
    }

    public Asn1GeneralizedTime? GetTimeInfo() => _timeInfo;

    public TBS_Sign SetTimeInfo(Asn1GeneralizedTime timeInfo)
    {
        _timeInfo = timeInfo;
        return this;
    }

    public DerBitString? GetDataHash() => _dataHash;

    public TBS_Sign SetDataHash(DerBitString dataHash)
    {
        _dataHash = dataHash;
        return this;
    }

    public TBS_Sign SetDataHash(byte[] dataHash)
    {
        _dataHash = new DerBitString(dataHash);
        return this;
    }

    public DerIA5String? GetPropertyInfo() => _propertyInfo;

    public TBS_Sign SetPropertyInfo(DerIA5String propertyInfo)
    {
        _propertyInfo = propertyInfo;
        return this;
    }

    public TBS_Sign SetPropertyInfo(string propertyInfo)
    {
        _propertyInfo = new DerIA5String(propertyInfo);
        return this;
    }

    public ExtensionDatas? GetExtDatas() => _extDatas;

    public TBS_Sign SetExtDatas(ExtensionDatas extDatas)
    {
        _extDatas = extDatas;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(6);
        if (_version != null) v.Add(_version);
        if (_eseal != null) v.Add(_eseal);
        if (_timeInfo != null) v.Add(_timeInfo);
        if (_dataHash != null) v.Add(_dataHash);
        if (_propertyInfo != null) v.Add(_propertyInfo);
        if (_extDatas != null) v.Add(new DerTaggedObject(true, 0, _extDatas));
        return new DerSequence(v);
    }
}
