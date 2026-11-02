using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V1;

/// <summary>待电子签章数据（Java v1 TBS_Sign）：版本/电子印章/签章时间/杂凑值/属性/证书/算法标识。</summary>
public class TBS_Sign : Asn1Encodable
{
    private DerInteger? _version;
    private SESeal? _eseal;
    private DerBitString? _timeInfo;
    private DerBitString? _dataHash;
    private DerIA5String? _propertyInfo;
    private Asn1OctetString? _cert;
    private DerObjectIdentifier? _signatureAlgorithm;

    public TBS_Sign() { }

    public TBS_Sign(DerInteger version, SESeal eseal, DerBitString timeInfo, DerBitString dataHash,
        DerIA5String propertyInfo, Asn1OctetString cert, DerObjectIdentifier signatureAlgorithm)
    {
        _version = version;
        _eseal = eseal;
        _timeInfo = timeInfo;
        _dataHash = dataHash;
        _propertyInfo = propertyInfo;
        _cert = cert;
        _signatureAlgorithm = signatureAlgorithm;
    }

    public TBS_Sign(Asn1Sequence seq)
    {
        _version = DerInteger.GetInstance(seq[0]);
        _eseal = SESeal.GetInstance(seq[1]);

        // 兼容非标签章：时间信息可能为 OCTET STRING
        _timeInfo = ToBitString(seq[2]);
        _dataHash = ToBitString(seq[3]);

        _propertyInfo = new DerIA5String(DerIA5String.GetInstance(seq[4]).GetString());
        _cert = Asn1OctetString.GetInstance(seq[5]);
        _signatureAlgorithm = DerObjectIdentifier.GetInstance(seq[6]);
    }

    /// <summary>将 OCTET STRING 视为 BIT STRING（内容相同），否则按 BIT STRING 解析。</summary>
    private static DerBitString ToBitString(object o) =>
        o is Asn1OctetString octets
            ? new DerBitString(octets.GetOctets())
            : DerBitString.GetInstance(o);

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

    public DerBitString? GetTimeInfo() => _timeInfo;

    public TBS_Sign SetTimeInfo(DerBitString timeInfo)
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

    public DerIA5String? GetPropertyInfo() => _propertyInfo;

    public TBS_Sign SetPropertyInfo(DerIA5String propertyInfo)
    {
        _propertyInfo = propertyInfo;
        return this;
    }

    public Asn1OctetString? GetCert() => _cert;

    public TBS_Sign SetCert(Asn1OctetString cert)
    {
        _cert = cert;
        return this;
    }

    public DerObjectIdentifier? GetSignatureAlgorithm() => _signatureAlgorithm;

    public TBS_Sign SetSignatureAlgorithm(DerObjectIdentifier signatureAlgorithm)
    {
        _signatureAlgorithm = signatureAlgorithm;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(7);
        if (_version != null) v.Add(_version);
        if (_eseal != null) v.Add(_eseal);
        if (_timeInfo != null) v.Add(_timeInfo);
        if (_dataHash != null) v.Add(_dataHash);
        if (_propertyInfo != null) v.Add(_propertyInfo);
        if (_cert != null) v.Add(_cert);
        if (_signatureAlgorithm != null) v.Add(_signatureAlgorithm);
        return new DerSequence(v);
    }
}
