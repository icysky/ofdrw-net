using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Sm2Strut;

/// <summary>
/// GB/T 35275-2017 6.10 ContentInfo：内容交换的通用语法结构。
/// <para>ContentInfo ::= SEQUENCE { contentType OBJECT IDENTIFIER, content [0] EXPLICIT ANY OPTIONAL }</para>
/// </summary>
public class ContentInfo : Asn1Encodable
{
    private DerObjectIdentifier? _contentType;
    private Asn1Encodable? _content;

    public ContentInfo() { }

    public ContentInfo(DerObjectIdentifier contentType, Asn1Encodable? content)
    {
        _contentType = contentType;
        _content = content;
    }

    public ContentInfo(Asn1Sequence seq)
    {
        _contentType = DerObjectIdentifier.GetInstance(seq[0]);
        if (seq.Count > 1)
        {
            var tagged = Asn1TaggedObject.GetInstance(seq[1]);
            if (!tagged.IsExplicit() || tagged.TagNo != 0)
                throw new ArgumentException("content 无法解析");
            _content = tagged.GetExplicitBaseObject();
        }
    }

    public static ContentInfo? GetInstance(object? obj)
    {
        if (obj == null) return null;
        return obj as ContentInfo ?? new ContentInfo(Asn1Sequence.GetInstance(obj));
    }

    public DerObjectIdentifier? GetContentType() => _contentType;

    public ContentInfo SetContentType(DerObjectIdentifier contentType)
    {
        _contentType = contentType;
        return this;
    }

    public Asn1Encodable? GetContent() => _content;

    public ContentInfo SetContent(Asn1Encodable? content)
    {
        _content = content;
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(2);
        if (_contentType != null) v.Add(_contentType);
        if (_content != null) v.Add(new DerTaggedObject(true, 0, _content));
        return new DerSequence(v);
    }
}
