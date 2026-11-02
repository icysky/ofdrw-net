using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.Text;

namespace Ofd.Reader.Extractor;

/// <summary>文本抽取过滤器（Java ExtractorFilter，函数式接口）。</summary>
public interface IExtractorFilter
{
    /// <summary>
    /// 获取允许返回的文本（返回 null 或空白表示丢弃该 TextCode）。
    /// </summary>
    string? GetAllowText(TextObject textObject, TextCode textCode);
}
