using Ofd.Core.BasicStructure.Res;
using Ofd.Core.BasicStructure.Res.Resources;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：把被多个页面共用的栅格图像从 PageRes 迁到 DocumentRes（GB/T 42133-2022 6.5a）。
/// <para>
/// 迁移保持图像对象 ID 与 MediaFile 不变，因此各页面的 ResourceID 引用无需改写；
/// 仅把 <c>MultiMedia</c> 条目从各页 PageRes 的 <c>MultiMedias</c> 容器移入 DocumentRes。
/// </para>
/// </summary>
public class ImageResourceRegHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        var resMgt = reader.GetResMgt();
        if (resMgt == null) return;

        var docDir = ofdDir.ObtainDocDefault();
        var pagesDir = docDir.ObtainPages();

        // 图像 ID → 出现次数；同时记录首次出现的条目（用于迁移）
        var count = new Dictionary<string, int>(StringComparer.Ordinal);
        var firstSeen = new Dictionary<string, CT_MultiMedia>(StringComparer.Ordinal);

        int pageCount = reader.GetNumberOfPages();
        for (int i = 0; i < pageCount; i++)
        {
            Res pageRes;
            try
            {
                var pageDir = pagesDir.GetByIndex(i);
                if (!pageDir.Exist(PageDir.PageResFileName)) continue;
                pageRes = pageDir.GetPageRes();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var media in pageRes.GetResources().OfType<MultiMedias>().SelectMany(c => c.GetMultiMedias()))
            {
                if (media.GetType() != MediaType.Image) continue;

                string? id = media.GetID()?.ToString();
                if (id == null) continue;

                count[id] = count.GetValueOrDefault(id) + 1;
                firstSeen.TryAdd(id, media);
            }
        }

        var sharedIds = count.Where(kv => kv.Value >= 2).Select(kv => kv.Key).ToList();
        if (sharedIds.Count == 0) return;

        var documentRes = docDir.Exist(DocDir.DocumentResFileName)
            ? docDir.GetDocumentRes()
            : CreateDocumentRes(docDir);

        var target = documentRes.GetResources().OfType<MultiMedias>().FirstOrDefault();
        if (target == null)
        {
            target = new MultiMedias();
            documentRes.AddResource(target);
        }

        foreach (var id in sharedIds)
        {
            var media = firstSeen[id];

            // 从原 PageRes 容器摘除，挂到 DocumentRes 的 MultiMedias
            media.GetProxy().Detach();
            target.AddMultiMedia(media);
        }

        docDir.SetDocumentRes(documentRes);
    }

    private static Res CreateDocumentRes(DocDir docDir)
    {
        var res = new Res();
        docDir.SetDocumentRes(res);
        docDir.GetDocument()?.GetCommonData()?
            .SetDocumentRes(docDir.GetAbsLoc().Cat(DocDir.DocumentResFileName));
        return res;
    }
}
