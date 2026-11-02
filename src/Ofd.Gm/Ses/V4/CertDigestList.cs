using System.Collections;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V4;

/// <summary>签章者证书杂凑值列表（Java CertDigestList）。</summary>
public class CertDigestList : Asn1Encodable, IEnumerable<CertDigestObj>
{
    private readonly List<CertDigestObj> _dataSequence = new();

    public CertDigestList() { }

    public CertDigestList(IEnumerable<CertDigestObj> arr) => _dataSequence.AddRange(arr);

    public CertDigestList(Asn1Sequence seq)
    {
        for (int i = 0; i != seq.Count; i++)
        {
            var item = CertDigestObj.GetInstance(seq[i]);
            if (item != null) Add(item);
        }
    }

    public static CertDigestList? GetInstance(object? obj)
    {
        if (obj == null) return null;
        return obj as CertDigestList ?? new CertDigestList(Asn1Sequence.GetInstance(obj));
    }

    public CertDigestList Add(CertDigestObj obj)
    {
        _dataSequence.Add(obj);
        return this;
    }

    public CertDigestObj Get(int index) => _dataSequence[index];

    public int Count => _dataSequence.Count;

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(_dataSequence.Count);
        foreach (var d in _dataSequence) v.Add(d);
        return new DerSequence(v);
    }

    public IEnumerator<CertDigestObj> GetEnumerator() => _dataSequence.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
