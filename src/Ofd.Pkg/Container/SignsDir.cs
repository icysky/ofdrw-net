using System.Globalization;
using SignaturesType = Ofd.Core.Signatures.Signatures;

namespace Ofd.Pkg.Container;

/// <summary>签名容器（Java SignsDir）：Signatures.xml + Sign_N/ 子容器。</summary>
public class SignsDir : VirtualContainer
{
    /// <summary>签名列表文件名称。</summary>
    public const string SignaturesFileName = "Signatures.xml";

    /// <summary>签名 Index 最大值 + 1（即签名容器数量）。</summary>
    private int _maxSignIndex;

    public SignsDir(string fullDir) : base(fullDir)
    {
        InitContainer();
    }

    private void InitContainer()
    {
        foreach (var dir in Directory.GetDirectories(GetSysAbsPath()))
        {
            string dirName = Path.GetFileName(dir);
            if (!dirName.StartsWith(SignDir.SignContainerPrefix, StringComparison.Ordinal)) continue;
            string numb = dirName.Replace(SignDir.SignContainerPrefix, "");
            // 容错：Java 对非法目录名抛 NumberFormatException，此处跳过非法项
            if (!int.TryParse(numb, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num)) continue;
            if (_maxSignIndex <= num) _maxSignIndex = num + 1;
        }
    }

    /// <summary>获取签名列表文件（不存在抛 FileNotFoundException）。</summary>
    public SignaturesType GetSignatures() => new(GetObjByFile(SignaturesFileName));

    /// <summary>设置签名列表文件。</summary>
    public SignsDir SetSignatures(SignaturesType signatures)
    {
        PutObj(SignaturesFileName, signatures);
        return this;
    }

    /// <summary>创建新的签名/章虚拟容器（Sign_{maxIndex}）。</summary>
    public SignDir NewSignDir()
    {
        string name = SignDir.SignContainerPrefix + _maxSignIndex.ToString(CultureInfo.InvariantCulture);
        _maxSignIndex++;
        return ObtainContainer(name, p => new SignDir(p));
    }

    /// <summary>获取指定签名容器（index 必须大于 0；不存在抛 FileNotFoundException）。</summary>
    public SignDir GetByIndex(int index)
    {
        // 上游语义：index <= 0 视为非法（NumberFormatException）
        if (index <= 0) throw new FormatException("签名容器index必须大于0");
        string containerName = SignDir.SignContainerPrefix + index.ToString(CultureInfo.InvariantCulture);
        return GetContainer(containerName, p => new SignDir(p));
    }

    /// <summary>按容器名获取签名容器（不存在抛 FileNotFoundException）。</summary>
    public SignDir GetSignDir(string containerName) => GetContainer(containerName, p => new SignDir(p));
}
