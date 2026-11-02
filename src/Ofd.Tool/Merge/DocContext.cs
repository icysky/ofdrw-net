using Ofd.Core;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Layout;
using Ofd.Reader;

namespace Ofd.Tool.Merge;

/// <summary>
/// OFD 文档上下文（Java DocContext）：合并时提供文档、资源与对象 ID 映射。
/// </summary>
public class DocContext : IDisposable
{
    public readonly OFDReader Reader;
    public readonly ResourceManage ResMgt;
    public readonly string Filepath;

    /// <summary>
    /// 新旧映射表：Key = 原文档对象 ID，Value = 资源对象（其 ID 已替换为新文档中的 ID）。
    /// </summary>
    internal readonly Dictionary<string, OFDElement> ResOldNewMap;

    private CT_PageArea? _defaultArea;

    public DocContext(string filepath)
    {
        Filepath = filepath;
        Reader = new OFDReader(filepath);
        ResMgt = Reader.GetResMgt();
        ResOldNewMap = new Dictionary<string, OFDElement>(StringComparer.Ordinal);
    }

    /// <summary>获取文档默认页面区域（缺失时使用 A4）。</summary>
    public CT_PageArea GetDefaultArea(int numOfDoc = 0)
    {
        if (_defaultArea != null) return _defaultArea;

        var document = Reader.GetDoc(numOfDoc);
        _defaultArea = document.GetCommonData()?.GetPageArea();
        if (_defaultArea == null) _defaultArea = PageLayout.A4().GetPageArea();

        return _defaultArea;
    }

    public void Dispose() => Reader.Dispose();
}
