using System.Collections;
using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V5;

/// <summary>自定义属性字段序列（Java v5 ExtensionDatas）。</summary>
public class ExtensionDatas : Asn1Encodable, IEnumerable<ExtData>
{
    private readonly List<ExtData> _dataSequence = new();

    public ExtensionDatas() { }

    public ExtensionDatas(IEnumerable<ExtData> arr) => _dataSequence.AddRange(arr);

    public ExtensionDatas(Asn1Sequence seq)
    {
        for (int i = 0; i != seq.Count; i++)
        {
            try
            {
                var item = ExtData.GetInstance(seq[i]);
                if (item != null) Add(item);
            }
            catch
            {
                // 兼容非标签章：忽略无法解析的项
            }
        }
    }

    public ExtensionDatas Add(ExtData o)
    {
        _dataSequence.Add(o);
        return this;
    }

    public ExtData Get(int index) => _dataSequence[index];

    public int Count => _dataSequence.Count;

    public static ExtensionDatas? GetInstance(object? obj)
    {
        switch (obj)
        {
            case null:
                return null;
            case ExtensionDatas datas:
                return datas;
            case Asn1OctetString octets:
                return new ExtensionDatas(Asn1Sequence.GetInstance(octets.GetOctets()));
            case Asn1TaggedObject tagged:
                return new ExtensionDatas(Asn1Sequence.GetInstance(tagged, true));
            default:
                return new ExtensionDatas(Asn1Sequence.GetInstance(obj));
        }
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(_dataSequence.Count);
        foreach (var d in _dataSequence) v.Add(d);
        return new DerSequence(v);
    }

    public IEnumerator<ExtData> GetEnumerator() => _dataSequence.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
