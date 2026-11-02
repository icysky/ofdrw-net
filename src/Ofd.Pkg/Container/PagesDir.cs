using System.Globalization;

namespace Ofd.Pkg.Container;

/// <summary>页面容器（Java PagesDir）：Page_N/ 子容器集合。</summary>
public class PagesDir : VirtualContainer
{
    /// <summary>最大页面索引 + 1。</summary>
    private int _maxPageIndex;

    public PagesDir(string fullDir) : base(fullDir)
    {
        InitContainer();
    }

    private void InitContainer()
    {
        foreach (var dir in Directory.GetDirectories(GetSysAbsPath()))
        {
            string name = Path.GetFileName(dir);
            if (!name.StartsWith(PageDir.PageContainerPrefix, StringComparison.Ordinal)) continue;
            string numb = name.Replace(PageDir.PageContainerPrefix, "");
            // 容错：非法页面目录名跳过（Java 此处 catch NumberFormatException）
            if (!int.TryParse(numb, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num)) continue;
            if (_maxPageIndex <= num) _maxPageIndex = num + 1;
        }
    }

    /// <summary>创建一个新的页面容器（Page_{maxIndex}）。</summary>
    public PageDir NewPageDir()
    {
        string name = PageDir.PageContainerPrefix + _maxPageIndex.ToString(CultureInfo.InvariantCulture);
        _maxPageIndex++;
        return ObtainContainer(name, p => new PageDir(p));
    }

    /// <summary>获取指定索引的页面容器（页码 = index + 1；不存在抛 FileNotFoundException）。</summary>
    public PageDir GetByIndex(int index)
    {
        string containerName = PageDir.PageContainerPrefix + index.ToString(CultureInfo.InvariantCulture);
        return GetContainer(containerName, p => new PageDir(p));
    }

    /// <summary>按容器名获取页面容器（不存在抛 FileNotFoundException）。</summary>
    public PageDir GetPageDir(string containerName) => GetContainer(containerName, p => new PageDir(p));
}
