using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器：附件去技术化（GB/T 42133-2022 6.15）—— 仅保留 TXT/XML 文本附件。
/// </summary>
public class AttachmentHandler : IArchiveHandler
{
    /// <summary>可保留的附件格式。</summary>
    private static readonly HashSet<string> KeepFormats = new(StringComparer.OrdinalIgnoreCase) { "TXT", "XML" };

    public void Handle(OFDReader reader, OFDDir ofdDir)
    {
        try
        {
            var attachments = reader.GetAttachmentList();
            if (attachments == null || attachments.Count == 0) return;

            string workDir = reader.GetWorkDir();

            foreach (var att in attachments.ToList())
            {
                string? format = att.GetFormat();
                if (format != null && KeepFormats.Contains(format)) continue; // 文本格式保留

                // 删除附件文件
                var fileLoc = att.GetFileLoc();
                if (fileLoc != null)
                {
                    string path = fileLoc.ToString();
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

                att.GetProxy().Detach();
            }
        }
        catch (Exception e)
        {
            throw new IOException("处理附件时异常: " + e.Message, e);
        }
    }
}
