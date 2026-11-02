using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace Ofd.Gm.Sm2Strut;

/// <summary>GB/T 35275-2017 signerInfo 类型：签名者证书标识 + 摘要/签名算法 + 签名值。</summary>
public class SignerInfo : Asn1Encodable
{
    /// <summary>版本号固定为 1。</summary>
    public static readonly DerInteger VERSION_1 = new(1);

    private DerInteger _version = VERSION_1;
    private IssuerAndSerialNumber? _issuerAngSerialNumber;
    private AlgorithmIdentifier? _digestAlgorithm;
    private Asn1Set? _authenticatedAttributes;
    private AlgorithmIdentifier? _digestEncryptionAlgorithm;
    private Asn1OctetString? _encryptedDigest;
    private Asn1Set? _unauthenticatedAttributes;

    public SignerInfo() { }

    public SignerInfo(IssuerAndSerialNumber issuerAngSerialNumber, AlgorithmIdentifier digestAlgorithm,
        AlgorithmIdentifier digestEncryptionAlgorithm, Asn1OctetString encryptedDigest)
    {
        _version = VERSION_1;
        _issuerAngSerialNumber = issuerAngSerialNumber;
        _digestAlgorithm = digestAlgorithm;
        _digestEncryptionAlgorithm = digestEncryptionAlgorithm;
        _encryptedDigest = encryptedDigest;
    }

    public SignerInfo(Asn1Sequence seq)
    {
        _version = DerInteger.GetInstance(seq[0]);
        _issuerAngSerialNumber = IssuerAndSerialNumber.GetInstance(seq[1]);
        _digestAlgorithm = AlgorithmIdentifier.GetInstance(seq[2]);

        int i = 3;
        if (seq[i] is Asn1TaggedObject tagged)
        {
            _authenticatedAttributes = Asn1Set.GetInstance(tagged, false);
            i++;
        }
        _digestEncryptionAlgorithm = AlgorithmIdentifier.GetInstance(seq[i++]);
        _encryptedDigest = Asn1OctetString.GetInstance(seq[i++]);
        if (seq.Count > i)
            _unauthenticatedAttributes = Asn1Set.GetInstance(Asn1TaggedObject.GetInstance(seq[i]), false);
    }

    public static SignerInfo? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SignerInfo ?? new SignerInfo(Asn1Sequence.GetInstance(o));
    }

    public DerInteger GetVersion() => _version;

    public SignerInfo SetVersion(DerInteger version)
    {
        _version = version;
        return this;
    }

    public IssuerAndSerialNumber? GetIssuerAngSerialNumber() => _issuerAngSerialNumber;

    public SignerInfo SetIssuerAngSerialNumber(IssuerAndSerialNumber issuerAngSerialNumber)
    {
        _issuerAngSerialNumber = issuerAngSerialNumber;
        return this;
    }

    public AlgorithmIdentifier? GetDigestAlgorithm() => _digestAlgorithm;

    public SignerInfo SetDigestAlgorithm(AlgorithmIdentifier digestAlgorithm)
    {
        _digestAlgorithm = digestAlgorithm;
        return this;
    }

    public Asn1Set? GetAuthenticatedAttributes() => _authenticatedAttributes;

    public SignerInfo SetAuthenticatedAttributes(Asn1Set authenticatedAttributes)
    {
        _authenticatedAttributes = authenticatedAttributes;
        return this;
    }

    public AlgorithmIdentifier? GetDigestEncryptionAlgorithm() => _digestEncryptionAlgorithm;

    public SignerInfo SetDigestEncryptionAlgorithm(AlgorithmIdentifier digestEncryptionAlgorithm)
    {
        _digestEncryptionAlgorithm = digestEncryptionAlgorithm;
        return this;
    }

    public Asn1OctetString? GetEncryptedDigest() => _encryptedDigest;

    public SignerInfo SetEncryptedDigest(Asn1OctetString encryptedDigest)
    {
        _encryptedDigest = encryptedDigest;
        return this;
    }

    public Asn1Set? GetUnauthenticatedAttributes() => _unauthenticatedAttributes;

    public SignerInfo SetUnauthenticatedAttributes(Asn1Set unauthenticatedAttributes)
    {
        _unauthenticatedAttributes = unauthenticatedAttributes;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(7);
        v.Add(_version);
        if (_issuerAngSerialNumber != null) v.Add(_issuerAngSerialNumber);
        if (_digestAlgorithm != null) v.Add(_digestAlgorithm);
        if (_authenticatedAttributes != null) v.Add(new DerTaggedObject(false, 0, _authenticatedAttributes));
        if (_digestEncryptionAlgorithm != null) v.Add(_digestEncryptionAlgorithm);
        if (_encryptedDigest != null) v.Add(_encryptedDigest);
        if (_unauthenticatedAttributes != null) v.Add(new DerTaggedObject(false, 1, _unauthenticatedAttributes));
        return new DerSequence(v);
    }
}
