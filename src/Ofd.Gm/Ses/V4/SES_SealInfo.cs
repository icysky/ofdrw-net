using Ofd.Gm.Ses.V1;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V4;

/// <summary>印章信息（Java SES_SealInfo）：头信息 + 印章标识 + 属性 + 图片 + 自定义数据。</summary>
public class SES_SealInfo : Asn1Encodable
{
    private SES_Header? _header;
    private DerIA5String? _esID;
    private SES_ESPropertyInfo? _property;
    private SES_ESPictrueInfo? _picture;
    private ExtensionDatas? _extDatas;

    public SES_SealInfo() { }

    public SES_SealInfo(SES_Header? header, DerIA5String? esID, SES_ESPropertyInfo? property,
        SES_ESPictrueInfo? picture, ExtensionDatas? extDatas)
    {
        _header = header;
        _esID = esID;
        _property = property;
        _picture = picture;
        _extDatas = extDatas;
    }

    public SES_SealInfo(Asn1Sequence seq)
    {
        _header = SES_Header.GetInstance(seq[0]);
        _esID = new DerIA5String(DerIA5String.GetInstance(seq[1]).GetString());
        _property = SES_ESPropertyInfo.GetInstance(seq[2]);
        _picture = SES_ESPictrueInfo.GetInstance(seq[3]);
        if (seq.Count > 4)
            _extDatas = ExtensionDatas.GetInstance(seq[4]);
    }

    public static SES_SealInfo? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SES_SealInfo ?? new SES_SealInfo(Asn1Sequence.GetInstance(o));
    }

    public SES_Header? GetHeader() => _header;

    public SES_SealInfo SetHeader(SES_Header header)
    {
        _header = header;
        return this;
    }

    public DerIA5String? GetEsID() => _esID;

    public SES_SealInfo SetEsID(DerIA5String esID)
    {
        _esID = esID;
        return this;
    }

    public SES_SealInfo SetEsID(string esID)
    {
        _esID = new DerIA5String(esID);
        return this;
    }

    public SES_ESPropertyInfo? GetProperty() => _property;

    public SES_SealInfo SetProperty(SES_ESPropertyInfo property)
    {
        _property = property;
        return this;
    }

    public SES_ESPictrueInfo? GetPicture() => _picture;

    public SES_SealInfo SetPicture(SES_ESPictrueInfo picture)
    {
        _picture = picture;
        return this;
    }

    public ExtensionDatas? GetExtDatas() => _extDatas;

    public SES_SealInfo SetExtDatas(ExtensionDatas extDatas)
    {
        _extDatas = extDatas;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(5);
        if (_header != null) v.Add(_header);
        if (_esID != null) v.Add(_esID);
        if (_property != null) v.Add(_property);
        if (_picture != null) v.Add(_picture);
        if (_extDatas != null) v.Add(_extDatas);
        return new DerSequence(v);
    }
}
