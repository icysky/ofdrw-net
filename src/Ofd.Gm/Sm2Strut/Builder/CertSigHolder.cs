using Ofd.Gm.Cert;
using Org.BouncyCastle.Asn1;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Gm.Sm2Strut.Builder;

/// <summary>证书和签名值封装（Java CertSigHolder）。</summary>
public class CertSigHolder
{
    private byte[] _signature;
    private BcX509Certificate? _certificate;

    /// <summary>ASN.1 证书结构缓存。</summary>
    private Asn1Sequence? _asn1Obj;

    public CertSigHolder(byte[] signature, BcX509Certificate certificate)
    {
        _signature = signature;
        _certificate = certificate;
    }

    public byte[] GetSignature() => _signature;

    public void SetSignature(byte[] signature) => _signature = signature;

    public BcX509Certificate? GetCertificate() => _certificate;

    public void SetCertificate(BcX509Certificate certificate)
    {
        _certificate = certificate;
        _asn1Obj = null;
    }

    /// <summary>转换证书对象为 ASN.1 结构（缓存）。</summary>
    public Asn1Sequence GetAsn1Cert()
    {
        if (_certificate == null) throw new ArgumentException("证书对象(certificate)为空");
        return _asn1Obj ??= CertTools.Asn1(_certificate);
    }
}
