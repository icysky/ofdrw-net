using Ofd.Core.BasicType;

namespace Ofd.Crypto;

/// <summary>文件在容器中的路径（Java ContainerPath）：容器内路径 + 文件系统绝对路径。</summary>
public class ContainerPath
{
    /// <summary>容器内绝对路径。</summary>
    private readonly string _path;

    /// <summary>文件系统内绝对路径。</summary>
    private readonly string _abs;

    public ContainerPath(string path, string abs)
    {
        _path = path;
        _abs = abs;
    }

    public string GetPath() => _path;

    public string GetAbs() => _abs;

    /// <summary>创建加密后的文件路径（空文件）。</summary>
    public ContainerPath CreateEncryptedFile() =>
        NewDatFile(_path, Path.GetDirectoryName(_abs) ?? throw new InvalidOperationException("文件路径无父目录"));

    /// <summary>
    /// 创建后缀为 .dat 的文件：原文件名全小写 + .dat；重名时追加 _N 序号。
    /// </summary>
    public static ContainerPath NewDatFile(string cAbs, string parent)
    {
        if (cAbs.Length == 0) throw new ArgumentException("容器内路径为空");
        if (cAbs[0] != '/') cAbs = "/" + cAbs;

        if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

        var containerLoc = new ST_Loc(cAbs);
        string originalName = containerLoc.GetFileName();
        int off = originalName.LastIndexOf('.');
        string name = off != -1 ? originalName[..off] : originalName;

        string encFileName = name.ToLowerInvariant() + ".dat";
        string resPath = Path.Combine(parent, encFileName);
        int cnt = 1;
        while (File.Exists(resPath))
        {
            encFileName = name.ToLowerInvariant() + "_" + cnt + ".dat";
            resPath = Path.Combine(parent, encFileName);
            cnt++;
        }

        using (File.Create(resPath)) { }
        return new ContainerPath(containerLoc.Parent() + "/" + encFileName, resPath);
    }
}
