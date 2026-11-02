using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>印章图片信息（Java v5 SES_ESPictrueInfo）：类型 + 数据 + 宽高。</summary>
public class SES_ESPictrueInfo : Asn1Encodable
{
    private DerIA5String? _type;
    private Asn1OctetString? _data;
    private DerInteger? _width;
    private DerInteger? _height;

    public SES_ESPictrueInfo() { }

    public SES_ESPictrueInfo(DerIA5String type, Asn1OctetString data, DerInteger width, DerInteger height)
    {
        _type = type;
        _data = data;
        _width = width;
        _height = height;
    }

    public SES_ESPictrueInfo(Asn1Sequence seq)
    {
        _type = DerIA5String.GetInstance(seq[0]);
        _data = Asn1OctetString.GetInstance(seq[1]);
        _width = DerInteger.GetInstance(seq[2]);
        _height = DerInteger.GetInstance(seq[3]);
    }

    public static SES_ESPictrueInfo? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SES_ESPictrueInfo ?? new SES_ESPictrueInfo(Asn1Sequence.GetInstance(o));
    }

    public DerIA5String? GetType() => _type;

    public SES_ESPictrueInfo SetType(DerIA5String type)
    {
        _type = type;
        return this;
    }

    public SES_ESPictrueInfo SetType(string type)
    {
        _type = new DerIA5String(type);
        return this;
    }

    public Asn1OctetString? GetData() => _data;

    public SES_ESPictrueInfo SetData(Asn1OctetString data)
    {
        _data = data;
        return this;
    }

    public SES_ESPictrueInfo SetData(byte[] data)
    {
        _data = new DerOctetString(data);
        return this;
    }

    public DerInteger? GetWidth() => _width;

    public SES_ESPictrueInfo SetWidth(DerInteger width)
    {
        _width = width;
        return this;
    }

    public SES_ESPictrueInfo SetWidth(long width)
    {
        _width = new DerInteger(width);
        return this;
    }

    public DerInteger? GetHeight() => _height;

    public SES_ESPictrueInfo SetHeight(DerInteger height)
    {
        _height = height;
        return this;
    }

    public SES_ESPictrueInfo SetHeight(long height)
    {
        _height = new DerInteger(height);
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(4);
        if (_type != null) v.Add(_type);
        if (_data != null) v.Add(_data);
        if (_width != null) v.Add(_width);
        if (_height != null) v.Add(_height);
        return new DerSequence(v);
    }
}
