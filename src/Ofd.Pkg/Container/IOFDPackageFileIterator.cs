namespace Ofd.Pkg.Container;

/// <summary>OFD 包内文件迭代器（Java OFDPackageFileIterator，函数式接口）。</summary>
public interface IOFDPackageFileIterator
{
    /// <summary>
    /// 访问文件。
    /// </summary>
    /// <param name="pkgAbsPath">包内绝对路径（Unix 分隔符，以 / 开头）</param>
    /// <param name="path">文件系统中的路径</param>
    /// <returns>true 继续遍历；false 停止遍历</returns>
    bool Visit(string pkgAbsPath, string path);
}
