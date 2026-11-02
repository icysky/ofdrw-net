using System.Text.RegularExpressions;
using Ofd.Core.BasicStructure.Res;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 3：删除外部资源引用（GB/T 42133-2022 6.1）。
/// <para>http(s) URL 与包外路径的资源条目删除，不下载任何内容。</para>
/// </summary>
public class ExternalResourceHandler : IArchiveHandler
{
    /// <summary>外部 URL 模式。</summary>
    private static readonly Regex ExternalUrl = new("^https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        var resMgt = reader.GetResMgt();
        if (resMgt == null) return;

        string workDir = Path.GetFullPath(reader.GetWorkDir());
        var mediaList = resMgt.GetMultiMedias();
        if (mediaList == null || mediaList.Count == 0) return;

        var toRemove = new List<CT_MultiMedia>();

        foreach (var media in mediaList)
        {
            var mediaFile = media.GetMediaFile();
            if (mediaFile == null) continue;

            string path = mediaFile.ToString();

            // 外部 URL
            if (ExternalUrl.IsMatch(path))
            {
                toRemove.Add(media);
                continue;
            }

            // 包外路径
            string relPath = path.StartsWith('/') ? path[1..] : path;
            string absPath;
            try
            {
                absPath = Path.GetFullPath(Path.Combine(workDir, relPath));
            }
            catch (Exception)
            {
                toRemove.Add(media);
                continue;
            }

            if (!absPath.StartsWith(workDir, StringComparison.OrdinalIgnoreCase))
            {
                toRemove.Add(media);
                try
                {
                    if (File.Exists(absPath)) File.Delete(absPath);
                }
                catch (IOException)
                {
                    // 忽略
                }
            }
        }

        foreach (var media in toRemove) media.GetProxy().Detach();
    }
}
