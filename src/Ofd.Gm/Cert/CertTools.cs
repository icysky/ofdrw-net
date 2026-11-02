using Org.BouncyCastle.Asn1;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Gm.Cert;

/// <summary>证书转换工具（Java CertTools）：X509 证书与其 ASN.1 结构互转。</summary>
public static class CertTools
{
    /// <summary>转换证书对象为 ASN.1 结构（完整证书 SEQUENCE）。</summary>
    public static Asn1Sequence Asn1(BcX509Certificate certificate)
    {
        if (certificate == null) throw new ArgumentException("证书(certificate)为空");

        var p = Asn1Object.FromByteArray(certificate.GetEncoded());
        if (p == null) throw new ArgumentException("无法解析证书(certificate)");
        return Asn1Sequence.GetInstance(p);
    }

    /// <summary>转换 ASN.1 结构为证书对象。</summary>
    public static BcX509Certificate Obj(Asn1Sequence certificate) =>
        new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(certificate.GetEncoded());

    /// <summary>从 DER 字节解析证书对象。</summary>
    public static BcX509Certificate FromDer(byte[] der) =>
        new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(der);
}
