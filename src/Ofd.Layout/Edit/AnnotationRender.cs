using Ofd.Core.Annotation;
using Ofd.Core.Annotation.PageAnnot;
using Ofd.Core.BasicType;
using Ofd.Layout.Element.Canvas;
using Ofd.Layout.Engine;
using Ofd.Layout.Engine.Render;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Layout.Edit;

/// <summary>
/// 注释渲染器（Java AnnotationRender）：把注释绘制到页面的分页注释文件（Annots/）。
/// </summary>
public class AnnotationRender
{
    private readonly DocDir _docDir;

    /// <summary>注释目录（可能为空，为空时创建）。</summary>
    private AnnotsDir? _annotsDir;

    /// <summary>注释列表文件。</summary>
    private Annotations? _annotations;

    private ResourceLocator? _rl;

    private readonly ResManager _prm;
    private readonly AtomicCounter _maxUnitId;

    public AnnotationRender(DocDir docDir, ResManager prm, AtomicCounter maxUnitId)
    {
        _docDir = docDir;
        _prm = prm;
        _maxUnitId = maxUnitId;
    }

    /// <summary>初始化注释文件结构（入口文件不存在则创建）。</summary>
    private void Init()
    {
        if (_annotations != null) return;

        Ofd.Core.BasicStructure.Doc.Document document;
        try
        {
            document = _docDir.GetDocument();
        }
        catch (Exception e)
        {
            throw new BadOFDException("错误的OFD结构无法获取到Document.xml文件", e);
        }

        _rl = new ResourceLocator(_docDir);

        var annListFileLoc = document.GetAnnotations();
        if (annListFileLoc != null && !_rl.Exist(annListFileLoc.ToString()))
            annListFileLoc = null;

        if (annListFileLoc != null)
        {
            try
            {
                _annotations = _rl.Get(annListFileLoc, e => new Annotations(e));

                string parent = annListFileLoc.Parent();
                _annotsDir = parent.ToLowerInvariant().EndsWith(DocDir.AnnotsDir.ToLowerInvariant())
                    ? _docDir.ObtainAnnots()
                    : null;

                _rl.Cd(parent);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                // 无法解析则重建
            }
        }

        if (_annotations == null)
        {
            _annotsDir = _docDir.ObtainAnnots();
            _annotations = new Annotations();
            _annotsDir.PutObj(DocDir.AnnotationsFileName, _annotations);
            document.SetAnnotations(_annotsDir.GetAbsLoc().Cat(DocDir.AnnotationsFileName));
        }
    }

    /// <summary>渲染注释到指定页面。</summary>
    public void Render(PageInfo pageInfo, Annotation build)
    {
        var drawer = build.GetDrawer();
        if (drawer == null) return;

        Init();

        var id = pageInfo.GetId();
        if (id == null) throw new RenderException("页面缺少对象ID，无法渲染注释");

        // 查找已存在的页面注释条目
        var record = _annotations!.GetByPageId(id.ToString()!);
        PageAnnot? pageAnnot = null;

        if (record == null)
        {
            record = new AnnPage().SetPageID(id);
            _annotations.AddPage(record);
        }
        else
        {
            var annPageFileLoc = record.GetFileLoc();
            try
            {
                pageAnnot = annPageFileLoc == null ? null : _rl!.Get(annPageFileLoc, e => new PageAnnot(e));
            }
            catch (Exception)
            {
                pageAnnot = null;
            }
        }

        // 分页注释文件不存在则创建
        if (pageAnnot == null)
        {
            _annotsDir ??= _docDir.ObtainAnnots();

            string containerName = PageDir.PageContainerPrefix + pageInfo.GetPageN();
            var pageDir = _annotsDir.ObtainContainer(containerName, p => new PageDir(p));

            pageAnnot = new PageAnnot();
            var pageAnnotLoc = pageDir.AddAnnot(pageAnnot);
            if (pageAnnotLoc != null) record.SetFileLoc(pageAnnotLoc);
        }

        // 创建注释对象并分配 ID
        var annot = build.Build();
        annot.SetId(new ST_ID(_maxUnitId.IncrementAndGet()));
        pageAnnot.AddAnnot(annot);

        // 绘制注解内容
        var appearance = annot.GetAppearance();
        if (appearance == null) return;

        var box = new ST_Box(0, 0, build.GetBoundary().Width, build.GetBoundary().Height);
        using var ctx = new DrawContext(appearance, box, _maxUnitId, _prm);
        drawer.Draw(ctx);
    }
}
