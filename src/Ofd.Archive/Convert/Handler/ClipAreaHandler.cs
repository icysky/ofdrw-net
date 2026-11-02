using Ofd.Archive.Check;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 14：裁剪区优化（GB/T 42133-2022 6.3.2a/b）。
/// <para>面积为 0 的裁剪区删除，并把引用它的对象设为 Visible=false。</para>
/// </summary>
public class ClipAreaHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            int pageCount = reader.GetNumberOfPages();
            for (int i = 1; i <= pageCount; i++)
            {
                var page = reader.GetPage(i);
                ProcessPageClips(page.GetProxy());
            }
        }
        catch (Exception e)
        {
            throw new IOException("优化裁剪区时异常: " + e.Message, e);
        }
    }

    /// <summary>处理页面中的裁剪区。</summary>
    private static void ProcessPageClips(DomElement page)
    {
        foreach (var clip in DomWalker.Descendants(page, "ClipArea").ToList())
            CheckClipArea(clip);
    }

    /// <summary>面积 ≤ 0 的裁剪区：删除并置引用对象不可见。</summary>
    private static void CheckClipArea(DomElement clip)
    {
        string? boundaryStr = clip.Attr("Boundary");
        if (boundaryStr == null) return;

        var boundary = ST_Box.GetInstance(boundaryStr);
        if (boundary == null) return;

        if (boundary.Width > 0 && boundary.Height > 0) return;

        string? clipId = clip.Attr("ID");
        var parent = clip.Parent;

        clip.Detach();

        if (parent == null || clipId == null) return;

        foreach (var sibling in parent.Kids.OfType<DomElement>())
        {
            if (sibling.Attr("Clip") == clipId) sibling.SetAttr("Visible", "false");
        }
    }
}
