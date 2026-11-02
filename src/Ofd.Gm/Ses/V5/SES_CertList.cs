using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>签章者证书信息列表（Java v5 SES_CertList，ASN1Choice）：type=1 证书列表 / type=2 杂凑值列表。</summary>
public class SES_CertList : Asn1Encodable
{
    private readonly CertInfoList? _certs;
    private readonly CertDigestList? _certDigestList;

    public SES_CertList(CertInfoList certs)
    {
        _certs = certs;
        _certDigestList = null;
    }

    public SES_CertList(CertDigestList certDigestList)
    {
        _certs = null;
        _certDigestList = certDigestList;
    }

    public static SES_CertList? GetInstance(DerInteger type, object? obj)
    {
        switch (obj)
        {
            case null:
                return null;
            case SES_CertList certList:
                return certList;
            case Asn1Encodable:
            {
                int t = type.IntValueExact;
                return t switch
                {
                    1 => new SES_CertList(CertInfoList.GetInstance(obj)!),
                    2 => new SES_CertList(CertDigestList.GetInstance(obj)!),
                    _ => throw new ArgumentException("unknown type in getInstance(): " + obj.GetType().Name),
                };
            }
            case byte[] bytes:
                return GetInstance(type, Asn1Object.FromByteArray(bytes));
            default:
                throw new ArgumentException("unknown object in getInstance(): " + obj.GetType().Name);
        }
    }

    /// <summary>获取承载的 ASN.1 对象（证书列表或杂凑值列表）。</summary>
    public Asn1Encodable? Get() => _certs != null ? _certs : _certDigestList;

    public CertInfoList? GetCerts() => _certs;

    public CertDigestList? GetCertDigestList() => _certDigestList;

    public override Asn1Object ToAsn1Object() =>
        Get()?.ToAsn1Object() ?? throw new InvalidOperationException("SES_CertList 未设置证书列表或杂凑值列表");
}
