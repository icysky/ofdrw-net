using System.Globalization;
using System.IO.Compression;
using Ofd.Core.BasicStructure.Ofd;
using Ofd.Core.BasicType;
using Ofd.Core.Crypto.Encryt;

namespace Ofd.Pkg.Container;

/// <summary>
/// OFD 文档虚拟容器（Java OFDDir）：以目录为载体管理 OFD 包结构，
/// 可打包为 OFD（ZIP）文件；工作目录需显式 Flush/Dispose 或 Clean 清理。
/// </summary>
public class OFDDir : VirtualContainer
{
    /// <summary>OFD 文档主入口文件名称。</summary>
    public const string OFDFileName = "OFD.xml";

    /// <summary>解密入口文件名称。</summary>
    public const string EncryptionsFileName = "Encryptions.xml";

    /// <summary>OFD 防止夹带文件名称。</summary>
    public const string OFDEntriesFileName = "OFDEntries.xml";

    /// <summary>最大文档索引 + 1。</summary>
    private int _maxDocIndex;

    /// <summary>新建一个 OFD 文档（使用系统临时目录作为工作空间）。</summary>
    public static OFDDir NewOFD()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "ofd-tmp-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDirectory);
        }
        catch (IOException e)
        {
            throw new InvalidOperationException("无法创建OFD虚拟容器工作空间，原因：" + e.Message, e);
        }
        return new OFDDir(tempDirectory);
    }

    /// <summary>在指定路径创建或读取 OFD 文档容器（目录已存在时视为读取）。</summary>
    public OFDDir(string fullDir) : base(fullDir)
    {
        InitContainer();
    }

    private void InitContainer()
    {
        foreach (var dir in Directory.GetDirectories(GetSysAbsPath()))
        {
            string name = Path.GetFileName(dir);
            if (!name.StartsWith(DocDir.DocContainerPrefix, StringComparison.Ordinal)) continue;
            string numb = name.Replace(DocDir.DocContainerPrefix, "");
            // 容错：非法文档目录名跳过（Java 对非法名抛 NumberFormatException）
            if (!int.TryParse(numb, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num)) continue;
            if (_maxDocIndex <= num) _maxDocIndex = num + 1;
        }
    }

    /// <summary>获取文档主入口文件对象（不存在抛 FileNotFoundException）。</summary>
    public OFD GetOfd() => new(GetObjByFile(OFDFileName));

    /// <summary>设置文档主入口文件对象。</summary>
    public OFDDir SetOfd(OFD ofd)
    {
        PutObj(OFDFileName, ofd);
        return this;
    }

    /// <summary>获取解密入口文件（不存在则创建新的空文件）。</summary>
    public Encryptions ObtainEncryptions()
    {
        var encryptions = GetEncryptions();
        if (encryptions == null)
        {
            encryptions = new Encryptions();
            SetEncryptions(encryptions);
        }
        return encryptions;
    }

    /// <summary>获取解密入口文件，不存在返回 null。</summary>
    public Encryptions? GetEncryptions()
    {
        try
        {
            var obj = GetObjByFile(EncryptionsFileName);
            return new Encryptions(obj);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>设置解密入口文件（null 忽略）。</summary>
    public OFDDir SetEncryptions(Encryptions? encryptions)
    {
        if (encryptions == null) return this;
        PutObj(EncryptionsFileName, encryptions);
        return this;
    }

    /// <summary>新建一个文档容器（Doc_{maxIndex}）。</summary>
    public DocDir NewDoc()
    {
        string name = DocDir.DocContainerPrefix + _maxDocIndex.ToString(CultureInfo.InvariantCulture);
        _maxDocIndex++;
        return ObtainContainer(name, p => new DocDir(p));
    }

    /// <summary>获取指定索引的文档（不存在则创建）。</summary>
    public DocDir ObtainDoc(int index)
    {
        string name = DocDir.DocContainerPrefix + index.ToString(CultureInfo.InvariantCulture);
        if (index >= _maxDocIndex) _maxDocIndex = index + 1;
        return ObtainContainer(name, p => new DocDir(p));
    }

    /// <summary>获取最新（序号最大）的文档目录。</summary>
    public DocDir GetLatestDir()
    {
        string name = DocDir.DocContainerPrefix + (_maxDocIndex - 1).ToString(CultureInfo.InvariantCulture);
        return ObtainContainer(name, p => new DocDir(p));
    }

    /// <summary>通过文档索引获取文档容器（不存在抛 FileNotFoundException）。</summary>
    public DocDir GetDocByIndex(int index)
    {
        string name = DocDir.DocContainerPrefix + index.ToString(CultureInfo.InvariantCulture);
        return GetContainer(name, p => new DocDir(p));
    }

    /// <summary>按容器名获取文档容器（不存在抛 FileNotFoundException）。</summary>
    public DocDir GetDocDir(string name) => GetContainer(name, p => new DocDir(p));

    /// <summary>
    /// 获取默认文档容器：读模式下优先使用 OFD.xml 最后一个 DocBody 的 DocRoot，
    /// 否则取 Doc_0。
    /// </summary>
    public DocDir ObtainDocDefault()
    {
        if (Exist(OFDFileName))
        {
            // 存在 OFD.xml 大概率为读取操作：以最后一个 DocBody 的 DocRoot 作为默认文档
            OFD ofd;
            try
            {
                ofd = GetOfd();
            }
            catch (Exception)
            {
                throw new InvalidOperationException("OFD.xml 文件解析失败");
            }

            var docBodies = ofd.GetDocBodies();
            if (docBodies.Count != 0)
            {
                var docBody = docBodies[^1];
                var docRoot = docBody.GetDocRoot();
                if (docRoot != null)
                    return ObtainContainer(docRoot.Parent(), p => new DocDir(p));
            }
        }
        return ObtainDoc(0);
    }

    /// <summary>
    /// 把文件写入 OFD 虚拟容器（路径不存在则创建），返回文件在容器内的绝对路径。
    /// </summary>
    public ST_Loc PutFileAbs(string absPath, string path)
    {
        if (string.IsNullOrWhiteSpace(absPath)) throw new ArgumentException("文件绝对路径（absPath）为空");
        if (path == null) throw new ArgumentException("文件路径（path）为空");

        string target = Path.GetFullPath(Path.Combine(GetSysAbsPath(), absPath.TrimStart('/', '\\')));
        // 检查路径是否越界
        if (!target.StartsWith(GetContainerPath(), StringComparison.Ordinal))
            throw new ArgumentException("文件路径越界，不能写入到OFD虚拟容器外部");

        string? parent = Path.GetDirectoryName(target);
        if (parent != null) Directory.CreateDirectory(parent);
        File.Copy(path, target, overwrite: true);

        string relPath = Path.GetRelativePath(GetContainerPath(), target).Replace(Path.DirectorySeparatorChar, '/');
        return new ST_Loc("/").Cat(relPath);
    }

    /// <summary>获取 OFD 虚拟容器内的文件路径（不存在不校验，仅返回映射路径）。</summary>
    public string GetFileAbs(string absPath)
    {
        if (string.IsNullOrWhiteSpace(absPath)) throw new ArgumentException("文件绝对路径（absPath）为空");
        string target = Path.GetFullPath(Path.Combine(GetSysAbsPath(), absPath.TrimStart('/', '\\')));
        if (!target.StartsWith(GetContainerPath(), StringComparison.Ordinal))
            throw new ArgumentException("文件路径越界，不能读取OFD虚拟容器外部");
        return target;
    }

    /// <summary>打包成 OFD 并输出到流（先 flush，再以 ZIP 写入）。</summary>
    public void Jar(Stream outStream)
    {
        if (outStream == null) throw new ArgumentException("生成OFD文件输出流（outStream）不能为空");
        Flush();
        using var zip = new ZipArchive(outStream, ZipArchiveMode.Create, leaveOpen: true);
        ZipDir(GetSysAbsPath(), "", zip);
    }

    /// <summary>打包成 OFD 文件（先 flush；同名文件会被删除）。</summary>
    public void Jar(string filePath)
    {
        if (filePath == null) throw new ArgumentException("生成OFD文件路径（filePath）不能为空");
        if (File.Exists(filePath)) File.Delete(filePath);

        Flush();
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        ZipDir(GetSysAbsPath(), "", zip);
    }

    private void ZipDir(string workDirPath, string dir, ZipArchive zip)
    {
        var entries = Directory.GetFileSystemEntries(workDirPath);
        // 空目录直接跳过：资源迁移/合并后可能残留空目录（如 Res/），
        // 原实现直接抛异常会让整个打包失败（Java 原版同样问题）。
        if (entries.Length == 0) return;

        // 与 Java Files.list 顺序一致：先文件系统枚举次序（NTFS 为字典序）
        foreach (var f in entries)
        {
            string entryName = Path.GetFileName(f);
            if (!string.IsNullOrEmpty(dir)) entryName = dir + entryName;

            bool isDir = Directory.Exists(f);
            if (isDir) entryName += "/";

            var now = DateTimeOffset.Now;
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            entry.LastWriteTime = now;

            if (isDir)
            {
                ZipDir(f, entryName, zip);
            }
            else
            {
                using var es = entry.Open();
                using var src = File.OpenRead(f);
                src.CopyTo(es);
            }
        }
    }

    /// <summary>遍历 OFD 包内所有文件（pkgAbsPath 为 Unix 风格包内绝对路径）。</summary>
    public void Walk(IOFDPackageFileIterator iterator)
    {
        if (iterator == null) throw new ArgumentException("包内文件迭代器(iterator)为空");

        string sysRoot = ToUnix(GetSysAbsPath());
        foreach (var file in Directory.EnumerateFiles(GetContainerPath(), "*", SearchOption.AllDirectories))
        {
            string abs = ToUnix(Path.GetFullPath(file)).Replace(sysRoot, "");
            if (!iterator.Visit(abs, file)) break;
        }
    }

    private static string ToUnix(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
}
