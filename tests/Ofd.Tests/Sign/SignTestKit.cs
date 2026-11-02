using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicType;
using Ofd.Pkg.Container;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;
using PageNode = Ofd.Core.BasicStructure.PageTree.Page;
using PageObjType = Ofd.Core.BasicStructure.PageObj.PageObj;

namespace Ofd.Tests.Sign;

/// <summary>签章测试公用构造（最小 OFD、SM2 自签证书）。</summary>
internal static class SignTestKit
{
    public static string BuildSampleOfd(string root, int pages = 2)
    {
        var ofdDir = new OFDDir(Path.Combine(root, "plain"));
        var doc = ofdDir.NewDoc();

        var pageTree = new Pages();
        for (int i = 0; i < pages; i++)
            pageTree.AddPage(new PageNode().SetID(new ST_ID(i + 1))
                .SetBaseLoc(new ST_Loc($"Pages/Page_{i}/Content.xml")));

        doc.SetDocument(new Document()
            .SetCommonData(new CT_CommonData().SetMaxUnitID(100))
            .SetPages(pageTree));

        for (int i = 0; i < pages; i++)
            doc.ObtainPages().NewPageDir().SetContent(new PageObjType());

        ofdDir.SetOfd(new OFD().AddDocBody(new DocBody().SetDocRoot(new ST_Loc("Doc_0/Document.xml"))));
        ofdDir.Flush();

        string file = Path.Combine(root, "plain.ofd");
        ofdDir.Jar(file);
        return file;
    }

    public static (ECPrivateKeyParameters Priv, BcX509Certificate Cert) NewSm2Cert(long serial = 51)
    {
        var curve = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("sm2p256v1");
        var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
        var gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
        gen.Init(new ECKeyGenerationParameters(domain, new Org.BouncyCastle.Security.SecureRandom()));
        var kp = gen.GenerateKeyPair();

        var certGen = new X509V3CertificateGenerator();
        certGen.SetSerialNumber(Org.BouncyCastle.Math.BigInteger.ValueOf(serial));
        certGen.SetIssuerDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Sign-Kit"));
        certGen.SetSubjectDN(new Org.BouncyCastle.Asn1.X509.X509Name("CN=Sign-Kit"));
        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-1));
        certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
        certGen.SetPublicKey(kp.Public);
        var cert = certGen.Generate(new Org.BouncyCastle.Crypto.Operators.Asn1SignatureFactory("SM3withSM2", kp.Private));
        return ((ECPrivateKeyParameters)kp.Private, cert);
    }
}
