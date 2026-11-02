using System.Text;
using System.Text.RegularExpressions;
using Ofd.Archive.Convert.Handler;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert;

/// <summary>
/// OFD-A 转换器（Java OFDArchiveConverter）：普通 OFD → 符合 GB/T 42133-2022 的 OFD-A。
/// <para>
/// 流程：解压 → 顺序执行处理器 → 从 OFD.xml 出发 DFS 收集可达文件 →
/// 按白名单复制到输出目录（排除夹带）→ 打包 → 清理临时目录。
/// </para>
/// </summary>
public class OFDArchiveConverter
{
    /// <summary>处理器管道（顺序敏感）。</summary>
    private readonly List<IArchiveHandler> _handlers;

    /// <summary>解密处理器引用（若管道中存在）。</summary>
    private readonly EncryptionHandler? _encryptionHandler;

    /// <summary>使用默认处理器管道。</summary>
    public OFDArchiveConverter()
    {
        _encryptionHandler = new EncryptionHandler();
        _handlers = LoadDefaultHandlers(_encryptionHandler);
    }

    /// <summary>使用自定义处理器管道。</summary>
    public OFDArchiveConverter(List<IArchiveHandler> handlers)
    {
        _handlers = new List<IArchiveHandler>(handlers);
        _encryptionHandler = handlers.OfType<EncryptionHandler>().FirstOrDefault();
    }

    /// <summary>添加 FEK 解密器（用于加密文档）。</summary>
    public OFDArchiveConverter AddDecryptor(Ofd.Crypto.Decryptor.IUserFekDecryptor decryptor)
    {
        if (_encryptionHandler == null)
            throw new InvalidOperationException(
                "自定义处理器列表中无 EncryptionHandler，无法添加解密器。请在列表中包含 EncryptionHandler 实例。");

        _encryptionHandler.AddDecryptor(decryptor);
        return this;
    }

    /// <summary>把 OFD 文件转换为 OFD-A（源文件不受影响）。</summary>
    public void Convert(string srcPath, string dstPath)
    {
        if (srcPath == null || !File.Exists(srcPath))
            throw new ArgumentException("源文件不存在: " + srcPath);
        if (dstPath == null)
            throw new ArgumentException("输出路径为空");

        string tempDir = Path.Combine(Path.GetTempPath(), "ofd-archive-" + Guid.NewGuid().ToString("N")[..8]);
        string srcDir = Path.Combine(tempDir, "src");
        string outputDir = Path.Combine(tempDir, "output");

        try
        {
            Directory.CreateDirectory(outputDir);

            // 1) 解压到 src/
            ZipUtil.UnZipFiles(srcPath, srcDir + Path.DirectorySeparatorChar);

            // 1.5) 加密包必须在此刻解密：包内没有 OFD.xml（被加密为 ofd.dat），
            //      OFDReader 构造会直接失败，因此解密只能前置。
            _encryptionHandler?.DecryptInPlace(srcDir);

            // 2) 按顺序执行处理器
            using (var reader = new OFDReader(srcDir, false))
            {
                // 关键：处理器经由 reader 访问文档对象，修改落在 reader 内部容器的缓存中，
                // 必须复用同一个 OFDDir 实例 Flush，否则修改不会写回磁盘。
                var ofdDir = reader.GetOFDDir();

                foreach (var handler in _handlers)
                {
                    try
                    {
                        handler.Handle(reader, ofdDir);
                    }
                    catch (Exception ex)
                    {
                        throw new IOException("处理器 " + handler.GetType().Name + " 执行失败: " + ex.Message, ex);
                    }
                }

                // 3) 把内存中的修改写回磁盘
                ofdDir.Flush();
            }

            // 4) 收集可达文件白名单
            var reachable = CollectReachableFiles(srcDir);

            // 4) 按白名单复制（排除非法夹带）
            foreach (var file in reachable)
            {
                string rel = Path.GetRelativePath(srcDir, file);
                string dest = Path.Combine(outputDir, rel);

                string? dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                File.Copy(file, dest, true);
            }

            // 5) 打包
            var outputOfdDir = new OFDDir(outputDir);
            using var fs = File.Create(dstPath);
            outputOfdDir.Jar(fs);
        }
        finally
        {
            // 6) 清理临时目录
            DeleteRecursively(tempDir);
        }
    }

    /// <summary>从 OFD.xml 出发 DFS 收集所有被引用的文件。</summary>
    private static HashSet<string> CollectReachableFiles(string rootDir)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string ofdXml = Path.Combine(rootDir, "OFD.xml");
        if (File.Exists(ofdXml)) DfsCollect(ofdXml, rootDir, visited, reachable);

        return reachable;
    }

    /// <summary>引用路径正则（属性值）：形如 "Doc_0/xxx.xml" 或 "/Doc_0/xxx"。</summary>
    private static readonly Regex AttrPathPattern = new("\"(/?[A-Za-z0-9_][A-Za-z0-9_/\\-]*\\.[A-Za-z0-9]+)\"", RegexOptions.Compiled);

    /// <summary>
    /// 引用路径正则（元素文本）：如 &lt;ofd:DocRoot&gt;Doc_0/Document.xml&lt;/ofd:DocRoot&gt;。
    /// <para>
    /// 注：OFD 的 DocRoot/FileLoc 等以元素文本承载路径，Java 版仅用引号正则，
    /// 会导致白名单只剩 OFD.xml（转换结果缺 Document.xml），此处补齐。
    /// </para>
    /// </summary>
    private static readonly Regex ElementPathPattern =
        new(">\\s*(/?[A-Za-z0-9_][A-Za-z0-9_/\\-]*\\.[A-Za-z0-9]+)\\s*<", RegexOptions.Compiled);

    /// <summary>DFS 收集（仅解析 XML，避免读取大二进制文件）。</summary>
    private static void DfsCollect(string currentFile, string rootDir, HashSet<string> visited, HashSet<string> reachable)
    {
        string full = Path.GetFullPath(currentFile);
        if (!visited.Add(full)) return; // 防循环引用
        reachable.Add(full);

        if (!full.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) return;

        string content;
        try
        {
            content = File.ReadAllText(full, Encoding.UTF8);
        }
        catch (Exception)
        {
            return; // 尽力而为
        }

        // 属性值 + 元素文本两类路径引用
        var candidates = new List<string>();
        foreach (Match m in AttrPathPattern.Matches(content)) candidates.Add(m.Groups[1].Value);
        foreach (Match m in ElementPathPattern.Matches(content)) candidates.Add(m.Groups[1].Value);

        foreach (string raw in candidates)
        {
            string refPath = raw.Trim();

            if (refPath.Length < 2) continue;
            if (refPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
            if (refPath.StartsWith("//", StringComparison.Ordinal)) continue;

            // 路径语义：以 "/" 开头为包内绝对路径（相对包根）；
            // 否则为相对路径（相对**引用文件所在目录**，如 Document.xml 中的 BaseLoc）。
            // Java 原实现一律相对包根解析，导致 BaseLoc="Pages/Page_0/Content.xml" 无法命中。
            string baseDir = refPath.StartsWith('/')
                ? rootDir
                : (Path.GetDirectoryName(full) ?? rootDir);

            if (refPath.StartsWith('/')) refPath = refPath[1..];

            // 候选路径（按优先级）：
            //   ① 引用文件所在目录（Document.xml 的 BaseLoc 等）
            //   ② 其下的 Res 子目录（FontFile / MediaFile 相对 Res 目录，
            //      如 PublicRes.xml 中的 <ofd:FontFile>xxx.ttf</ofd:FontFile> → Doc_0/Res/xxx.ttf）
            //   ③ 包根（部分实现写法）
            var pathCandidates = new List<string>();
            try
            {
                pathCandidates.Add(Path.GetFullPath(Path.Combine(baseDir, refPath)));
                pathCandidates.Add(Path.GetFullPath(Path.Combine(baseDir, "Res", refPath)));
                if (!refPath.StartsWith('/')) pathCandidates.Add(Path.GetFullPath(Path.Combine(rootDir, refPath)));
            }
            catch (Exception)
            {
                continue;
            }

            // 安全检查：必须位于包内且存在
            string? resolved = null;
            string rootFull = Path.GetFullPath(rootDir);
            foreach (var candidate in pathCandidates)
            {
                if (!candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(candidate)) continue;

                resolved = candidate;
                break;
            }

            if (resolved == null) continue;

            if (resolved.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                DfsCollect(resolved, rootDir, visited, reachable);
            else
                reachable.Add(resolved);
        }
    }

    /// <summary>递归删除目录。</summary>
    private static void DeleteRecursively(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch (IOException)
        {
            // 忽略
        }
    }

    /// <summary>
    /// 默认处理器管道。
    /// <para>
    /// 注：解密（EncryptionHandler）与其余处理器随移植进度补齐；当前按已实现顺序编排。
    /// </para>
    /// </summary>
    private static List<IArchiveHandler> LoadDefaultHandlers(EncryptionHandler encHandler) => new()
    {
        // 0. 文件解密（最先执行）
        encHandler,

        // 1. 结构变更
        new SingleDocHandler(),
        new DocTypeHandler(),

        // 2. 元数据清理
        new ExternalResourceHandler(),
        new PermissionHandler(),
        new VPrefsHandler(),

        // 3. 动作清理
        new NonGotoActionHandler(),
        new OutlineActionHandler(),

        // 4. 扩展清理
        new ExtensionHandler(),

        // 5. 资源处理
        new AudioVideoHandler(),
        new ImageExtensionHandler(),
        new ResourcePlacementHandler(),
        new ImageResourceRegHandler(),

        // 6. 内容处理
        new PageBlockFlattenHandler(),
        new ClipAreaHandler(),
        new ImageConvertHandler(),
        new LayerNameHandler(),
        new ImageInterpolateHandler(),

        // 7. 属性清理
        new CleanStrokeAttrHandler(),
        new CleanFillAttrHandler(),

        // 8. 注释处理
        new AnnotationHandler(),

        // 9. 签名/签章去技术化
        new SignatureHandler(),

        // 10. 附件处理
        new AttachmentHandler(),
    };
}
