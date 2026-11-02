using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageObj.Layer;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.Text;
using Ofd.Core.Text.Font;
using Ofd.Core.Text.Text;
using Ofd.Font;

namespace Ofd.Reader.Keyword;

/// <summary>
/// 关键字抽取（Java KeywordExtractor）：按关键字在文档中定位其外接矩形（单位 mm）。
/// <para>
/// 支持：同一 TextCode 内匹配、跨 TextCode 的前缀/后缀断字断行匹配、按页码过滤。
/// 字体高度度量由 AWT <c>Font.getStringBounds</c> 改为 <see cref="EnvFont.StrBounds"/>（SkiaSharp）。
/// </para>
/// </summary>
public static class KeywordExtractor
{
    /// <summary>每毫米的 point 单位（1pt ≈ 0.35277778mm）。</summary>
    private const double PointPerMm = 72 / 25.4;

    /// <summary>获取关键字坐标列表（单位 mm）。</summary>
    public static List<KeywordPosition> GetKeyWordPositionList(OFDReader reader, string keyword) =>
        GetKeyWordPositionList(reader, keyword, null);

    /// <summary>获取多个关键字的坐标列表。</summary>
    public static List<KeywordPosition> GetKeyWordPositionList(OFDReader reader, string[] keywords) =>
        GetKeyWordPositionList(reader, keywords, null);

    /// <summary>获取关键字坐标列表（限定页码，从 1 起）。</summary>
    public static List<KeywordPosition> GetKeyWordPositionList(OFDReader reader, string keyword, int[]? pages) =>
        GetKeyWordPositionList(reader, new[] { keyword }, pages);

    /// <summary>获取多个关键字坐标列表（限定页码，从 1 起）。</summary>
    public static List<KeywordPosition> GetKeyWordPositionList(OFDReader reader, string[] keywords, int[]? pages)
    {
        var boundaryMapping = new Dictionary<TextCode, KeywordResource>();

        var locator = reader.GetResourceLocator();
        var ofd = reader.GetOFDDir().GetOfd();
        string docFile = ofd.GetDocBody()?.GetDocRoot()?.Loc ?? throw new InvalidOperationException("OFD 缺少 DocRoot");
        var document = reader.GetDoc(0);
        string dataDir = docFile.Split('/')[0];

        // 字体映射与模板字典
        var fontMapping = GetFontMapping(locator, dataDir, document);
        var templatePage = GetTemplatePage(locator, dataDir, document);

        var textCodeList = new List<TextCode>();
        int numberOfPages = reader.GetNumberOfPages();

        bool hasPageLimit = pages != null && pages.Length > 0;
        if (hasPageLimit)
        {
            foreach (int page in pages!)
            {
                if (page < 1 || page > numberOfPages)
                    throw new ArgumentException($"页码不正确，支持范围[1-{numberOfPages}]");
            }
        }

        for (int page = 1; page <= numberOfPages; page++)
        {
            if (hasPageLimit && !pages!.Contains(page)) continue;

            PreparedContextData(reader, textCodeList, boundaryMapping, fontMapping, templatePage, page);
        }

        var positionList = new List<KeywordPosition>();

        for (int i = 0; i < textCodeList.Count; i++)
        {
            var textCode = textCodeList[i];
            string? content = textCode.GetContent();
            if (string.IsNullOrWhiteSpace(content)) continue;

            foreach (var keyword in keywords)
            {
                int textIndex = content.IndexOf(keyword, StringComparison.Ordinal);
                if (textIndex != -1)
                {
                    // 完整包含关键字
                    AddNormalKeyword(keyword, boundaryMapping, positionList, textCode, textIndex);
                }
                else if (keyword.StartsWith(content, StringComparison.Ordinal) && i != textCodeList.Count - 1)
                {
                    // 前缀匹配（关键字跨 TextCode）
                    AddPrefixBreakTextCodeList(keyword, boundaryMapping, textCodeList, positionList, i, textCode);
                }
                else
                {
                    int startIndex = CheckPostfixMatch(content, keyword);
                    if (startIndex != -1)
                        AddPostfixBreakTextCodeList(keyword, boundaryMapping, textCodeList, positionList, i, startIndex, textCode);
                }
            }
        }

        return positionList;
    }

    /// <summary>检查后缀匹配（关键字起始于 content 的某处并延续到下一个 TextCode）。</summary>
    private static int CheckPostfixMatch(string content, string keyword)
    {
        int startIndex = content.LastIndexOf(keyword[0]);
        if (startIndex == -1) return -1;

        for (int j = startIndex, k = 0; j < content.Length; j++, k++)
        {
            if (k >= keyword.Length) return -1;
            if (content[j] != keyword[k]) return -1;
        }

        return startIndex;
    }

    /// <summary>处理后缀跨 TextCode 匹配。</summary>
    private static void AddPostfixBreakTextCodeList(string keyword,
        Dictionary<TextCode, KeywordResource> boundaryMapping, List<TextCode> textCodeList,
        List<KeywordPosition> positionList, int textCodeIndex, int startIndex, TextCode textCode)
    {
        var mergeTextCodeList = new List<TextCode> { textCode };

        SearchNextText(keyword, textCodeList, mergeTextCodeList, boundaryMapping, textCodeIndex,
            (textCode.GetContent() ?? "")[startIndex..], textCode);

        var builder = string.Concat(mergeTextCodeList.Select(c => c.GetContent() ?? ""));
        if (builder.Contains(keyword, StringComparison.Ordinal))
            MergeKeywordPosition(keyword, startIndex, positionList, mergeTextCodeList, boundaryMapping);
    }

    /// <summary>处理前缀跨 TextCode 匹配。</summary>
    private static void AddPrefixBreakTextCodeList(string keyword,
        Dictionary<TextCode, KeywordResource> boundaryMapping, List<TextCode> textCodeList,
        List<KeywordPosition> positionList, int textCodeIndex, TextCode textCode)
    {
        var mergeTextCodeList = new List<TextCode> { textCode };

        SearchNextText(keyword, textCodeList, mergeTextCodeList, boundaryMapping, textCodeIndex,
            textCode.GetContent() ?? "", textCode);

        var builder = string.Concat(mergeTextCodeList.Select(c => c.GetContent() ?? ""));
        if (builder.Contains(keyword, StringComparison.Ordinal))
            MergeKeywordPosition(keyword, 0, positionList, mergeTextCodeList, boundaryMapping);
    }

    /// <summary>向后检索可拼接的 TextCode（限制同一页）。</summary>
    private static void SearchNextText(string keyword, List<TextCode> textCodeList,
        List<TextCode> mergeTextCodeList, Dictionary<TextCode, KeywordResource> boundaryMapping,
        int textCodeIndex, string firstMatchString, TextCode textCode)
    {
        var mergeText = new System.Text.StringBuilder(firstMatchString);

        if (!boundaryMapping.TryGetValue(textCode, out var kr) || kr == null) return;

        int currentPage = kr.GetPage();
        for (int j = textCodeIndex + 1; j < textCodeList.Count; j++)
        {
            var next = textCodeList[j];
            string nextContent = next.GetContent() ?? "";
            if (nextContent.Trim().Length == 0) continue;

            if (!boundaryMapping.TryGetValue(next, out var nextKr) || nextKr == null) continue;

            // 跨页不拼接
            if (currentPage != nextKr.GetPage()) break;

            mergeText.Append(nextContent);
            string merged = mergeText.ToString();

            if (merged == keyword || merged.StartsWith(keyword, StringComparison.Ordinal))
            {
                mergeTextCodeList.Add(next);
                break;
            }

            if (keyword.StartsWith(merged, StringComparison.Ordinal))
            {
                mergeTextCodeList.Add(next);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>处理正常（单 TextCode 内）关键字。</summary>
    private static void AddNormalKeyword(string keyword, Dictionary<TextCode, KeywordResource> boundaryMapping,
        List<KeywordPosition> positionList, TextCode textCode, int textIndex)
    {
        if (!boundaryMapping.TryGetValue(textCode, out var kr) || kr == null) return;

        var ctText = kr.GetText();
        if (ctText?.GetBoundary() == null) return;

        string content = textCode.GetContent() ?? "";
        bool hasNextKeyword = true;

        while (hasNextKeyword)
        {
            double fHeight = StrHeight(keyword, kr.GetFont(), ctText.GetSize());
            var deltaX = DeltaTool.GetDelta(textCode.GetDeltaX(), content.Length);
            var deltaY = DeltaTool.GetDelta(textCode.GetDeltaY(), content.Length);

            int keywordLength = keyword.Length;
            KeywordPosition position;
            var ctm = ctText.GetCTM();
            if (ctm != null)
            {
                position = GetCtmKeywordPosition(textCode, textIndex, kr.GetPage(), ctText, fHeight, ctm,
                    deltaX, deltaY, keywordLength);
            }
            else
            {
                position = GetKeywordPosition(textCode, textIndex, kr.GetPage(), ctText, fHeight,
                    deltaX, deltaY, keywordLength);
            }

            position.SetKeyword(keyword);
            positionList.Add(position);

            textIndex = content.IndexOf(keyword, textIndex + keywordLength, StringComparison.Ordinal);
            hasNextKeyword = textIndex != -1;
        }
    }

    /// <summary>CTM 变换后的关键字位置。</summary>
    private static KeywordPosition GetCtmKeywordPosition(TextCode textCode, int textIndex, int page,
        CT_Text ctText, double height, ST_Array ctm, List<float> deltaX, List<float> deltaY, int keywordLength)
    {
        double[] matrix = GetMatrix(ctm);
        double x = textCode.GetX() ?? 0d;
        double y = textCode.GetY() ?? 0d;

        for (int i = 0; i < textIndex; i++)
        {
            if (deltaX.Count > i) x += deltaX[i];
            if (deltaY.Count > i) y += deltaY[i];
        }

        double stringWidth = GetStringWidth(textIndex, keywordLength, deltaX, ctText.GetSize());

        var leftTop = Transform(matrix, x, y - height);
        var leftBottom = Transform(matrix, x, y);
        var rightTop = Transform(matrix, x + stringWidth, y - height);
        var rightBottom = Transform(matrix, x + stringWidth, y);

        var ctmBox = MergePos(leftTop, leftBottom, rightTop, rightBottom);

        var boundary = ctText.GetBoundary()!;
        ctmBox.SetTopLeftX(ctmBox.TopLeftX + boundary.TopLeftX);
        ctmBox.SetTopLeftY(ctmBox.TopLeftY + boundary.TopLeftY);

        return new KeywordPosition(page, ctmBox);
    }

    /// <summary>合并坐标点为外接矩形。</summary>
    private static ST_Box MergePos(params ST_Pos[] posList)
    {
        double topLeftX = 0, topLeftY = 0, bottomRightX = 0, bottomRightY = 0;

        for (int i = 0; i < posList.Length; i++)
        {
            var pos = posList[i];
            double px = pos.X;
            double py = pos.Y;

            if (i == 0)
            {
                topLeftX = bottomRightX = px;
                topLeftY = bottomRightY = py;
            }

            if (topLeftX > px) topLeftX = px;
            if (topLeftY > py) topLeftY = py;
            if (bottomRightX < px) bottomRightX = px;
            if (bottomRightY < py) bottomRightY = py;
        }

        return new ST_Box(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);
    }

    /// <summary>矩阵变换。</summary>
    private static ST_Pos Transform(double[] matrix, double sx, double sy)
    {
        double x = matrix[0] * sx + matrix[2] * sy + matrix[4];
        double y = matrix[1] * sx + matrix[3] * sy + matrix[5];
        return new ST_Pos(x, y);
    }

    /// <summary>CTM 数组转 double 矩阵。</summary>
    private static double[] GetMatrix(ST_Array ctm)
    {
        var array = ctm.GetArray();
        var matrix = new double[array.Count];
        for (int i = 0; i < array.Count; i++)
            matrix[i] = double.Parse(array[i], System.Globalization.CultureInfo.InvariantCulture);
        return matrix;
    }

    /// <summary>合并跨 TextCode 的关键字位置。</summary>
    private static void MergeKeywordPosition(string keyword, int firstStartIndex,
        List<KeywordPosition> positionList, List<TextCode> textCodeList,
        Dictionary<TextCode, KeywordResource> boundaryMapping)
    {
        var boxList = new List<ST_Box>();
        int page = 0, totalLength = 0, keywordLength = keyword.Length;

        for (int i = 0; i < textCodeList.Count; i++)
        {
            var textCode = textCodeList[i];
            string content = textCode.GetContent() ?? "";
            int textLength = content.Length;

            if (!boundaryMapping.TryGetValue(textCode, out var kr) || kr == null) continue;

            var ctText = kr.GetText();
            if (page == 0) page = kr.GetPage();

            if (ctText?.GetBoundary() == null) continue;

            if (i == 0 && firstStartIndex > 0)
            {
                textLength = totalLength = content.Length - firstStartIndex;
            }
            else
            {
                if (totalLength + textLength > keywordLength) textLength = keywordLength - totalLength;
                else totalLength += content.Length;
            }

            var deltaX = DeltaTool.GetDelta(textCode.GetDeltaX(), content.Length);
            var deltaY = DeltaTool.GetDelta(textCode.GetDeltaY(), content.Length);

            double width = i == 0 && firstStartIndex > 0
                ? GetStringWidth(firstStartIndex, textLength, deltaX, ctText.GetSize())
                : GetStringWidth(0, textLength, deltaX, ctText.GetSize());

            if (width == 0) width = ctText.GetSize();

            double height = StrHeight(keyword, kr.GetFont(), ctText.GetSize());

            var ctm = ctText.GetCTM();
            if (ctm != null)
            {
                double[] matrix = GetMatrix(ctm);
                double x = textCode.GetX() ?? 0d;
                double y = textCode.GetY() ?? 0d;

                if (i == 0 && firstStartIndex > 0)
                {
                    for (int j = 0; j < firstStartIndex && j < deltaX.Count; j++) x += deltaX[j];
                    for (int j = 0; j < firstStartIndex && j < deltaY.Count; j++) y += deltaY[j];
                }

                var leftBottom = Transform(matrix, x, y);
                var rightTop = Transform(matrix, x + width, y - height);

                var position = ctText.GetBoundary()!.GetTopLeftPos();
                var box = MergePos(leftBottom, rightTop);
                box.SetTopLeftX((position.X) + box.TopLeftX);
                box.SetTopLeftY((position.Y) + box.TopLeftY);

                boxList.Add(box);
            }
            else
            {
                var basePoint = GetLeftBottomPos(ctText.GetBoundary()!, textCode, deltaX, deltaY, 0);

                if (i == 0 && firstStartIndex > 0)
                {
                    for (int j = 0; j < firstStartIndex && j < deltaX.Count; j++)
                        basePoint.SetX(basePoint.X + deltaX[j]);
                    for (int j = 0; j < firstStartIndex && j < deltaY.Count; j++)
                        basePoint.SetY(basePoint.Y + deltaY[j]);
                }

                boxList.Add(new ST_Box(basePoint.X, basePoint.Y - height, width, height));
            }
        }

        if (boxList.Count == 0) return;

        var result = new KeywordPosition(page, MergeBox(boxList));
        result.SetKeyword(keyword);
        positionList.Add(result);
    }

    /// <summary>合并多个外接矩形。</summary>
    private static ST_Box MergeBox(List<ST_Box> boxList)
    {
        double topLeftX = 0, topLeftY = 0, bottomRightX = 0, bottomRightY = 0;

        for (int i = 0; i < boxList.Count; i++)
        {
            var box = boxList[i];
            if (i == 0)
            {
                topLeftX = box.TopLeftX;
                topLeftY = box.TopLeftY;
            }
            else
            {
                if (box.TopLeftX < topLeftX) topLeftX = box.TopLeftX;
                if (box.TopLeftY < topLeftY) topLeftY = box.TopLeftY;
            }

            if (box.TopLeftX + box.Width > bottomRightX) bottomRightX = box.TopLeftX + box.Width;
            if (box.TopLeftY + box.Height > bottomRightY) bottomRightY = box.TopLeftY + box.Height;
        }

        return new ST_Box(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);
    }

    /// <summary>获取模板页数据（模板ID → 模板页对象）。</summary>
    private static Dictionary<ST_ID, PageObj> GetTemplatePage(ResourceLocator locator, string dataDir, Core.BasicStructure.Doc.Document document)
    {
        var templatePage = new Dictionary<ST_ID, PageObj>();
        var commonData = document.GetCommonData();
        if (commonData == null) return templatePage;

        foreach (var tp in commonData.GetTemplatePages())
        {
            var baseLoc = tp.GetBaseLoc();
            var id = tp.GetID();
            if (baseLoc == null || id == null) continue;

            var page = locator.Get(dataDir + "/" + baseLoc.Loc, e => new PageObj(e));
            templatePage[id] = page;
        }

        return templatePage;
    }

    /// <summary>非 CTM 情况下的关键字位置。</summary>
    private static KeywordPosition GetKeywordPosition(TextCode textCode, int textIndex, int page,
        CT_Text ctText, double lineHeight, List<float> deltaX, List<float> deltaY, int keywordLength)
    {
        var position = ctText.GetBoundary()!.GetTopLeftPos();

        double positionX = position.X;
        double positionY = position.Y;

        double x = (textCode.GetX() ?? 0d) + positionX;
        double y = (textCode.GetY() ?? 0d) + positionY;

        double maxX = double.MinValue, maxY = double.MinValue;
        double minX = double.MaxValue, minY = double.MaxValue;

        for (int i = 0; i < textIndex + keywordLength; i++)
        {
            if (i >= textIndex)
            {
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
            }

            if (deltaX.Count > i) x += deltaX[i];
            if (deltaY.Count > i) y += deltaY[i];
        }

        double w = maxX - minX + ctText.GetSize();
        double h = maxY - minY + lineHeight;
        double tlx = minX;
        double tly = minY - lineHeight;

        return new KeywordPosition(page, new ST_Box(tlx, tly, w, h));
    }

    /// <summary>字体行高（mm）。</summary>
    private static double StrHeight(string str, CT_Font? ctFont, double sizeMm)
    {
        string name = ctFont?.GetFontName() ?? "黑体";
        var bounds = EnvFont.StrBounds(name, ctFont?.GetFamilyName(), str, sizeMm);
        return bounds.Height;
    }

    /// <summary>计算文本宽度（mm）：起始字号 + 若干 deltaX。</summary>
    private static double GetStringWidth(int textIndex, int keywordLength, List<float> deltaX, double fontSize)
    {
        double width = fontSize;
        for (int i = textIndex; i < textIndex + keywordLength - 1 && i < deltaX.Count; i++)
            width += deltaX[i];
        return width;
    }

    /// <summary>获取文字定位左下角坐标。</summary>
    private static ST_Pos GetLeftBottomPos(ST_Box boundary, TextCode textCode,
        List<float> deltaX, List<float> deltaY, int textIndex)
    {
        var position = boundary.GetTopLeftPos();

        double x = (textCode.GetX() ?? 0d) + (position.X);
        double y = (textCode.GetY() ?? 0d) + (position.Y);

        for (int i = 0; i < textIndex; i++)
        {
            if (deltaX.Count > i) x += deltaX[i];
            if (deltaY.Count > i) y += deltaY[i];
        }

        return ST_Pos.GetInstance(x, y);
    }

    /// <summary>预处理指定页面：收集 TextCode 与其字体/页码映射（含模板层）。</summary>
    private static void PreparedContextData(OFDReader reader, List<TextCode> textCodeList,
        Dictionary<TextCode, KeywordResource> boundaryMapping, Dictionary<ST_ID, CT_Font> fontMapping,
        Dictionary<ST_ID, PageObj> templatePageMap, int pageNumber)
    {
        var page = reader.GetPage(pageNumber);
        var layers = new List<CT_Layer>();

        var content = page.GetContent();
        if (content != null) layers.AddRange(content.GetLayers());

        // 叠加模板页图层
        foreach (var tpl in page.GetTemplates())
        {
            var templateId = tpl?.GetTemplateID()?.GetRefId();
            if (templateId == null || !templatePageMap.TryGetValue(templateId, out var templatePage)) continue;

            var tplContent = templatePage?.GetContent();
            if (tplContent != null) layers.AddRange(tplContent.GetLayers());
        }

        foreach (var layer in layers)
            PageBlockHandle(textCodeList, boundaryMapping, fontMapping, pageNumber, layer.GetPageBlocks());
    }

    /// <summary>递归处理页块中的文字对象。</summary>
    private static void PageBlockHandle(List<TextCode> textCodeList,
        Dictionary<TextCode, KeywordResource> boundaryMapping, Dictionary<ST_ID, CT_Font> fontMapping,
        int pageNumber, List<IPageBlockType> pageBlocks)
    {
        foreach (var block in pageBlocks)
        {
            if (block is TextObject text)
            {
                var fontId = text.GetFont()?.GetRefId();
                CT_Font? font = null;
                if (fontId != null) fontMapping.TryGetValue(fontId, out font);

                foreach (var code in text.GetTextCodes())
                {
                    var kr = new KeywordResource();
                    kr.SetPage(pageNumber);
                    kr.SetFont(font);
                    kr.SetText(text);

                    textCodeList.Add(code);
                    boundaryMapping[code] = kr;
                }
            }
            else if (block is CT_PageBlock ctPageBlock)
            {
                PageBlockHandle(textCodeList, boundaryMapping, fontMapping, pageNumber, ctPageBlock.GetPageBlocks());
            }
        }
    }

    /// <summary>获取字体映射（公共资源中的字体，ID → CT_Font）。</summary>
    private static Dictionary<ST_ID, CT_Font> GetFontMapping(ResourceLocator locator, string dataDir,
        Core.BasicStructure.Doc.Document document)
    {
        var fontMapping = new Dictionary<ST_ID, CT_Font>();

        var publicResLoc = document.GetCommonData()?.GetPublicRes();
        if (publicResLoc == null) return fontMapping;

        var res = locator.Get(dataDir + "/" + publicResLoc.Loc, e => new Res(e));
        foreach (Fonts fonts in res.GetFonts())
        {
            foreach (var ctFont in fonts.GetFonts())
            {
                var id = ctFont.GetID();
                if (id != null) fontMapping[id] = ctFont;
            }
        }

        return fontMapping;
    }
}
