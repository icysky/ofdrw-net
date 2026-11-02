using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.Text;
using Ofd.Reader.Extractor;

namespace Ofd.Reader;

/// <summary>内容抽取器（Java ContentExtractor）：按层/页块递归抽取页面文字与文字对象。</summary>
public class ContentExtractor
{
    /// <summary>解析结果接收器（对应 Java ContentExtractor.Receiver）。</summary>
    public delegate void Receiver(int pageNum, List<string> contents);

    private readonly OFDReader _reader;

    /// <summary>文本抽取过滤器（null 表示不过滤）。</summary>
    private readonly IExtractorFilter? _filter;

    /// <summary>构造文字抽取器。</summary>
    public ContentExtractor(OFDReader reader)
    {
        _reader = reader;
    }

    /// <summary>构造文字抽取器（带过滤器）。</summary>
    public ContentExtractor(OFDReader reader, IExtractorFilter filter)
    {
        _reader = reader;
        _filter = filter;
    }

    /// <summary>抽取指定页面内的所有文字（页码从 1 开始）。</summary>
    public List<string> GetPageContent(int pageNum)
    {
        var content = _reader.GetPage(pageNum).GetContent();
        if (content == null) return new List<string>();

        var txtContentList = new List<string>();
        foreach (var layer in content.GetLayers())
            PageBlockHandle(txtContentList, layer.GetPageBlocks());
        return txtContentList;
    }

    /// <summary>抽取指定页面内的所有文字对象（页码从 1 开始）。</summary>
    public List<TextObject> GetPageTextObject(int pageNum)
    {
        var content = _reader.GetPage(pageNum).GetContent();
        if (content == null) return new List<TextObject>();

        var textObjectList = new List<TextObject>();
        foreach (var layer in content.GetLayers())
            PageBlockTextObjectHandle(textObjectList, layer.GetPageBlocks());
        return textObjectList;
    }

    /// <summary>页块递归处理：收集 TextObject 的 TextCode 文本（嵌套 CT_PageBlock 递归）。</summary>
    private void PageBlockHandle(List<string> txtContentList, List<IPageBlockType> pageBlocks)
    {
        foreach (var block in pageBlocks)
        {
            switch (block)
            {
                case TextObject text:
                    foreach (var code in text.GetTextCodes())
                    {
                        if (_filter != null)
                        {
                            string? allowText = _filter.GetAllowText(text, code);
                            if (!string.IsNullOrWhiteSpace(allowText))
                                txtContentList.Add(allowText!);
                        }
                        else
                        {
                            txtContentList.Add(code.GetContent() ?? "");
                        }
                    }
                    break;

                case CT_PageBlock pageBlock:
                    PageBlockHandle(txtContentList, pageBlock.GetPageBlocks());
                    break;
            }
        }
    }

    /// <summary>页块递归处理：收集 TextObject（过滤时只要有一个 TextCode 命中即收集）。</summary>
    private void PageBlockTextObjectHandle(List<TextObject> textObjectList, List<IPageBlockType> pageBlocks)
    {
        foreach (var block in pageBlocks)
        {
            switch (block)
            {
                case TextObject text:
                    if (_filter != null)
                    {
                        foreach (var code in text.GetTextCodes())
                        {
                            string? allowText = _filter.GetAllowText(text, code);
                            if (!string.IsNullOrWhiteSpace(allowText))
                            {
                                textObjectList.Add(text);
                                break;
                            }
                        }
                    }
                    else
                    {
                        textObjectList.Add(text);
                    }
                    break;

                case CT_PageBlock pageBlock:
                    PageBlockTextObjectHandle(textObjectList, pageBlock.GetPageBlocks());
                    break;
            }
        }
    }

    /// <summary>获取 OFD 内所有文本内容。</summary>
    public List<string> ExtractAll()
    {
        int numberOfPages = _reader.GetNumberOfPages();
        var txtContentList = new List<string>();
        for (int pageNum = 1; pageNum <= numberOfPages; pageNum++)
        {
            var pageContent = GetPageContent(pageNum);
            if (pageContent.Count != 0) txtContentList.AddRange(pageContent);
        }
        return txtContentList;
    }

    /// <summary>获取 OFD 内所有文字对象。</summary>
    public List<TextObject> ExtractAllTextObject()
    {
        int numberOfPages = _reader.GetNumberOfPages();
        var txtObjectList = new List<TextObject>();
        for (int pageNum = 1; pageNum <= numberOfPages; pageNum++)
        {
            var pageTextObject = GetPageTextObject(pageNum);
            if (pageTextObject.Count != 0) txtObjectList.AddRange(pageTextObject);
        }
        return txtObjectList;
    }

    /// <summary>遍历所有页面并把每页文本交给接收器。</summary>
    public void Traverse(Receiver? e)
    {
        int numberOfPages = _reader.GetNumberOfPages();
        for (int pageNum = 1; pageNum <= numberOfPages; pageNum++)
        {
            var pageContent = GetPageContent(pageNum);
            if (pageContent.Count != 0 && e != null)
                e(pageNum, pageContent);
        }
    }
}
