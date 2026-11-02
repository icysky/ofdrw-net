using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;

namespace Ofd.Gm.Sm2Strut;

/// <summary>
/// GB/T 35276-2017 7.2 加密数据格式：
/// <para>SM2Cipher ::= SEQUENCE { xCoordinate INTEGER, yCoordinate INTEGER, HASH OCTET STRING(32), CipherText OCTET STRING }</para>
/// </summary>
public class SM2Cipher : Asn1Encodable
{
    private DerInteger? _xCoordinate;
    private DerInteger? _yCoordinate;
    private Asn1OctetString? _hash;
    private Asn1OctetString? _cipherText;

    public SM2Cipher() { }

    public SM2Cipher(Asn1Sequence seq)
    {
        _xCoordinate = DerInteger.GetInstance(seq[0]);
        _yCoordinate = DerInteger.GetInstance(seq[1]);
        _hash = Asn1OctetString.GetInstance(seq[2]);
        _cipherText = Asn1OctetString.GetInstance(seq[3]);
    }

    public static SM2Cipher? GetInstance(object? o)
    {
        if (o == null) return null;
        return o as SM2Cipher ?? new SM2Cipher(Asn1Sequence.GetInstance(o));
    }

    public DerInteger? GetxCoordinate() => _xCoordinate;

    public void SetxCoordinate(DerInteger xCoordinate) => _xCoordinate = xCoordinate;

    public DerInteger? GetyCoordinate() => _yCoordinate;

    public void SetyCoordinate(DerInteger yCoordinate) => _yCoordinate = yCoordinate;

    public Asn1OctetString? GetHash() => _hash;

    public void SetHash(Asn1OctetString hash) => _hash = hash;

    public Asn1OctetString? GetCipherText() => _cipherText;

    public void SetCipherText(Asn1OctetString cipherText) => _cipherText = cipherText;

    public override Asn1Object ToAsn1Object()
    {
        var v = new Asn1EncodableVector(4);
        if (_xCoordinate != null) v.Add(_xCoordinate);
        if (_yCoordinate != null) v.Add(_yCoordinate);
        if (_hash != null) v.Add(_hash);
        if (_cipherText != null) v.Add(_cipherText);
        return new DerSequence(v);
    }

    /// <summary>转换为 C1C3C2 格式：04 || X(32) || Y(32) || C3(32) || C2。</summary>
    public byte[] ConvertC1C3C2()
    {
        var x = new byte[32];
        var y = new byte[32];
        CopyCoordinate(_xCoordinate, x);
        CopyCoordinate(_yCoordinate, y);

        using var ms = new MemoryStream();
        ms.WriteByte(0x04); // C1：未压缩点前缀
        ms.Write(x, 0, x.Length);
        ms.Write(y, 0, y.Length);
        var hash = _hash?.GetOctets() ?? Array.Empty<byte>();
        ms.Write(hash, 0, hash.Length); // C3
        var cipher = _cipherText?.GetOctets() ?? Array.Empty<byte>();
        ms.Write(cipher, 0, cipher.Length); // C2
        return ms.ToArray();
    }

    /// <summary>把 C1C3C2 格式加密结果转换为 SM2Cipher。</summary>
    public static SM2Cipher FromC1C3C2(byte[] c1c3c2)
    {
        if (c1c3c2 == null || c1c3c2.Length < 1 + 32 + 32 + 32)
            throw new ArgumentException("C1C3C2 数据长度不足");

        int offset = 1; // 跳过未压缩点前缀 0x04
        var x = new byte[32];
        var y = new byte[32];
        var hash = new byte[32];
        int length = c1c3c2.Length - 1 - 32 - 32 - 32;
        var cipherText = new byte[length];

        Array.Copy(c1c3c2, offset, x, 0, 32); offset += 32;
        Array.Copy(c1c3c2, offset, y, 0, 32); offset += 32;
        Array.Copy(c1c3c2, offset, hash, 0, 32); offset += 32;
        Array.Copy(c1c3c2, offset, cipherText, 0, length);

        return new SM2Cipher
        {
            _xCoordinate = new DerInteger(new BigInteger(1, x)),
            _yCoordinate = new DerInteger(new BigInteger(1, y)),
            _hash = new DerOctetString(hash),
            _cipherText = new DerOctetString(cipherText),
        };
    }

    /// <summary>把坐标值按大端右对齐写入 32 字节缓冲。</summary>
    private static void CopyCoordinate(DerInteger? value, byte[] buffer)
    {
        if (value == null) return;
        byte[] tmp = BigIntegers.AsUnsignedByteArray(value.Value);
        if (tmp.Length >= buffer.Length)
            Array.Copy(tmp, tmp.Length - buffer.Length, buffer, 0, buffer.Length);
        else
            Array.Copy(tmp, 0, buffer, buffer.Length - tmp.Length, tmp.Length);
    }
}
