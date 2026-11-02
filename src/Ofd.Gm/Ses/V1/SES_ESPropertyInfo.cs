using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V1;

/// <summary>印章属性信息（Java v1 SES_ESPropertyInfo）：类型/名称/证书列表/制章与有效期（UTC 时间）。</summary>
public class SES_ESPropertyInfo : Asn1Encodable
{
    /// <summary>单位印章类型。</summary>
    public static readonly DerInteger OrgType = new(1);

    /// <summary>个人印章类型（上游常量同为 1，保持原样）。</summary>
    public static readonly DerInteger PersonType = new(1);

    private DerInteger? _type;
    private DerUtf8String? _name;
    private Asn1Sequence? _certList;
    private Asn1UtcTime? _createDate;
    private Asn1UtcTime? _validStart;
    private Asn1UtcTime? _validEnd;

    public SES_ESPropertyInfo() { }

    public SES_ESPropertyInfo(Asn1Sequence seq)
    {
        _type = DerInteger.GetInstance(seq[0]);
        _name = new DerUtf8String(DerUtf8String.GetInstance(seq[1]).GetString());
        _certList = Asn1Sequence.GetInstance(seq[2]);

        // 兼容非标签章：日期可能是 GeneralizedTime
        _createDate = GetTimeInstance(seq[3]);
        _validStart = GetTimeInstance(seq[4]);
        _validEnd = GetTimeInstance(seq[5]);
    }

    private static Asn1UtcTime? GetTimeInstance(object? o)
    {
        if (o == null) return null;
        if (o is Asn1GeneralizedTime gt)
            return new DerUtcTime(NormalizeToUtc(gt.TimeString));
        return Asn1UtcTime.GetInstance(o);
    }

    /// <summary>
    /// 将 14 位 GeneralizedTime 字符串（yyyyMMddHHmmss[Z]）转成 UTCTime（yyMMddHHmmss[Z]）。
    /// 注：Java 侧 ASN1UTCTime 接受 14 位字符串，C# 侧的 Asn1UtcTime 会严格校验格式。
    /// </summary>
    private static string NormalizeToUtc(string timeString)
    {
        bool hasZ = timeString.EndsWith('Z');
        string core = hasZ ? timeString[..^1] : timeString;
        if (core.Length != 14 || !core.All(char.IsDigit)) return timeString;
        return timeString[2..];
    }

    public SES_ESPropertyInfo(DerInteger type, DerUtf8String name, Asn1Sequence certList,
        Asn1UtcTime createDate, Asn1UtcTime validStart, Asn1UtcTime validEnd)
    {
        _type = type;
        _name = name;
        _certList = certList;
        _createDate = createDate;
        _validStart = validStart;
        _validEnd = validEnd;
    }

    public static SES_ESPropertyInfo? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SES_ESPropertyInfo ?? new SES_ESPropertyInfo(Asn1Sequence.GetInstance(o));
    }

    /// <summary>获取印章类型（Java getType；加后缀避免与 object.GetType 冲突）。</summary>
    public DerInteger? GetSealType() => _type;

    public SES_ESPropertyInfo SetType(DerInteger type)
    {
        _type = type;
        return this;
    }

    public SES_ESPropertyInfo SetType(int type)
    {
        _type = new DerInteger(type);
        return this;
    }

    public DerUtf8String? GetName() => _name;

    public SES_ESPropertyInfo SetName(DerUtf8String name)
    {
        _name = name;
        return this;
    }

    public SES_ESPropertyInfo SetName(string name)
    {
        _name = new DerUtf8String(name);
        return this;
    }

    public Asn1Sequence? GetCertList() => _certList;

    public SES_ESPropertyInfo SetCertList(Asn1Sequence certList)
    {
        _certList = certList;
        return this;
    }

    public Asn1UtcTime? GetCreateDate() => _createDate;

    public SES_ESPropertyInfo SetCreateDate(Asn1UtcTime createDate)
    {
        _createDate = createDate;
        return this;
    }

    public Asn1UtcTime? GetValidStart() => _validStart;

    public SES_ESPropertyInfo SetValidStart(Asn1UtcTime validStart)
    {
        _validStart = validStart;
        return this;
    }

    public Asn1UtcTime? GetValidEnd() => _validEnd;

    public SES_ESPropertyInfo SetValidEnd(Asn1UtcTime validEnd)
    {
        _validEnd = validEnd;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(6);
        if (_type != null) v.Add(_type);
        if (_name != null) v.Add(_name);
        if (_certList != null) v.Add(_certList);
        if (_createDate != null) v.Add(_createDate);
        if (_validStart != null) v.Add(_validStart);
        if (_validEnd != null) v.Add(_validEnd);
        return new DerSequence(v);
    }
}
