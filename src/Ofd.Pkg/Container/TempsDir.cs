using System.Globalization;
using System.Text.RegularExpressions;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicType;

namespace Ofd.Pkg.Container;

/// <summary>模板文件存放目录（Java TempsDir，GMT0099 OFD 2.0）。</summary>
public class TempsDir : VirtualContainer
{
    public const string TempFilePrefix = "Temp_";

    public static readonly Regex TempFileNameRegex = new(@"Temp_(\d+).xml", RegexOptions.Compiled);

    private int _maxTempIndex = -1;

    public TempsDir(string fullDir) : base(fullDir) { }

    /// <summary>向目录中加入模板文件（复制）。</summary>
    public TempsDir Add(string res)
    {
        if (res == null || !File.Exists(res)) return this;
        PutFile(res);
        return this;
    }

    /// <summary>向目录中加入模板页面，返回模板在容器内的绝对路径。</summary>
    public ST_Loc Add(string fileName, PageObj page)
    {
        PutObj(fileName, page);
        return GetAbsLoc().Cat(fileName);
    }

    /// <summary>向容器内加入模板（自动编号 Temp_N.xml），返回模板容器内绝对路径（null 输入返回 null）。</summary>
    public ST_Loc? Add(PageObj? page)
    {
        if (page == null) return null;
        _maxTempIndex = GetMaxTempIndex() + 1;
        string fileName = TempFilePrefix + _maxTempIndex.ToString(CultureInfo.InvariantCulture) + ".xml";
        return Add(fileName, page);
    }

    /// <summary>根据文件名获取模板页面对象（不存在抛 FileNotFoundException）。</summary>
    public PageObj Get(string fileName) => new(GetObjByFile(fileName));

    /// <summary>获取当前容器内最大的模板文件索引号（无则 -1；结果缓存）。</summary>
    public int GetMaxTempIndex()
    {
        if (_maxTempIndex >= 0) return _maxTempIndex;

        int max = -1;
        foreach (var item in Directory.GetFiles(GetContainerPath()))
        {
            string fileName = Path.GetFileName(item).ToLowerInvariant();
            if (!fileName.StartsWith(TempFilePrefix.ToLowerInvariant(), StringComparison.Ordinal)) continue;
            string numStr = fileName.Replace(TempFilePrefix.ToLowerInvariant(), "").Split('.')[0];
            if (int.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > max)
                max = n;
        }
        _maxTempIndex = max;
        return _maxTempIndex;
    }
}
