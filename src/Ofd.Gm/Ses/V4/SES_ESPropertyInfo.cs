using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V4;

/// <summary>印章属性（Java SES_ESPropertyInfo）：类型/名称/证书列表类型与内容/制章与有效期。</summary>
public class SES_ESPropertyInfo : Asn1Encodable
{
    /// <summary>签章者证书信息类型：1 - 数字证书类型。</summary>
    public static readonly DerInteger CertListType = new(1);

    /// <summary>签章者证书信息类型：2 - 数字证书杂凑值。</summary>
    public static readonly DerInteger CertDigestListType = new(2);

    /// <summary>
    /// 印章类型（<c>type</c>）。
    /// <para>
    /// 仅声明印章用途：<b>不参与摘要计算、不影响验签</b>，阅读器「印章属性」显示的就是它。
    /// 取值：
    /// <list type="table">
    /// <item><term>1</term><description>单位印章（公章），GM/T 0031 定义</description></item>
    /// <item><term>2</term><description>个人印章（私章），GM/T 0031 定义</description></item>
    /// <item><term>3</term><description>行业/厂商扩展（ofdrw 官方样本 V4RideRight.ofd 实测为 3）</description></item>
    /// <item><term>4</term><description>合同专用章（常见约定；ofdrw 测试章「合同专用章」实测为 4）</description></item>
    /// </list>
    /// 3/4 的语义由发章系统约定；与既有系统对接时，用 <c>tools/SignProbe inspect</c>
    /// 查看对方文件的取值后照抄即可。名称最好与类型呼应（如名称含「合同专用章」则用 4）。
    /// </para>
    /// </summary>
    private DerInteger? _type;

    private DerUtf8String? _name;
    private DerInteger? _certListType;
    private SES_CertList? _certList;
    private Asn1GeneralizedTime? _createDate;
    private Asn1GeneralizedTime? _validStart;
    private Asn1GeneralizedTime? _validEnd;

    public SES_ESPropertyInfo() { }

    public SES_ESPropertyInfo(DerInteger type, DerUtf8String name, DerInteger certListType,
        SES_CertList certList, Asn1GeneralizedTime createDate, Asn1GeneralizedTime validStart, Asn1GeneralizedTime validEnd)
    {
        _type = type;
        _name = name;
        _certListType = certListType;
        _certList = certList;
        _createDate = createDate;
        _validStart = validStart;
        _validEnd = validEnd;
    }

    public SES_ESPropertyInfo(Asn1Sequence seq)
    {
        _type = DerInteger.GetInstance(seq[0]);
        _name = new DerUtf8String(DerUtf8String.GetInstance(seq[1]).GetString());
        _certListType = DerInteger.GetInstance(seq[2]);
        _certList = SES_CertList.GetInstance(_certListType, seq[3]);
        _createDate = Asn1GeneralizedTime.GetInstance(seq[4]);
        _validStart = Asn1GeneralizedTime.GetInstance(seq[5]);
        _validEnd = Asn1GeneralizedTime.GetInstance(seq[6]);
    }

    public static SES_ESPropertyInfo? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SES_ESPropertyInfo ?? new SES_ESPropertyInfo(Asn1Sequence.GetInstance(o));
    }

    /// <summary>获取印章类型（Java 方法名 getType；此处加后缀避免与 object.GetType 冲突）。</summary>
    public DerInteger? GetSealType() => _type;

    public SES_ESPropertyInfo SetType(DerInteger type)
    {
        _type = type;
        return this;
    }

    public DerUtf8String? GetName() => _name;

    public SES_ESPropertyInfo SetName(DerUtf8String name)
    {
        _name = name;
        return this;
    }

    public DerInteger? GetCertListType() => _certListType;

    public SES_ESPropertyInfo SetCertListType(DerInteger certListType)
    {
        _certListType = certListType;
        return this;
    }

    public SES_CertList? GetCertList() => _certList;

    public SES_ESPropertyInfo SetCertList(SES_CertList certList)
    {
        _certList = certList;
        return this;
    }

    public Asn1GeneralizedTime? GetCreateDate() => _createDate;

    public SES_ESPropertyInfo SetCreateDate(Asn1GeneralizedTime createDate)
    {
        _createDate = createDate;
        return this;
    }

    public Asn1GeneralizedTime? GetValidStart() => _validStart;

    public SES_ESPropertyInfo SetValidStart(Asn1GeneralizedTime validStart)
    {
        _validStart = validStart;
        return this;
    }

    public Asn1GeneralizedTime? GetValidEnd() => _validEnd;

    public SES_ESPropertyInfo SetValidEnd(Asn1GeneralizedTime validEnd)
    {
        _validEnd = validEnd;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(7);
        if (_type != null) v.Add(_type);
        if (_name != null) v.Add(_name);
        if (_certListType != null) v.Add(_certListType);
        if (_certList != null) v.Add(_certList);
        if (_createDate != null) v.Add(_createDate);
        if (_validStart != null) v.Add(_validStart);
        if (_validEnd != null) v.Add(_validEnd);
        return new DerSequence(v);
    }
}
