using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicType;
using Ofd.Core.Text;
using Ofd.Core.Text.Font;
using Ofd.Layout.Element;
using Ofd.Reader;
using Ofd.Reader.Extractor;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout;

/// <summary>
/// 文档内容替换（Java DocContentReplace）：按映射替换 TextCode 文本并重算 DeltaX。
/// </summary>
public class DocContentReplace
{
    private readonly OFDDoc _ofdDoc;

    /// <summary>内容替换处理器（扩展点）。</summary>
    private IReplaceTextHandler? _replaceTextHandler;

    /// <summary>过时的字体变换处理器（仅兼容保留）。</summary>
    [Obsolete("请使用 IReplaceTextHandler")]
    private IReplaceTextCgTransformHandler? _replaceTextCgTransformHandler;

    public DocContentReplace(OFDDoc ofdDoc)
    {
        _ofdDoc = ofdDoc ?? throw new ArgumentException("OFDDoc不能为空");
    }

    public DocContentReplace(OFDDoc ofdDoc, IReplaceTextHandler replaceTextHandler) : this(ofdDoc) =>
        _replaceTextHandler = replaceTextHandler;

    /// <summary>替换指定页码的文本。</summary>
    public void ReplaceText(int pageNum, IDictionary<string, string> textMap) =>
        ReplaceText(pageNum, textMap, null);

    /// <summary>替换整个文档的文本。</summary>
    public void ReplaceText(IDictionary<string, string> textMap) => ReplaceText(textMap, null);

    /// <summary>替换指定页码的文本（可指定内容抽取过滤器）。</summary>
    public void ReplaceText(int pageNum, IDictionary<string, string> textMap, IExtractorFilter? filter)
    {
        var extractor = new ContentExtractor(GetReader(), filter);
        ReplaceTextByTextObject(textMap, extractor.GetPageTextObject(pageNum));
    }

    /// <summary>替换整个文档的文本（可指定内容抽取过滤器）。</summary>
    public void ReplaceText(IDictionary<string, string> textMap, IExtractorFilter? filter)
    {
        var extractor = new ContentExtractor(GetReader(), filter);
        ReplaceTextByTextObject(textMap, extractor.ExtractAllTextObject());
    }

    /// <summary>按 TextObject 集合替换文本。</summary>
    protected void ReplaceTextByTextObject(IDictionary<string, string> textMap, List<TextObject>? txtObjectList)
    {
        if (txtObjectList == null || txtObjectList.Count == 0) return;

        foreach (var txtObj in txtObjectList)
        {
            foreach (var txtCode in txtObj.GetTextCodes())
            {
                string? oldText = txtCode.GetContent();
                if (oldText == null || oldText.Trim().Length == 0) continue;

                if (!textMap.TryGetValue(oldText, out var newText) || newText == null) continue;

                // 删除 CGTransform：其中的检索码会去子集化字体文件中取字，
                // 替换后的字可能不在子集内，删除后直接使用 TextCode 内容更可靠
                var cgTransforms = txtObj.GetCGTransforms();
                if (cgTransforms.Count > 0) txtObj.RemoveOFDElemByNames("CGTransform");

                // 原字体（用于计算 DeltaX）
                string fontRefId = txtObj.GetFont()?.GetRefId()?.ToString() ?? "";
                var ctFont = GetReader().GetResMgt().GetFont(fontRefId);

                FontType? font = null;
                CT_CGTransform? cgTransform = null;

                if (_replaceTextHandler != null)
                {
                    cgTransform = _replaceTextHandler.HandleCgTransform(txtObj, newText, ctFont);
                    font = _replaceTextHandler.HandleNewFont(txtObj, newText, ctFont);
                }
#pragma warning disable CS0618
                else if (_replaceTextCgTransformHandler != null)
                {
                    cgTransform = _replaceTextCgTransformHandler.CreateCgTransform(txtObj, newText,
                        ctFont?.GetFontFile());
                }
#pragma warning restore CS0618

                if (cgTransform != null) txtObj.AddCGTransform(cgTransform);

                if (font == null)
                {
                    string fontName = ctFont?.GetFontName() ?? "宋体";
                    font = new FontType(fontName, ctFont?.GetFamilyName());

                    // Times New Roman 需要预设宽度映射表以保证 DeltaX 正确
                    if (fontName == "Times New Roman")
                        font.SetPrintableAsciiWidthMap(Ofd.Font.FontNameExtensions.TimesNewRomanPrintableAsciiWidthMap);
                }

                // 用 Paragraph 重新计算新内容的 DeltaX
                var paragraph = new Paragraph()
                    .SetFontSize(txtObj.GetSize())
                    .SetDefaultFont(font);
                paragraph.SetX(txtCode.GetX() ?? 0).SetY(txtCode.GetY() ?? 0);
                paragraph.Add(newText);

                txtCode.SetContent(newText);
                var span = paragraph.GetContents().FirstOrDefault();
                if (span != null) txtCode.SetDeltaX(span.GetDeltaX());
            }
        }
    }

    public IReplaceTextHandler? GetReplaceTextHandler() => _replaceTextHandler;

    public void SetReplaceTextHandler(IReplaceTextHandler? handler) => _replaceTextHandler = handler;

    [Obsolete("请使用 IReplaceTextHandler")]
    public IReplaceTextCgTransformHandler? GetReplaceTextCgTransformHandler() => _replaceTextCgTransformHandler;

    [Obsolete("请使用 IReplaceTextHandler")]
    public void SetReplaceTextCgTransformHandler(IReplaceTextCgTransformHandler? handler) =>
        _replaceTextCgTransformHandler = handler;

    /// <summary>获取 OFD 解析器。</summary>
    public OFDReader GetReader() =>
        _ofdDoc.GetReader() ?? throw new InvalidOperationException("文档未以修改模式打开，缺少 OFDReader");
}

/// <summary>内容替换处理器（扩展点）。</summary>
public interface IReplaceTextHandler
{
    /// <summary>为替换后的文字构造 CGTransform（默认不构造）。</summary>
    CT_CGTransform? HandleCgTransform(TextObject textObject, string newText, CT_Font? beforeCtFont) => null;

    /// <summary>指定替换文字使用的字体（默认沿用原字体）。</summary>
    FontType? HandleNewFont(TextObject textObject, string newText, CT_Font? beforeCtFont) => null;
}

/// <summary>字体变换类型的替换处理器（已过时，仅兼容保留）。</summary>
[Obsolete("请使用 IReplaceTextHandler")]
public interface IReplaceTextCgTransformHandler
{
    /// <summary>为对应文字构造 CGTransform。</summary>
    CT_CGTransform? CreateCgTransform(TextObject textObject, string newText, ST_Loc? beforeTextFontFile);
}
