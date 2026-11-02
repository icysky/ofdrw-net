using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.Parse;

/// <summary>版本持有器（Java SESVersionHolder）：版本号 + 原始 ASN.1 序列，按需解出对应版本对象。</summary>
public class SESVersionHolder
{
    private readonly SESVersion _version;
    private readonly Asn1Sequence _objSeq;

    public SESVersionHolder(SESVersion version, Asn1Sequence objSeq)
    {
        _version = version;
        _objSeq = objSeq;
    }

    public SESVersion GetVersion() => _version;

    public Asn1Sequence GetObjSeq() => _objSeq;

    /// <summary>获取版本对应的电子签章值对象（v1/v4 已实现；v5 TODO(P3-ses-v5)）。</summary>
    public Asn1Encodable? SesObject()
    {
        switch (_version)
        {
            case SESVersion.V1:
                if (_objSeq.Count == 2) return new V1.SES_Signature(_objSeq);
                break;
            case SESVersion.V4:
                if (_objSeq.Count >= 4) return new V4.SES_Signature(_objSeq);
                break;
            case SESVersion.V5:
                if (_objSeq.Count >= 4) return new V5.SES_Signature(_objSeq);
                break;
        }
        return null;
    }

    /// <summary>获取版本对应的电子印章对象（v1/v4/v5 均已实现）。</summary>
    public Asn1Encodable? SealObject()
    {
        switch (_version)
        {
            case SESVersion.V1:
                if (_objSeq.Count == 2) return new V1.SESeal(_objSeq);
                break;
            case SESVersion.V4:
                if (_objSeq.Count == 4) return new V4.SESeal(_objSeq);
                break;
            case SESVersion.V5:
                if (_objSeq.Count >= 4) return new V5.SESeal(_objSeq);
                break;
        }
        return null;
    }

    /// <summary>强制获取 v1 版本电子签章数据。</summary>
    public V1.SES_Signature? GetInstanceV1() => SesObject() as V1.SES_Signature;

    /// <summary>强制获取 v4 版本电子签章数据。</summary>
    public V4.SES_Signature? GetInstanceV4() => SesObject() as V4.SES_Signature;

    /// <summary>强制获取 v5 版本电子签章数据。</summary>
    public V5.SES_Signature? GetInstanceV5() => SesObject() as V5.SES_Signature;

    /// <summary>强制获取 v1 版本电子印章数据。</summary>
    public V1.SESeal? GetInstanceV1Seal() => SealObject() as V1.SESeal;

    /// <summary>强制获取 v4 版本电子印章数据。</summary>
    public V4.SESeal? GetInstanceV4Seal() => SealObject() as V4.SESeal;

    /// <summary>强制获取 v5 版本电子印章数据。</summary>
    public V5.SESeal? GetInstanceV5Seal() => SealObject() as V5.SESeal;
}
