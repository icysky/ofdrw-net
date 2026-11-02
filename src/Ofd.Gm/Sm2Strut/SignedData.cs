using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace Ofd.Gm.Sm2Strut;

/// <summary>
/// GB/T 35275-2017 8.1 signedData 类型：任意数据 + 至少一个签名者的签名值。
/// </summary>
public class SignedData : Asn1Encodable
{
    /// <summary>版本号固定为 1。</summary>
    public static readonly DerInteger VERSION_1 = new(1);

    private DerInteger _version = VERSION_1;
    private Asn1Set? _digestAlgorithms;
    private ContentInfo? _contentInfo;
    private Asn1Set? _certificates;
    private Asn1Set? _crls;
    private Asn1Set? _signerInfos;

    public SignedData() { }

    public SignedData(Asn1Set digestAlgorithms, ContentInfo contentInfo, Asn1Set? certificates, Asn1Set signerInfos)
    {
        _digestAlgorithms = digestAlgorithms;
        _contentInfo = contentInfo;
        _certificates = certificates;
        _signerInfos = signerInfos;
    }

    public SignedData(Asn1Sequence seq)
    {
        _version = DerInteger.GetInstance(seq[0]);
        _digestAlgorithms = Asn1Set.GetInstance(seq[1]);
        _contentInfo = ContentInfo.GetInstance(seq[2]);

        for (int i = 3; i < seq.Count; i++)
        {
            var o = seq[i];
            if (o is Asn1TaggedObject tagged)
            {
                switch (tagged.TagNo)
                {
                    case 0:
                        _certificates = Asn1Set.GetInstance(tagged, false);
                        break;
                    case 1:
                        _crls = Asn1Set.GetInstance(tagged, false);
                        break;
                    default:
                        throw new ArgumentException("无法接收标签参数 " + tagged.TagNo);
                }
            }
            else
            {
                _signerInfos = Asn1Set.GetInstance(o);
            }
        }
    }

    public static SignedData? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SignedData ?? new SignedData(Asn1Sequence.GetInstance(o));
    }

    public DerInteger GetVersion() => _version;

    public SignedData SetVersion(DerInteger version)
    {
        _version = version;
        return this;
    }

    public Asn1Set? GetDigestAlgorithms() => _digestAlgorithms;

    public SignedData SetDigestAlgorithms(Asn1Set digestAlgorithms)
    {
        _digestAlgorithms = digestAlgorithms;
        return this;
    }

    public ContentInfo? GetContentInfo() => _contentInfo;

    public SignedData SetContentInfo(ContentInfo contentInfo)
    {
        _contentInfo = contentInfo;
        return this;
    }

    public Asn1Set? GetCertificates() => _certificates;

    public SignedData SetCertificates(Asn1Set certificates)
    {
        _certificates = certificates;
        return this;
    }

    public Asn1Set? GetCrls() => _crls;

    public SignedData SetCrls(Asn1Set crls)
    {
        _crls = crls;
        return this;
    }

    public Asn1Set? GetSignerInfos() => _signerInfos;

    public SignedData SetSignerInfos(Asn1Set signerInfos)
    {
        _signerInfos = signerInfos;
        return this;
    }

    /// <summary>
    /// 按颁发者 DN 与序列号查找签名证书，不存在返回 null。
    /// <para>注：BC C# 2.x 无 ASN.1 <c>Certificate</c> 类型，返回完整证书的 ASN.1 序列。</para>
    /// </summary>
    public Asn1Sequence? GetSignCert(IssuerAndSerialNumber? iaSn)
    {
        if (iaSn == null || _certificates == null) return null;

        foreach (var item in _certificates)
        {
            var certSeq = Asn1Sequence.GetInstance(item);
            var tbs = TbsCertificateStructure.GetInstance(certSeq[0]);
            bool snEq = tbs.SerialNumber.Equals(iaSn.GetCertSerialNumber());
            bool dnEq = tbs.Issuer.Equals(iaSn.GetName());
            if (snEq && dnEq) return certSeq;
        }
        return null;
    }

    /// <summary>按颁发者 DN 与序列号获取签名证书（高层 X509Certificate，便于验签取公钥）。</summary>
    public Org.BouncyCastle.X509.X509Certificate? GetSignCertX509(IssuerAndSerialNumber? iaSn)
    {
        var seq = GetSignCert(iaSn);
        return seq == null ? null : new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(seq.GetEncoded());
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(6);
        v.Add(_version);
        if (_digestAlgorithms != null) v.Add(_digestAlgorithms);
        if (_contentInfo != null) v.Add(_contentInfo);
        if (_certificates != null) v.Add(new DerTaggedObject(false, 0, _certificates));
        if (_crls != null) v.Add(new DerTaggedObject(false, 1, _crls));
        if (_signerInfos != null) v.Add(_signerInfos);
        return new DerSequence(v);
    }
}
