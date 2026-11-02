using Ofd.Core;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.PageObj.Layer.Block;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.CompositeObj;
using Ofd.Core.PageDescription.Color.ColorSpace;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Core.Text.Font;

namespace Ofd.Reader;

/// <summary>
/// 资源管理器（只读，Java ResourceManage）：按 ID 随机访问 PublicRes / DocumentRes 中的资源，
/// 返回均为只读副本，且文档内相对路径在加载时统一转换为绝对路径。
/// </summary>
public class ResourceManage
{
    /// <summary>颜色空间。</summary>
    private readonly Dictionary<string, CT_ColorSpace> _colorSpaceMap = new();

    /// <summary>绘制参数。</summary>
    private readonly Dictionary<string, CT_DrawParam> _drawParamMap = new();

    /// <summary>字形。</summary>
    private readonly Dictionary<string, CT_Font> _fontMap = new();

    /// <summary>多媒体对象。</summary>
    private readonly Dictionary<string, CT_MultiMedia> _multiMediaMap = new();

    /// <summary>矢量图像。</summary>
    private readonly Dictionary<string, CT_VectorG> _compositeGraphicUnitMap = new();

    /// <summary>所有资源与 ID 的映射表。</summary>
    private readonly Dictionary<string, OFDElement> _allResMap = new();

    /// <summary>文档公共数据结构。</summary>
    private CT_CommonData? _commonData;

    private readonly OFDReader _ofdReader;

    /// <summary>创建资源管理器（默认加载 Doc_0 的资源）。</summary>
    public ResourceManage(OFDReader ofdReader)
    {
        _ofdReader = ofdReader;
        try
        {
            LoadDefaultDoc();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("文档结构解析异常", e);
        }
    }

    /// <summary>指定文档序号创建资源管理器。</summary>
    public ResourceManage(OFDReader ofdReader, int docNum)
    {
        _ofdReader = ofdReader;
        try
        {
            LoadDoc(docNum);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("文档结构解析异常", e);
        }
    }

    // ==================== 资源访问 ====================

    /// <summary>获取绘制参数（只读副本），不存在返回 null。</summary>
    public CT_DrawParam? GetDrawParam(string id) => _drawParamMap.GetValueOrDefault(id);

    /// <summary>递归解析绘制参数并覆盖继承内容，不存在返回 null。</summary>
    public CT_DrawParam? GetDrawParamFinal(string id) => GetDrawParamFinal(id, new List<string>());

    private CT_DrawParam? GetDrawParamFinal(string id, List<string> visited)
    {
        if (id == null) return null;
        var current = _drawParamMap.GetValueOrDefault(id);
        return SuperDrawParam(current, visited);
    }

    /// <summary>寻找继承属性覆盖当前为空的属性（返回补全后的副本）。</summary>
    public CT_DrawParam? SuperDrawParam(CT_DrawParam? current) => SuperDrawParam(current, new List<string>());

    /// <summary>寻找继承属性覆盖当前为空的属性（带循环引用检测）。</summary>
    public CT_DrawParam? SuperDrawParam(CT_DrawParam? current, List<string> visited)
    {
        if (current == null) return null;

        // 复制为副本防止污染
        current = current.Clone();
        var relative = current.GetRelative();
        if (relative == null) return current;

        string relativeId = relative.ToString();
        if (visited.Contains(relativeId))
        {
            // 发现循环引用，直接返回当前副本
            return current;
        }

        string? currentId = current.GetID()?.ToString();
        if (currentId != null) visited.Add(currentId);

        var parent = GetDrawParamFinal(relativeId, visited);

        if (currentId != null && visited.Count > 0) visited.RemoveAt(visited.Count - 1);

        if (parent == null) return current;

        // 本次绘制属性覆盖其引用的绘制参数中的同名属性
        CopyAttrIfAbsent(current, parent, "LineWidth");
        CopyAttrIfAbsent(current, parent, "Join");
        CopyAttrIfAbsent(current, parent, "Cap");
        CopyAttrIfAbsent(current, parent, "DashOffset");
        CopyAttrIfAbsent(current, parent, "DashPattern");
        CopyAttrIfAbsent(current, parent, "MiterLimit");

        if (current.GetFillColor() == null && parent.GetFillColor() != null)
            current.SetFillColor(parent.GetFillColor());
        if (current.GetStrokeColor() == null && parent.GetStrokeColor() != null)
            current.SetStrokeColor(parent.GetStrokeColor());

        return current;
    }

    /// <summary>
    /// 补充图元信息：将图元引用的绘制参数合并到图元（对应 Java superDrawParam(CT_GraphicUnit)）。
    /// </summary>
    public CT_DrawParam? SuperDrawParam(OFDElement? current)
    {
        if (current == null) return null;

        string? drawParam = current.AttributeValue("DrawParam");
        if (drawParam == null) return null;

        var parent = GetDrawParamFinal(drawParam);
        if (parent == null) return null;

        CopyAttrIfAbsent(current, parent, "LineWidth");
        CopyAttrIfAbsent(current, parent, "Join");
        CopyAttrIfAbsent(current, parent, "Cap");
        CopyAttrIfAbsent(current, parent, "DashOffset");
        CopyAttrIfAbsent(current, parent, "DashPattern");
        CopyAttrIfAbsent(current, parent, "MiterLimit");
        return parent;
    }

    private static void CopyAttrIfAbsent(OFDElement target, OFDElement parent, string name)
    {
        string? value = parent.AttributeValue(name);
        if (target.AttributeValue(name) == null && value != null)
            target.AddAttribute(name, value);
    }

    /// <summary>获取多媒体对象，不存在返回 null。</summary>
    public CT_MultiMedia? GetMultiMedia(string id) => _multiMediaMap.GetValueOrDefault(id);

    /// <summary>获取图片资源的字节内容；非图片/不存在返回 null。</summary>
    public byte[]? GetImageByteArray(string refID)
    {
        var multiMedia = GetMultiMedia(refID);
        if (multiMedia == null) return null;
        if (multiMedia.GetType() != MediaType.Image) return null;

        var loc = multiMedia.GetMediaFile();
        if (loc == null) return null;

        var rl = _ofdReader.GetResourceLocator();
        rl.Save();
        try
        {
            return File.ReadAllBytes(rl.GetFile(loc));
        }
        finally
        {
            rl.Restore();
        }
    }

    // TODO(P4-render): GetImage(refID) / GetImage(ImageObject)（JBIG2 解码与蒙板合成，依赖渲染批次）

    /// <summary>获取字形，不存在返回 null。</summary>
    public CT_Font? GetFont(string id) => _fontMap.GetValueOrDefault(id);

    /// <summary>获取颜色空间；id 为 null 时返回 CommonData 的默认颜色空间。</summary>
    public CT_ColorSpace? GetColorSpace(string? id)
    {
        if (id == null)
        {
            var defaultCS = _commonData?.GetDefaultCS();
            if (defaultCS == null) return null;
            id = defaultCS.ToString();
        }
        return _colorSpaceMap.GetValueOrDefault(id);
    }

    /// <summary>获取矢量图形，不存在返回 null。</summary>
    public CT_VectorG? GetCompositeGraphicUnit(string id) => _compositeGraphicUnitMap.GetValueOrDefault(id);

    /// <summary>获取文档中所有颜色空间。</summary>
    public List<CT_ColorSpace> GetColorSpaces() => new(_colorSpaceMap.Values);

    /// <summary>获取文档中所有绘制参数。</summary>
    public List<CT_DrawParam> GetDrawParams() => new(_drawParamMap.Values);

    /// <summary>获取文档中所有字形。</summary>
    public List<CT_Font> GetFonts() => new(_fontMap.Values);

    /// <summary>获取文档中所有媒体对象。</summary>
    public List<CT_MultiMedia> GetMultiMedias() => new(_multiMediaMap.Values);

    /// <summary>获取文档中所有矢量图形。</summary>
    public List<CT_VectorG> GetCompositeGraphicUnits() => new(_compositeGraphicUnitMap.Values);

    /// <summary>通过 ID 获取任意资源，不存在返回 null。</summary>
    public OFDElement? Get(string id) => _allResMap.GetValueOrDefault(id);

    /// <summary>获取解析器。</summary>
    public OFDReader GetOfdReader() => _ofdReader;

    // ==================== 加载 ====================

    /// <summary>加载指定文档的资源（先清理上一文档的资源）。</summary>
    public ResourceManage LoadDoc(int docNum)
    {
        var ofdDir = _ofdReader.GetOFDDir();
        var ofd = ofdDir.GetOfd();
        var docBody = ofd.GetDocBody(docNum);

        // 每个文档的 ID 体系独立，资源也独立：清理上一文档资源
        _colorSpaceMap.Clear();
        _drawParamMap.Clear();
        _fontMap.Clear();
        _multiMediaMap.Clear();
        _compositeGraphicUnitMap.Clear();

        LoadDocRes(docBody);
        return this;
    }

    private void LoadDefaultDoc()
    {
        var ofdDir = _ofdReader.GetOFDDir();
        var ofd = ofdDir.GetOfd();
        var docBody = ofd.GetDocBody();
        if (docBody == null) throw new BadOFDException("OFD.xml 中不存在任何 DocBody");
        LoadDocRes(docBody);
    }

    private void LoadDocRes(DocBody docBody)
    {
        var rl = _ofdReader.GetResourceLocator();
        try
        {
            rl.Save();
            var docRoot = docBody.GetDocRoot() ?? throw new BadOFDException("DocBody 中 DocRoot 为空");
            var document = rl.Get(docRoot, e => new Document(e));
            rl.Cd(docRoot.Parent());

            var commonData = document.GetCommonData();
            if (commonData == null) throw new BadOFDException("Document.xml 中不存在 CommonData");
            _commonData = new CT_CommonData(commonData.GetProxy().Clone());

            // 公共资源（PublicRes）
            foreach (var pubResLoc in _commonData.GetPublicResList())
                LoadResFile(rl, pubResLoc);

            // 文档资源序列（DocumentRes）
            foreach (var docResLoc in _commonData.GetDocumentResList())
                LoadResFile(rl, docResLoc);
        }
        finally
        {
            rl.Restore();
        }
    }

    /// <summary>加载资源文件中描述的资源对象（异常全部忽略，保证解析继续）。</summary>
    private void LoadResFile(ResourceLocator rl, ST_Loc? resLoc)
    {
        if (resLoc == null) return;

        try
        {
            rl.Save();
            rl.Cd(resLoc.Parent());
            var res = rl.Get(resLoc, e => new Res(e));
            var baseLoc = res.GetBaseLoc();

            foreach (var ofdResource in res.GetResources())
            {
                switch (ofdResource)
                {
                    case ColorSpaces colorSpaces:
                        foreach (var colorSpace in colorSpaces.GetColorSpaces())
                        {
                            var item = new CT_ColorSpace(colorSpace.GetProxy().Clone());
                            if (item.GetProfile() != null)
                                item.SetProfile(Abs(rl, baseLoc, item.GetProfile()!));
                            string id = item.GetID()!.ToString()!;
                            _colorSpaceMap[id] = item;
                            _allResMap[id] = item;
                        }
                        continue;

                    case DrawParams drawParams:
                        foreach (var drawParam in drawParams.GetDrawParams())
                        {
                            var item = drawParam.Clone();
                            string id = item.GetID()!.ToString()!;
                            _drawParamMap[id] = item;
                            _allResMap[id] = item;
                        }
                        continue;

                    case Fonts fonts:
                        foreach (var font in fonts.GetFonts())
                        {
                            var item = new CT_Font(font.GetProxy().Clone());
                            if (item.GetFontFile() != null)
                                item.SetFontFile(Abs(rl, baseLoc, item.GetFontFile()!));
                            string id = item.GetID()!.ToString()!;
                            _fontMap[id] = item;
                            _allResMap[id] = item;
                        }
                        continue;

                    case MultiMedias multiMedias:
                        foreach (var multiMedia in multiMedias.GetMultiMedias())
                        {
                            var item = new CT_MultiMedia(multiMedia.GetProxy().Clone());
                            if (item.GetMediaFile() != null)
                                item.SetMediaFile(Abs(rl, baseLoc, item.GetMediaFile()!));
                            string id = item.GetID()!.ToString()!;
                            _multiMediaMap[id] = item;
                            _allResMap[id] = item;
                        }
                        continue;

                    case CompositeGraphicUnits units:
                        foreach (var vectorG in units.GetCompositeGraphicUnits())
                        {
                            var item = new CT_VectorG(vectorG.GetProxy().Clone());
                            string id = item.GetID()!.ToString()!;
                            _compositeGraphicUnitMap[id] = item;
                            _allResMap[id] = item;
                        }
                        break;
                }
            }
        }
        catch (Exception)
        {
            // 无法解析的资源描述文件忽略，尽力而为（与 Java 一致）
        }
        finally
        {
            rl.Restore();
        }
    }

    /// <summary>获取资源的绝对地址。</summary>
    private static ST_Loc Abs(ResourceLocator rl, ST_Loc? baseLoc, ST_Loc target)
    {
        if (target.IsRootPath()) return target;

        ST_Loc absLoc;
        if (baseLoc != null)
        {
            absLoc = baseLoc.IsRootPath() ? baseLoc : rl.GetAbsTo(baseLoc);
            absLoc = absLoc.Cat(target);
        }
        else
        {
            absLoc = rl.GetAbsTo(target);
        }
        return absLoc;
    }
}
