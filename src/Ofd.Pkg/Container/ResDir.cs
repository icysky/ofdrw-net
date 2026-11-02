namespace Ofd.Pkg.Container;

/// <summary>资源目录（Java ResDir）：向目录中加入资源文件（复制语义）。</summary>
public class ResDir : VirtualContainer
{
    public ResDir(string fullDir) : base(fullDir) { }

    /// <summary>
    /// 向目录中加入资源（复制到容器，同名会被重命名）。
    /// </summary>
    public ResDir Add(string res)
    {
        if (res == null || !File.Exists(res)) return this;
        PutFile(res);
        return this;
    }

    /// <summary>
    /// 向目录中加入资源并返回加入后文件路径（同名重命名后可由此取得实际路径）。
    /// </summary>
    public string? AddWithPath(string? res)
    {
        if (res == null || !File.Exists(res)) return null;
        return PutFileWithPath(res);
    }

    /// <summary>获取容器中的资源文件路径（不存在抛 FileNotFoundException）。</summary>
    public string Get(string name) => GetFileOrThrow(name);
}
