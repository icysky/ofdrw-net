using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.V1;

/// <summary>电子签章数据（Java v1 SES_Signature）：待签章数据 + 签名值。</summary>
public class SES_Signature : Asn1Encodable
{
    private TBS_Sign? _toSign;
    private DerBitString? _signature;

    public SES_Signature() { }

    public SES_Signature(TBS_Sign toSign, DerBitString signature)
    {
        _toSign = toSign;
        _signature = signature;
    }

    public SES_Signature(Asn1Sequence seq)
    {
        _toSign = TBS_Sign.GetInstance(seq[0]);
        _signature = DerBitString.GetInstance(seq[1]);
    }

    public static SES_Signature? GetInstance(object? o)
    {
        switch (o)
        {
            case null:
                return null;
            case SES_Signature sig:
                return sig;
            case byte[] bytes:
                try
                {
                    return new SES_Signature(Asn1Sequence.GetInstance(Asn1Object.FromByteArray(bytes)));
                }
                catch (Exception e)
                {
                    throw new ArgumentException("电子签章数据 格式错误", e);
                }
            default:
                return new SES_Signature(Asn1Sequence.GetInstance(o));
        }
    }

    public TBS_Sign? GetToSign() => _toSign;

    public SES_Signature SetToSign(TBS_Sign toSign)
    {
        _toSign = toSign;
        return this;
    }

    public DerBitString? GetSignature() => _signature;

    public SES_Signature SetSignature(DerBitString signature)
    {
        _signature = signature;
        return this;
    }

    public SES_Signature SetSignature(byte[] signature)
    {
        _signature = new DerBitString(signature);
        return this;
    }

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(2);
        if (_toSign != null) v.Add(_toSign);
        if (_signature != null) v.Add(_signature);
        return new DerSequence(v);
    }
}
