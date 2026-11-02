using System.IO.Compression;
using System.Text;

namespace Ofd.Reader;

/// <summary>
/// ZIP 解压工具（Java ZipUtil）：解压 OFD 到工作目录，带路径越界防护与容错。
/// </summary>
public static class ZipUtil
{
    /// <summary>解压文件名默认字符集（Java 默认 UTF-8，可通过 SetDefaultCharset 改为 GBK 等）。</summary>
    private static Encoding _charset = new UTF8Encoding(false);

    /// <summary>设置解压默认字符集（用于解决中文文件名乱码）。</summary>
    public static void SetDefaultCharset(string charset)
    {
        _charset = Encoding.GetEncoding(charset);
    }

    /// <summary>获取当前默认字符集名称。</summary>
    public static string GetDefaultCharset() => _charset.WebName;

    /// <summary>解压到指定目录。</summary>
    public static void UnZipFiles(string zipPath, string descDir) => UnZipFiles(new FileInfo(zipPath), descDir);

    /// <summary>解压流到指定目录。</summary>
    public static void UnZipFiles(Stream src, string descDir) => UnZipFileByApacheCommonCompress(src, descDir);

    /// <summary>解压文件到指定目录。</summary>
    public static void UnZipFiles(FileInfo zipFile, string descDir) => UnZipFileByApacheCommonCompress(zipFile, descDir);

    /// <summary>解压文件到指定目录（文件不存在抛 IOException）。</summary>
    public static void UnZipFileByApacheCommonCompress(FileInfo srcFile, string descDir)
    {
        if (srcFile == null || !srcFile.Exists)
            throw new IOException("解压文件不存在: " + srcFile?.FullName);

        using var fin = srcFile.OpenRead();
        UnZipFileByApacheCommonCompress(fin, descDir);
    }

    /// <summary>解压流到指定目录：逐条目校验解析路径不越界后写入。</summary>
    public static void UnZipFileByApacheCommonCompress(Stream src, string descDir)
    {
        if (src == null) throw new ArgumentException("解压源流（src）为空");
        string root = Path.GetFullPath(descDir);
        Directory.CreateDirectory(root);

        try
        {
            using var zip = new ZipArchive(src, ZipArchiveMode.Read, leaveOpen: true, entryNameEncoding: _charset);
            foreach (var entry in zip.Entries)
            {
                string name = entry.FullName.Replace('\\', '/');
                if (string.IsNullOrEmpty(name)) continue;

                string target = Path.GetFullPath(Path.Combine(root, name));
                // 校验路径合法性（防 Zip Slip）
                if (!target.StartsWith(root, StringComparison.Ordinal))
                    throw new IOException($"不合法的路径：{target}");

                bool isDirectory = name.EndsWith("/", StringComparison.Ordinal)
                                   || (entry.Length == 0 && name.EndsWith(".", StringComparison.Ordinal));
                if (isDirectory)
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                string? parent = Path.GetDirectoryName(target);
                if (parent != null) Directory.CreateDirectory(parent);
                using var es = entry.Open();
                using var os = new FileStream(target, FileMode.Create, FileAccess.Write);
                es.CopyTo(os);
            }
        }
        catch (EndOfStreamException)
        {
            // 部分 OFD 压缩包结尾异常，忽略以尽力解压（Java 捕获 EOFException 同理）
        }
    }
}
