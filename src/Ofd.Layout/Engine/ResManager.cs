using System.Text;
using Ofd.Core;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.BasicType;
using Ofd.Core.CompositeObj;
using Ofd.Core.Dom;
using Ofd.Core.PageDescription.Color.ColorSpace;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Core.Text.Font;
using Ofd.Pkg.Container;
using Ofd.Reader;
using FontType = Ofd.Font.Font;

namespace Ofd.Layout.Engine;

/// <summary>
/// 公共资源管理器（Java ResManager）：统一管理文档待加入资源（字体/图片/绘制参数/颜色空间/矢量图），
/// 并通过资源内容哈希去重。
/// </summary>
public class ResManager
{
    /// <summary>新增资源对象 ID。</summary>
    public readonly List<ST_ID> NewResIds = new();

    /// <summary>资源内容哈希 → 对象 ID（用于复用相同资源）。</summary>
    private readonly Dictionary<int, ST_ID> _resObjHash = new();

    private readonly AtomicCounter _maxUnitId;
    private readonly OFDDir? _root;
    private DocDir _docDir;

    private MultiMedias? _medias;
    private DrawParams? _drawParams;
    private Fonts? _fonts;
    private ColorSpaces? _colorSpaces;
    private CompositeGraphicUnits? _compositeGraphicUnits;

    private readonly Document _document;
    private Res? _documentRes;
    private Res? _publicRes;

    /// <summary>创建资源管理器（要求 Doc_N 中存在 Document.xml）。</summary>
    public ResManager(OFDDir? root, DocDir docDir, AtomicCounter maxUnitId)
    {
        _root = root;
        _docDir = docDir;
        _maxUnitId = maxUnitId;

        try
        {
            _document = docDir.GetDocument();
        }
        catch (FileNotFoundException e)
        {
            throw new InvalidOperationException("文档解析失败未能找到 Document.xml", e);
        }

        // 已有的公共资源
        if (docDir.Exist(DocDir.PublicResFileName))
        {
            try
            {
                _publicRes = docDir.GetPublicRes();
                ReloadRes(_publicRes);
            }
            catch (FileNotFoundException)
            {
                // 忽略
            }
        }

        // 已有的文档资源
        if (docDir.Exist(DocDir.DocumentResFileName))
        {
            try
            {
                _documentRes = docDir.GetDocumentRes();
                ReloadRes(_documentRes);
            }
            catch (FileNotFoundException)
            {
                // 忽略
            }
        }
    }

    /// <summary>通过 OFD 解析器创建资源管理器。</summary>
    public ResManager(OFDReader reader)
    {
        _root = reader.GetOFDDir();
        var ofd = _root.GetOfd();
        var rl = reader.GetResourceLocator();

        var docRoot = ofd.GetDocBody().GetDocRoot()
                      ?? throw new InvalidOperationException("OFD 文档缺少 DocRoot");
        _document = rl.Get(docRoot, e => new Document(e));

        var commonData = _document.GetCommonData();
        _docDir = _root.ObtainDocDefault();
        _maxUnitId = new AtomicCounter((int)(commonData?.GetMaxUnitID()?.Id ?? 0));

        try
        {
            rl.Save();
            rl.Cd(_docDir);

            foreach (var loc in commonData?.GetPublicResList() ?? new List<ST_Loc?>())
            {
                if (loc == null) continue;
                _publicRes = rl.Get(loc, e => new Res(e));
                ReloadRes(_publicRes);
            }

            foreach (var loc in commonData?.GetDocumentResList() ?? new List<ST_Loc?>())
            {
                if (loc == null) continue;
                _documentRes = rl.Get(loc, e => new Res(e));
                ReloadRes(_documentRes);
            }
        }
        catch
        {
            // 与 Java 一致：忽略异常
        }
        finally
        {
            rl.Restore();
        }
    }

    /// <summary>重载资源缓存（去除 ID 后按内容做键）。</summary>
    private void ReloadRes(Res res)
    {
        foreach (var resource in res.GetResources())
        {
            if (resource is not OFDElement elem) continue;
            var elements = elem.Elements();
            if (elements.Count == 0) continue;

            foreach (var ctResObj in elements)
            {
                var id = ST_ID.GetInstance(ctResObj.Attr("ID"));
                if (id == null) return;

                // 复制对象、移除 ID 后序列化为 XML 作为键
                var copy = ctResObj.Clone();
                copy.Attr("ID");
                var idAttr = copy.Attrs.FirstOrDefault(a => a.K == "ID");
                if (idAttr.K != null) copy.Attrs.RemoveAll(a => a.K == "ID");

                string key = Encoding.UTF8.GetString(DomSerializer.Dump(copy));
                _resObjHash[key.GetHashCode()] = id;
            }
        }
    }

    /// <summary>增加字体资源（已存在则复用），返回对象 ID。</summary>
    public ST_ID? AddFont(FontType font) => AddFontRet(font).GetObjID();

    /// <summary>增加字体资源并返回字体对象（已存在则复用）。</summary>
    public CT_Font AddFontRet(FontType font)
    {
        string? familyName = font.GetFamilyName();

        var ctFont = new CT_Font()
            .SetFontName(font.GetName() ?? "")
            .SetFamilyName(familyName);

        string? fontFile = font.GetFontFile();
        if (fontFile != null && font.IsEmbeddable())
        {
            // 字体文件加入文档容器
            string added = _docDir.AddResourceWithPath(fontFile)
                           ?? throw new IOException("字体文件加入失败: " + fontFile);

            string filename = Path.GetFileName(added);
            // 资源相对路径不是 Res 时使用绝对路径
            if (_publicRes != null && _publicRes.GetBaseLoc()?.Loc != "Res")
                filename = _docDir.GetAbsLoc().Cat("Res").Cat(filename).ToString();

            ctFont.SetFontFile(filename);
        }

        // 特殊字族属性
        switch ((familyName ?? "").ToLowerInvariant())
        {
            case "serif":
                ctFont.SetSerif(true);
                break;
            case "bold":
                ctFont.SetBold(true);
                break;
            case "italic":
                ctFont.SetItalic(true);
                break;
            case "fixedwidth":
                ctFont.SetFixedWidth(true);
                break;
        }

        AddRawWithCache(ctFont);
        return ctFont;
    }

    /// <summary>加入图片资源（已存在则复用），返回资源 ID。</summary>
    public ST_ID? AddImage(string imgPath)
    {
        string added = _docDir.AddResourceWithPath(imgPath)
                       ?? throw new IOException("图片文件加入失败: " + imgPath);

        string filename = Path.GetFileName(added);
        if (_documentRes != null && _documentRes.GetBaseLoc()?.Loc != "Res")
            filename = _docDir.GetAbsLoc().Cat("Res").Cat(filename).ToString();

        var multiMedia = new CT_MultiMedia()
            .SetType(MediaType.Image)
            .SetFormat(PictureFormat(filename))
            .SetMediaFile(new ST_Loc(filename));

        return AddRawWithCache(multiMedia);
    }

    /// <summary>加入绘制参数（相同内容复用）。</summary>
    public ST_ID? AddDrawParam(CT_DrawParam? param)
    {
        if (param == null) return null;
        return AddRawWithCache(param);
    }

    /// <summary>根据图片文件名推断格式（JPG→JPEG、TIF→TIFF）。</summary>
    private static string PictureFormat(string fileName)
    {
        int idx = fileName.LastIndexOf('.');
        string suffix = (idx >= 0 ? fileName[(idx + 1)..] : fileName).ToUpperInvariant();

        return suffix switch
        {
            "JPG" => "JPEG",
            "TIF" => "TIFF",
            _ => suffix,
        };
    }

    /// <summary>获取（必要时创建）公共资源清单。</summary>
    public Res PubRes()
    {
        if (_publicRes != null) return _publicRes;

        var pubRes = new Res().SetBaseLoc(new ST_Loc("Res"));
        _docDir.SetPublicRes(pubRes);

        var commonData = _document.GetCommonData();
        if (commonData == null)
        {
            commonData = new CT_CommonData();
            _document.SetCommonData(commonData);
        }
        commonData.AddPublicRes(new ST_Loc("PublicRes.xml"));

        _publicRes = pubRes;
        return pubRes;
    }

    /// <summary>获取（必要时创建）文档资源清单。</summary>
    public Res DocRes()
    {
        if (_documentRes != null) return _documentRes;

        var docRes = new Res().SetBaseLoc(new ST_Loc("Res"));
        _docDir.SetDocumentRes(docRes);

        var commonData = _document.GetCommonData();
        if (commonData == null)
        {
            commonData = new CT_CommonData();
            _document.SetCommonData(commonData);
        }
        commonData.AddDocumentRes(new ST_Loc("DocumentRes.xml"));

        _documentRes = docRes;
        return docRes;
    }

    /// <summary>加入资源对象并去重，返回资源 ID。</summary>
    public ST_ID? AddRawWithCache(OFDElement? resObj)
    {
        if (resObj == null) return null;

        // 移除已有 ID 后按内容做键
        resObj.RemoveAttr("ID");
        string key = Encoding.UTF8.GetString(DomSerializer.Dump(resObj.GetProxy()));

        if (_resObjHash.TryGetValue(key.GetHashCode(), out var objId))
        {
            resObj.SetObjID(objId); // 复用已有资源
            return objId;
        }

        objId = new ST_ID(_maxUnitId.IncrementAndGet());
        resObj.SetObjID(objId);
        NewResIds.Add(objId);
        _resObjHash[key.GetHashCode()] = objId;

        AddToProperList(resObj);
        return objId;
    }

    /// <summary>按资源类型加入对应清单。</summary>
    private void AddToProperList(OFDElement resObj)
    {
        switch (resObj)
        {
            case CT_ColorSpace colorSpace:
            {
                var resMenu = PubRes();
                if (_colorSpaces == null)
                {
                    _colorSpaces = new ColorSpaces();
                    resMenu.AddResource(_colorSpaces);
                }
                _colorSpaces.AddColorSpace(colorSpace);
                break;
            }
            case CT_Font font:
            {
                var resMenu = PubRes();
                if (_fonts == null)
                {
                    _fonts = new Fonts();
                    resMenu.AddResource(_fonts);
                }
                _fonts.AddFont(font);
                break;
            }
            case CT_DrawParam drawParam:
            {
                var resMenu = DocRes();
                if (_drawParams == null)
                {
                    _drawParams = new DrawParams();
                    resMenu.AddResource(_drawParams);
                }
                _drawParams.AddDrawParam(drawParam);
                break;
            }
            case CT_MultiMedia multiMedia:
            {
                var resMenu = DocRes();
                if (_medias == null)
                {
                    _medias = new MultiMedias();
                    resMenu.AddResource(_medias);
                }
                _medias.AddMultiMedia(multiMedia);
                break;
            }
            case CT_VectorG vectorG:
            {
                var resMenu = DocRes();
                if (_compositeGraphicUnits == null)
                {
                    _compositeGraphicUnits = new CompositeGraphicUnits();
                    resMenu.AddResource(_compositeGraphicUnits);
                }
                _compositeGraphicUnits.AddCompositeGraphicUnit(vectorG);
                break;
            }
        }
    }

    /// <summary>通过字族名或字体名获取字体对象（不区分大小写），找不到返回 null。</summary>
    public ExistCTFont? GetFont(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        string target = name.ToLowerInvariant();
        Res resMenu = PubRes();
        var found = FindFont(resMenu, target);

        if (found == null)
        {
            resMenu = DocRes();
            found = FindFont(resMenu, target);
        }

        if (found == null) return null;

        // 字体文件绝对路径
        string? absPath = null;
        var loc = found.GetFontFile();
        if (loc != null && _root != null)
        {
            var abs = Abs(resMenu, loc);
            try
            {
                absPath = _root.GetFileAbs(abs.Loc);
            }
            catch (FileNotFoundException)
            {
                // 忽略
            }
        }

        return new ExistCTFont(found, absPath);
    }

    private static CT_Font? FindFont(Res res, string lowerName)
    {
        CT_Font? resFont = null;
        foreach (var fonts in res.GetFonts())
        {
            foreach (var ctFont in fonts.GetFonts())
            {
                string? fontName = ctFont.GetFontName()?.ToLowerInvariant();
                string? familyName = ctFont.GetFamilyName()?.ToLowerInvariant();

                if (lowerName == fontName || lowerName == familyName)
                    resFont = ctFont; // 取最后一个匹配
            }
        }
        return resFont;
    }

    public OFDDir? GetRoot() => _root;

    public void SetRoot(OFDDir root) => throw new NotSupportedException("根容器在构造时确定，运行期不可变更");

    /// <summary>资源完整路径（相对路径拼接资源清单 BaseLoc 与文档路径）。</summary>
    private ST_Loc Abs(Res res, ST_Loc target)
    {
        if (target.IsRootPath()) return target;

        var baseLoc = res.GetBaseLoc();
        ST_Loc absLoc = baseLoc != null && baseLoc.IsRootPath()
            ? baseLoc
            : _docDir.GetAbsLoc().Cat(baseLoc);

        return absLoc.Cat(target);
    }

    /// <summary>获取新加入的资源 ID 列表。</summary>
    public List<ST_ID> GetNewResIds() => NewResIds;

    /// <summary>获取当前操作的文档容器。</summary>
    /// <summary>切换绑定的文档目录（用于与页面解析引擎共用同一容器实例，保证缓存同源）。</summary>
    public ResManager SetDocDir(DocDir docDir)
    {
        if (docDir == null) return this;
        _docDir = docDir;
        return this;
    }

    public DocDir GetDocDir() => _docDir;
}
