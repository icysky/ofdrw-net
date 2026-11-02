using Ofd.Core;
using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Core.CompositeObj;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：资源位置修正（GB/T 42133-2022 6.2.6a）。
/// <para>
/// ColorSpace/Font 应在 PublicRes；Image/VectorG/DrawParam 应在 DocumentRes 或 PageRes。
/// 迁移以「资源容器」为单位（保持对象 ID 不变，引用无需改写）；
/// 目标位置已有同类容器时按子元素合并，避免出现重复容器。
/// </para>
/// </summary>
public class ResourcePlacementHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        var docDir = ofdDir.ObtainDocDefault();

        MoveOutOfPublicRes(docDir);
        MoveFromPageResToPublicRes(reader, docDir);
    }

    /// <summary>PublicRes 中的图像/矢量图/绘制参数 → DocumentRes。</summary>
    private static void MoveOutOfPublicRes(DocDir docDir)
    {
        if (!docDir.Exist(DocDir.PublicResFileName)) return;

        Res publicRes;
        try
        {
            publicRes = docDir.GetPublicRes();
        }
        catch (Exception)
        {
            return;
        }

        var movable = publicRes.GetResources()
            .Where(r => r is MultiMedias || r is CompositeGraphicUnits || r is DrawParams)
            .ToList();

        if (movable.Count == 0) return;

        var documentRes = docDir.Exist(DocDir.DocumentResFileName)
            ? docDir.GetDocumentRes()
            : CreateRes(docDir, isPublic: false);

        foreach (var container in movable) MoveContainer(publicRes, documentRes, container);

        docDir.SetPublicRes(publicRes);
        docDir.SetDocumentRes(documentRes);
    }

    /// <summary>各页 PageRes 中的字体/颜色空间 → PublicRes。</summary>
    private static void MoveFromPageResToPublicRes(OFDReader reader, DocDir docDir)
    {
        var pagesDir = docDir.ObtainPages();
        Res? publicRes = null;

        int pageCount = reader.GetNumberOfPages();
        for (int i = 0; i < pageCount; i++)
        {
            PageDir pageDir;
            Res pageRes;

            try
            {
                pageDir = pagesDir.GetByIndex(i);
                if (!pageDir.Exist(PageDir.PageResFileName)) continue;
                pageRes = pageDir.GetPageRes();
            }
            catch (Exception)
            {
                continue;
            }

            var movable = pageRes.GetResources()
                .Where(r => r is Fonts || r is ColorSpaces)
                .ToList();

            if (movable.Count == 0) continue;

            publicRes ??= docDir.Exist(DocDir.PublicResFileName)
                ? docDir.GetPublicRes()
                : CreateRes(docDir, isPublic: true);

            foreach (var container in movable) MoveContainer(pageRes, publicRes, container);

            pageDir.SetPageRes(pageRes);
            docDir.SetPublicRes(publicRes);
        }
    }

    /// <summary>把资源容器从 from 移到 to（目标已有同类容器则合并子元素）。</summary>
    private static void MoveContainer(Res from, Res to, IOFDResource container)
    {
        var source = (OFDElement)container;
        var target = to.GetResources().FirstOrDefault(r => r.GetType() == container.GetType());

        if (target == null)
        {
            source.GetProxy().Detach();
            to.AddResource(container);
            return;
        }

        // 合并：把源容器的子元素整体搬到目标容器
        var targetElement = (OFDElement)target;
        foreach (var kid in source.GetProxy().Kids.OfType<DomElement>().ToList())
        {
            source.GetProxy().Kids.Remove(kid);
            targetElement.GetProxy().Add(kid);
        }

        source.GetProxy().Detach();
    }

    private static Res CreateRes(DocDir docDir, bool isPublic)
    {
        var res = new Res();

        if (isPublic)
        {
            docDir.SetPublicRes(res);
            docDir.GetDocument()?.GetCommonData()?
                .SetPublicRes(docDir.GetAbsLoc().Cat(DocDir.PublicResFileName));
        }
        else
        {
            docDir.SetDocumentRes(res);
            docDir.GetDocument()?.GetCommonData()?
                .SetDocumentRes(docDir.GetAbsLoc().Cat(DocDir.DocumentResFileName));
        }

        return res;
    }
}
