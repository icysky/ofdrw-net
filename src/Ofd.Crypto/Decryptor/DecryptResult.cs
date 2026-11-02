namespace Ofd.Crypto.Decryptor;

/// <summary>解密结果：文件加密密钥（FEK）与初始化向量（IV），均为 16 字节。</summary>
public class DecryptResult
{
    private readonly byte[] _fek;
    private readonly byte[] _iv;

    public DecryptResult(byte[] fek, byte[] iv)
    {
        if (fek == null || fek.Length != 16) throw new ArgumentException("文件加密密钥(fek)必须为16字节");
        if (iv == null || iv.Length != 16) throw new ArgumentException("初始化向量(iv)必须为16字节");
        _fek = (byte[])fek.Clone();
        _iv = (byte[])iv.Clone();
    }

    /// <summary>获取 FEK 副本。</summary>
    public byte[] GetFek() => (byte[])_fek.Clone();

    /// <summary>获取 IV 副本。</summary>
    public byte[] GetIv() => (byte[])_iv.Clone();
}
