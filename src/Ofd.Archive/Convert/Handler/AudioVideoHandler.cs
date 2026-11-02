using Ofd.Core.BasicStructure.Res;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 9：删除音视频资源及其物理文件（GB/T 42133-2022 6.2.6g）。
/// </summary>
public class AudioVideoHandler : IArchiveHandler
{
    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        var resMgt = reader.GetResMgt();
        if (resMgt == null) return;

        var mediaList = resMgt.GetMultiMedias();
        if (mediaList == null || mediaList.Count == 0) return;

        string workDir = reader.GetWorkDir();
        var toRemove = new List<CT_MultiMedia>();

        foreach (var media in mediaList)
        {
            var type = media.GetType();
            if (type != MediaType.Audio && type != MediaType.Video) continue;

            // 删除物理文件
            var mediaFile = media.GetMediaFile();
            if (mediaFile != null)
            {
                string path = mediaFile.ToString();
                if (path.StartsWith('/')) path = path[1..];
                try
                {
                    string filePath = Path.Combine(workDir, path);
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                catch (IOException)
                {
                    // 忽略
                }
            }

            toRemove.Add(media);
        }

        foreach (var media in toRemove) media.GetProxy().Detach();
    }
}
