using System.Collections;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V4;

/// <summary>签章者证书列表（Java CertInfoList）。</summary>
public class CertInfoList : Asn1Encodable, IEnumerable<Asn1OctetString>
{
    private readonly List<Asn1OctetString> _dataSequence = new();

    public CertInfoList() { }

    public CertInfoList(IEnumerable<Asn1OctetString> arr) => _dataSequence.AddRange(arr);

    public CertInfoList(Asn1Sequence seq)
    {
        for (int i = 0; i != seq.Count; i++)
            Add(DerOctetString.GetInstance(seq[i]));
    }

    public static CertInfoList? GetInstance(object? obj)
    {
        if (obj == null) return null;
        return obj as CertInfoList ?? new CertInfoList(Asn1Sequence.GetInstance(obj));
    }

    public CertInfoList Add(Asn1OctetString obj)
    {
        _dataSequence.Add(obj);
        return this;
    }

    public Asn1OctetString Get(int index) => _dataSequence[index];

    public int Count => _dataSequence.Count;

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(_dataSequence.Count);
        foreach (var d in _dataSequence) v.Add(d);
        return new DerSequence(v);
    }

    public IEnumerator<Asn1OctetString> GetEnumerator() => _dataSequence.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
