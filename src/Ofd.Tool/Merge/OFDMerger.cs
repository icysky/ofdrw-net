using System.Security.Cryptography;
using Ofd.Core;
using Ofd.Core.Annotation;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicStructure.Doc;
using Ofd.Core.BasicStructure.PageObj;
using Ofd.Core.BasicStructure.PageTree;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicType;
using Ofd.Core.CompositeObj;
using PageContent = Ofd.Core.BasicStructure.PageObj.Content;
using Ofd.Core.Dom;
using Ofd.Core.PageDescription.Color.ColorSpace;
using Ofd.Core.PageDescription.DrawParam;
using Ofd.Core.Text.Font;
using Ofd.Layout.Engine;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Tool.Merge;

/// <summary>
/// 文档合并工具（Java OFDMerger）：从多个 OFD 中挑选页面，合并为新文档。
/// <para>
/// 合并时会迁移页面内容、模板、注释与资源（字体/图像/绘制参数/颜色空间/矢量图），并重新分配对象 ID。
/// </para>
/// </summary>
public class OFDMerger : IDisposable
{
    /// <summary>新页面列表（每个元素代表新文档中的一页）。</summary>
    public readonly List<PageEntry> PageArr = new(10);

    /// <summary>文档上下文映射（Key：文件名）。</summary>
    private readonly Dictionary<string, DocContext> _docCtxMap = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _dest;

    private BareOFDDoc? _ofdDoc;
    private Annotations? _newDocAnnotations;
    private AnnotsDir? _annotsDir;

    /// <summary>资源文件哈希表（Key：文件哈希，Value：新文档中的文件名）。</summary>
    private readonly Dictionary<string, ST_Loc> _resFileHashTable = new(StringComparer.Ordinal);

    /// <summary>模板页面映射表（Key：原模板页对象 ID）。</summary>
    private readonly Dictionary<string, CT_TemplatePage> _tplPageMap = new(StringComparer.Ordinal);

    private readonly AtomicCounter _resFileCounter = new(0);

    /// <summary>需要迁移的资源引用属性（Font/ResourceID/Substitution/ImageMask/Thumbnail/DrawParam/ColorSpace）。</summary>
    private static readonly string[] ResAttrs =
    {
        "Font", "ResourceID", "Substitution", "ImageMask", "Thumbnail", "DrawParam", "ColorSpace",
    };

    public OFDMerger(string? dest)
    {
        if (dest == null) throw new ArgumentException("合并结果路径(dest)为空");

        string? parent = Path.GetDirectoryName(Path.GetFullPath(dest));
        if (!Directory.Exists(parent))
            throw new ArgumentException($"OFD文件存储路径(dest)上级目录 [{parent}] 不存在");

        _dest = dest;
    }

    /// <summary>添加待合并文档的页面（页码从 1 起；为空表示全部）。</summary>
    public OFDMerger Add(string? filepath, params int[] pageIndexes)
    {
        if (filepath == null) return this;

        var ctx = GetOrLoadContext(filepath);

        int[] indexes = pageIndexes;
        if (indexes == null || indexes.Length == 0)
        {
            int count = ctx.Reader.GetNumberOfPages();
            indexes = Enumerable.Range(1, count).ToArray();
        }

        foreach (int index in indexes) PageArr.Add(new PageEntry(index, ctx));
        return this;
    }

    /// <summary>添加并混合两个文档中的页面（以第一个文档页面为底）。</summary>
    public OFDMerger AddMix(string dstDocFilepath, int dstPageIndex, string tbMixDocFilepath, int tbMixPageIndex) =>
        AddMix(new List<DocPage>
        {
            new(dstDocFilepath, dstPageIndex),
            new(tbMixDocFilepath, tbMixPageIndex),
        });

    /// <summary>添加并混合多个文档页面到一个页面中（第 1 个在最底下）。</summary>
    public OFDMerger AddMix(List<DocPage>? pageList)
    {
        if (pageList == null || pageList.Count == 0) return this;

        PageEntry? newPage = null;
        foreach (var page in pageList)
        {
            if (page.Path == null || page.Index < 1) continue;

            var ctx = GetOrLoadContext(page.Path);

            if (newPage == null)
            {
                newPage = new PageEntry(page.Index, ctx) { TbMixPages = new List<PageEntry>(pageList.Count) };
            }
            else
            {
                newPage.TbMixPages!.Add(new PageEntry(page.Index, ctx));
            }
        }

        if (newPage != null) PageArr.Add(newPage);
        return this;
    }

    /// <summary>按详细条目添加页面。</summary>
    public OFDMerger Add(params PageEntry[]? pages)
    {
        if (pages == null) return this;

        foreach (var page in pages)
        {
            if (page.DocCtx?.Filepath == null) continue;

            string key = Path.GetFileName(page.DocCtx.Filepath);
            _docCtxMap.TryAdd(key, page.DocCtx);

            if (page.TbMixPages != null)
            {
                foreach (var mix in page.TbMixPages)
                {
                    if (mix.DocCtx?.Filepath == null) continue;
                    _docCtxMap.TryAdd(Path.GetFileName(mix.DocCtx.Filepath), mix.DocCtx);
                }
            }

            PageArr.Add(page);
        }

        return this;
    }

    private DocContext GetOrLoadContext(string filepath)
    {
        string key = Path.GetFileName(Path.GetFullPath(filepath));
        if (_docCtxMap.TryGetValue(key, out var ctx)) return ctx;

        ctx = new DocContext(filepath);
        _docCtxMap[key] = ctx;
        return ctx;
    }

    /// <summary>执行合并并打包。</summary>
    public void Close()
    {
        try
        {
            if (PageArr.Count > 0) DoMerge();
        }
        finally
        {
            foreach (var ctx in _docCtxMap.Values)
            {
                try
                {
                    ctx.Dispose();
                }
                catch (Exception)
                {
                    // 忽略关闭异常
                }
            }
        }
    }

    public void Dispose() => Close();

    /// <summary>执行合并。</summary>
    private void DoMerge()
    {
        if (File.Exists(_dest)) File.Delete(_dest);

        using var ofdDoc = new BareOFDDoc(_dest);
        _ofdDoc = ofdDoc;

        var pages = ofdDoc.Document.GetPages();
        if (pages == null)
        {
            pages = new Pages();
            ofdDoc.Document.SetPages(pages);
        }

        var pagesDir = ofdDoc.DocDir.ObtainPages();

        foreach (var pageEntry in PageArr)
        {
            var ctx = pageEntry.DocCtx;
            if (ctx == null || pageEntry.PageIndex == null) continue;

            int pageIndex = pageEntry.PageIndex.Value;

            var defaultArea = new CT_PageArea(ctx.GetDefaultArea(0).GetProxy().Clone());

            ST_ID? oldPageId;
            try
            {
                oldPageId = ctx.Reader.GetPageObjectId(pageIndex);
            }
            catch (Exception)
            {
                continue; // 页码非法
            }
            if (oldPageId == null) continue;

            // 复制页面对象
            var srcPage = ctx.Reader.GetPage(pageIndex);
            var page = new PageObj(srcPage.GetProxy().Clone());

            if (page.GetArea() == null)
                page.SetArea(new CT_PageArea(defaultArea.GetProxy().Clone()));

            // 创建页面容器并登记到 Pages
            var pageDir = pagesDir.NewPageDir();
            string pageLoc = $"Pages/Page_{pageDir.GetIndex()}/Content.xml";
            var newPageItem = new Page(ofdDoc.MaxUnitID.IncrementAndGet(), pageLoc);
            pages.AddPage(newPageItem);

            var newPageId = newPageItem.GetID();

            // 迁移模板
            if (pageEntry.CopyTemplate)
            {
                foreach (var tpl in page.GetTemplates())
                {
                    var tplNewId = PageTplMigrate(ctx, tpl);
                    if (tplNewId != null) tpl.SetTemplateID(tplNewId);
                }
            }

            // 资源迁移与引用替换
            DomMigrate(ctx, page.GetProxy());

            pageDir.SetContent(page);

            // 迁移注释
            if (pageEntry.CopyAnnotations && newPageId != null)
                PageAnnotationMigrate(ctx, oldPageId, newPageId, "Page_" + pageDir.GetIndex());

            // 混合页面
            if (pageEntry.TbMixPages != null)
            {
                foreach (var mixEntry in pageEntry.TbMixPages)
                    MixPage(pageDir, newPageId, page, mixEntry);
            }
        }
    }

    /// <summary>执行混合页面内容（追加到底层页面之上）。</summary>
    private void MixPage(PageDir targetPageDir, ST_ID? newPageId, PageObj targetPageObj, PageEntry beMixEntry)
    {
        var ctx = beMixEntry.DocCtx;
        if (ctx == null || beMixEntry.PageIndex == null) return;

        int pageIndex = beMixEntry.PageIndex.Value;

        ST_ID? oldPageId;
        try
        {
            oldPageId = ctx.Reader.GetPageObjectId(pageIndex);
        }
        catch (Exception)
        {
            return;
        }
        if (oldPageId == null) return;

        var srcPage = ctx.Reader.GetPage(pageIndex);
        var beMixPage = new PageObj(srcPage.GetProxy().Clone());

        // 迁移模板
        if (beMixEntry.CopyTemplate)
        {
            var mixTemplates = beMixPage.GetTemplates();
            foreach (var tpl in mixTemplates)
            {
                var tplNewId = PageTplMigrate(ctx, tpl);
                if (tplNewId != null) tpl.SetTemplateID(tplNewId);
            }

            // 目标页缺失的模板补入
            var targetTemplates = targetPageObj.GetTemplates();
            foreach (var tpl in mixTemplates)
            {
                bool exist = targetTemplates.Any(t => t.GetTemplateID()?.ToString() == tpl.GetTemplateID()?.ToString());
                if (!exist) targetPageObj.AddTemplate(tpl);
            }
        }

        DomMigrate(ctx, beMixPage.GetProxy());

        // 迁移注释
        if (beMixEntry.CopyAnnotations && newPageId != null)
            PageAnnotationMigrate(ctx, oldPageId, newPageId, "Page_" + targetPageDir.GetIndex());

        // 追加内容
        var targetContent = targetPageObj.GetContent();
        if (targetContent == null)
        {
            targetContent = new PageContent();
            targetPageObj.SetContent(targetContent);
        }

        var mixContent = beMixPage.GetContent();
        if (mixContent == null) return;

        foreach (var kid in mixContent.GetProxy().Kids.ToList())
        {
            if (kid is DomElement el) targetContent.GetProxy().Add(el.Clone());
        }
    }

    /// <summary>迁移页面注释到新文档。</summary>
    private void PageAnnotationMigrate(DocContext docCtx, ST_ID oldPageId, ST_ID newPageId, string pageAnnotDirName)
    {
        var ofdDoc = _ofdDoc!;
        var rl = docCtx.Reader.GetResourceLocator();

        try
        {
            rl.Save();
            var srcDoc = docCtx.Reader.CdDefaultDoc();

            var annotListPath = srcDoc.GetAnnotations();
            if (annotListPath == null || !rl.Exist(annotListPath.ToString())) return;

            var annotList = rl.Get(annotListPath, e => new Annotations(e));
            if (annotList == null) return;

            var annPage = annotList.GetByPageId(oldPageId.ToString()!);
            if (annPage?.GetFileLoc() == null) return;

            // 进入注释入口文件所在目录
            rl.Cd(annotListPath.Parent());

            var pageAnnot = rl.Get(annPage.GetFileLoc()!, e => new PageAnnot(e));
            if (pageAnnot == null) return;

            var pageAnnotCopy = new PageAnnot(pageAnnot.GetProxy().Clone());

            if (_newDocAnnotations == null)
            {
                _annotsDir = ofdDoc.DocDir.ObtainAnnots();
                _newDocAnnotations = new Annotations();
                _annotsDir.SetAnnotations(_newDocAnnotations);
                ofdDoc.Document.SetAnnotations(_annotsDir.GetAbsLoc().Cat(DocDir.AnnotationsFileName));
            }

            var pageDir = _annotsDir!.ObtainContainer(pageAnnotDirName, p => new PageDir(p));
            var pageAnnotPath = pageDir.AddAnnot(pageAnnotCopy);

            var annotItem = new AnnPage().SetPageID(newPageId);
            if (pageAnnotPath != null) annotItem.SetFileLoc(pageAnnotPath);
            _newDocAnnotations.AddPage(annotItem);

            // 迁移注释中的资源
            DomMigrate(docCtx, pageAnnotCopy.GetProxy());
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("页面注释迁移失败：" + e.Message);
        }
        finally
        {
            rl.Restore();
        }
    }

    /// <summary>迁移页面模板，返回新文档中的模板引用 ID。</summary>
    private ST_RefID? PageTplMigrate(DocContext docCtx, Template tplObj)
    {
        var ofdDoc = _ofdDoc!;
        string? oldId = tplObj.GetTemplateID()?.ToString();
        if (oldId == null) return null;

        if (_tplPageMap.TryGetValue(oldId, out var cached))
        {
            // 已迁移过且内容一致 → 直接复用
            if (SameXml(cached, docCtx, oldId)) return cached.GetID()?.Ref();
        }

        var entity = docCtx.Reader.GetTemplate(oldId);
        if (entity?.GetPage() == null || entity.GetTplInfo() == null) return null;

        // 复制模板页面
        var pageObj = new PageObj(entity.GetPage()!.GetProxy().Clone());
        var templatePage = entity.GetTplInfo()!;

        // 迁移模板页面资源
        DomMigrate(docCtx, pageObj.GetProxy());

        // 写入模板容器并更新模板信息
        var tplPageLoc = ofdDoc.DocDir.ObtainTemps().Add(pageObj);
        if (tplPageLoc != null) templatePage.SetBaseLoc(tplPageLoc);

        var newId = new ST_ID(ofdDoc.MaxUnitID.IncrementAndGet());
        templatePage.SetID(newId);
        ofdDoc.CData.AddTemplatePage(templatePage);

        _tplPageMap[oldId] = templatePage;
        return newId.Ref();
    }

    /// <summary>比较模板页缓存与当前引用的内容是否一致（简化：按 ID 认为一致）。</summary>
    private static bool SameXml(CT_TemplatePage cached, DocContext docCtx, string oldId)
    {
        var entity = docCtx.Reader.GetTemplate(oldId);
        if (entity?.GetTplInfo() == null) return false;

        return cached.GetProxy().Name == entity.GetTplInfo()!.GetProxy().Name;
    }

    /// <summary>DOM 节点资源迁移：替换资源引用并重新分配对象 ID。</summary>
    private void DomMigrate(DocContext docCtx, DomElement dom)
    {
        foreach (var attrName in ResAttrs)
        {
            foreach (var element in SelfAndDescendants(dom))
            {
                string? oldResId = element.Attr(attrName);
                if (oldResId == null) continue;

                long newResId = ResMigrate(docCtx, oldResId);
                if (newResId > 0) element.SetAttr(attrName, newResId.ToString());
            }
        }

        // 重新分配对象 ID
        foreach (var element in SelfAndDescendants(dom))
        {
            if (element.Attr("ID") == null) continue;
            element.SetAttr("ID", _ofdDoc!.MaxUnitID.IncrementAndGet().ToString());
        }
    }

    /// <summary>从原文档迁移资源到新文档，返回新文档中的资源 ID（0 表示未找到/不支持）。</summary>
    private long ResMigrate(DocContext docCtx, string oldResId)
    {
        var ofdDoc = _ofdDoc!;
        var resObj = docCtx.ResMgt?.Get(oldResId);
        if (resObj == null) return 0;

        // 已迁移缓存
        if (docCtx.ResOldNewMap.TryGetValue(oldResId, out var cachedObj))
            return cachedObj.GetObjID()?.Id ?? 0;

        docCtx.ResOldNewMap[oldResId] = resObj;

        var rl = docCtx.Reader.GetResourceLocator();

        switch (resObj)
        {
            case CT_ColorSpace cs:
            {
                var profile = cs.GetProfile();
                if (profile != null)
                {
                    string filepath = rl.GetFile(profile);
                    cs.SetProfile(CopyResFile(filepath));
                }
                ofdDoc.Prm.AddRawWithCache(cs);
                break;
            }
            case CT_DrawParam dp:
                ofdDoc.Prm.AddRawWithCache(dp);
                break;
            case CT_Font font:
            {
                var fontFile = font.GetFontFile();
                if (fontFile != null)
                {
                    string filepath = rl.GetFile(fontFile);
                    font.SetFontFile(CopyResFile(filepath));
                }
                ofdDoc.Prm.AddRawWithCache(font);
                break;
            }
            case CT_MultiMedia media:
            {
                var mediaFile = media.GetMediaFile();
                if (mediaFile != null)
                {
                    string filepath = rl.GetFile(mediaFile);
                    media.SetMediaFile(CopyResFile(filepath));
                }
                ofdDoc.Prm.AddRawWithCache(media);
                break;
            }
            case CT_VectorG vectorG:
                // 矢量图像内部还可能引用资源，先递归迁移
                DomMigrate(docCtx, vectorG.GetProxy());
                ofdDoc.Prm.AddRawWithCache(vectorG);
                break;
            default:
                // 未知资源类型不迁移
                return 0;
        }

        return resObj.GetObjID()?.Id ?? 0;
    }

    /// <summary>复制资源文件到新文档 Res 目录（按内容哈希去重，文件名用计数器）。</summary>
    private ST_Loc CopyResFile(string filepath)
    {
        string hash;
        using (var sha = SHA256.Create())
        using (var fs = File.OpenRead(filepath))
            hash = Convert.ToHexString(sha.ComputeHash(fs));

        if (_resFileHashTable.TryGetValue(hash, out var cached)) return cached;

        string fileName = Path.GetFileName(filepath);
        int dot = fileName.LastIndexOf('.');
        fileName = dot != -1
            ? _resFileCounter.IncrementAndGet() + fileName[dot..]
            : _resFileCounter.IncrementAndGet().ToString();

        var resDir = _ofdDoc!.DocDir.ObtainRes();
        using (var fs = File.OpenRead(filepath)) resDir.AddRaw(fileName, fs);

        var loc = new ST_Loc(fileName);
        _resFileHashTable[hash] = loc;
        return loc;
    }

    /// <summary>自身与全部后代元素。</summary>
    private static IEnumerable<DomElement> SelfAndDescendants(DomElement root)
    {
        yield return root;
        foreach (var kid in root.Kids)
        {
            if (kid is not DomElement el) continue;
            foreach (var d in SelfAndDescendants(el)) yield return d;
        }
    }
}
