using System.Globalization;
using Ofd.Core.Signatures.Sig;

namespace Ofd.Pkg.Container;

/// <summary>签名资源容器（Java SignDir）：Signature.xml / Seal.esl / SignedValue.dat。</summary>
public class SignDir : VirtualContainer
{
    /// <summary>签名容器名称前缀。</summary>
    public const string SignContainerPrefix = "Sign_";

    /// <summary>电子印章文件名。</summary>
    public const string SealFileName = "Seal.esl";

    /// <summary>签名/签章描述文件名。</summary>
    public const string SignatureFileName = "Signature.xml";

    /// <summary>签名值文件名。</summary>
    public const string SignedValueFileName = "SignedValue.dat";

    /// <summary>表示第几个签名（目录名去掉前缀后的字符串）。</summary>
    private readonly string _index;

    public SignDir(string fullDir) : base(fullDir)
    {
        _index = ContainerName.Replace(SignContainerPrefix, "");
    }

    /// <summary>获取该签名容器索引（非数字名称抛 FormatException，与 Java NumberFormatException 对应）。</summary>
    public int GetIndex() => int.Parse(_index, CultureInfo.InvariantCulture);

    /// <summary>获取签名容器名称中索引部分（兼容非规范名称）。</summary>
    public string GetIndexStr() => _index;

    /// <summary>获取签名/签章描述文件（不存在抛 FileNotFoundException）。</summary>
    public Signature GetSignature() => new(GetObjByFile(SignatureFileName));

    /// <summary>设置签名/签章描述文件（内存缓存，flush 时落盘）。</summary>
    public SignDir SetSignature(Signature signature)
    {
        PutObj(SignatureFileName, signature);
        return this;
    }

    /// <summary>获取电子印章文件路径（不存在抛 FileNotFoundException）。</summary>
    public string GetSeal() => GetFileOrThrow(SealFileName);

    /// <summary>设置电子印章文件（复制进容器；保持源文件名，与 Java putFile 一致）。</summary>
    public SignDir SetSeal(string seal)
    {
        PutFile(seal);
        return this;
    }

    /// <summary>获取签名值文件路径（不存在抛 FileNotFoundException）。</summary>
    public string GetSignedValue() => GetFileOrThrow(SignedValueFileName);

    /// <summary>设置签名值文件（复制进容器；保持源文件名，与 Java putFile 一致）。</summary>
    public SignDir SetSignedValue(string signedValue)
    {
        PutFile(signedValue);
        return this;
    }
}
