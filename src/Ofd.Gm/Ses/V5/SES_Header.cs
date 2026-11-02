using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>头信息（Java v5 SES_Header）：固定 "ES" + 版本号 5 + 厂商 ID。</summary>
public class SES_Header : Asn1Encodable
{
    /// <summary>电子印章数据结构版本号 V5。</summary>
    public static readonly DerInteger V5 = new(5);

    /// <summary>电子印章数据标识符固定值 "ES"。</summary>
    public static readonly DerIA5String ID = new("ES");

    private DerIA5String _id = ID;
    private DerInteger _version = V5;
    private DerIA5String _vid = new("");

    public SES_Header() { }

    public SES_Header(DerInteger version, DerIA5String vid)
    {
        _id = ID;
        _version = version;
        _vid = vid;
    }

    public SES_Header(Asn1Sequence seq)
    {
        _id = DerIA5String.GetInstance(seq[0]);
        _version = DerInteger.GetInstance(seq[1]);
        _vid = DerIA5String.GetInstance(seq[2]);
    }

    public static SES_Header? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SES_Header ?? new SES_Header(Asn1Sequence.GetInstance(o));
    }

    public DerIA5String GetId() => _id;

    public DerInteger GetVersion() => _version;

    public SES_Header SetVersion(DerInteger version)
    {
        _version = version;
        return this;
    }

    public SES_Header SetVersion(int version)
    {
        _version = new DerInteger(version);
        return this;
    }

    public DerIA5String GetVid() => _vid;

    public SES_Header SetVid(DerIA5String vid)
    {
        _vid = vid;
        return this;
    }

    public SES_Header SetVid(string vid)
    {
        _vid = new DerIA5String(vid);
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(3);
        v.Add(ID);
        v.Add(_version);
        v.Add(_vid);
        return new DerSequence(v);
    }
}
