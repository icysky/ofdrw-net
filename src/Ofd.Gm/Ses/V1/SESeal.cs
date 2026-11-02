using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V1;

/// <summary>电子印章数据（Java v1 SESeal）：印章信息 + 制章人对印章签名的信息。</summary>
public class SESeal : Asn1Encodable
{
    private SES_SealInfo? _esealInfo;
    private SES_SignInfo? _signInfo;

    public SESeal() { }

    public SESeal(SES_SealInfo esealInfo, SES_SignInfo signInfo)
    {
        _esealInfo = esealInfo;
        _signInfo = signInfo;
    }

    public SESeal(Asn1Sequence seq)
    {
        _esealInfo = SES_SealInfo.GetInstance(seq[0]);
        // 兼容非标签章：其签名信息格式与标准不同，可能无法解析
        try
        {
            if (seq.Count > 1) _signInfo = SES_SignInfo.GetInstance(seq[1]);
        }
        catch
        {
            // ignore
        }
    }

    public static SESeal? GetInstance(object? o)
    {
        switch (o)
        {
            case null:
                return null;
            case SESeal seal:
                return seal;
            case byte[] bytes:
                try
                {
                    return new SESeal(Asn1Sequence.GetInstance(Asn1Object.FromByteArray(bytes)));
                }
                catch (Exception e)
                {
                    throw new ArgumentException("电子印章数据 无法解析", e);
                }
            case DerOctetString octets:
                return new SESeal(Asn1Sequence.GetInstance(octets.GetOctets()));
            default:
                return new SESeal(Asn1Sequence.GetInstance(o));
        }
    }

    public SES_SealInfo? GetEsealInfo() => _esealInfo;

    public SESeal SetEsealInfo(SES_SealInfo esealInfo)
    {
        _esealInfo = esealInfo;
        return this;
    }

    public SES_SignInfo? GetSignInfo() => _signInfo;

    public SESeal SetSignInfo(SES_SignInfo signInfo)
    {
        _signInfo = signInfo;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(2);
        if (_esealInfo != null) v.Add(_esealInfo);
        if (_signInfo != null) v.Add(_signInfo);
        return new DerSequence(v);
    }
}
