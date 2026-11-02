using System.Security.Cryptography;
using Ofd.Core;
using Ofd.Core.BasicType;
using Ofd.Core.Dom;

namespace Ofd.Pkg.Container;

/// <summary>
/// 虚拟容器对象（Java org.ofdrw.pkg.container.VirtualContainer）：
/// 以文件系统目录为载体的容器，元素对象缓存于内存，flush 时仅写回改动过的文件。
/// <para>
/// 改动判定：Java 用 SM3 对序列化字节做摘要并比对；此处用 SHA-256——该哈希
/// <b>仅用于进程内判重、不写入任何产物</b>，算法选择不影响输出字节。
/// </para>
/// </summary>
public class VirtualContainer : IDisposable
{
    /// <summary>文件根路径（完整路径含容器名）。</summary>
    private string _fullPath = "";

    /// <summary>目录名称。</summary>
    private string _name = "";

    /// <summary>所属容器（根容器指向自身）。</summary>
    private VirtualContainer? _parent;

    /// <summary>元素缓存（文件名 → 元素）。</summary>
    private readonly Dictionary<string, DomElement> _fileCache = new(7);

    /// <summary>读取文件时记录的序列化摘要（用于判断改动）。</summary>
    private readonly Dictionary<string, byte[]> _fileSrcHash = new(7);

    /// <summary>子容器缓存。</summary>
    private readonly Dictionary<string, VirtualContainer> _dirCache = new(5);

    /// <summary>容器名称。</summary>
    public string ContainerName => _name;

    /// <summary>所属容器（根容器为自身）。</summary>
    public VirtualContainer Parent => _parent ?? this;

    protected VirtualContainer()
    {
        _parent = this;
    }

    /// <summary>通过完整路径构造虚拟容器（目录不存在则创建）。</summary>
    public VirtualContainer(string fullDir) : this()
    {
        if (fullDir == null) throw new ArgumentException("完整路径(fullDir)为空");
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);
        _fullPath = Path.GetFullPath(fullDir);
        _name = Path.GetFileName(_fullPath);
    }

    /// <summary>在父目录下创建（或打开）名为 dirName 的容器目录。</summary>
    public VirtualContainer(string parent, string dirName) : this()
    {
        if (parent == null) throw new ArgumentException("根路径(parent)为空");
        if (!Directory.Exists(parent)) throw new InvalidOperationException("请传入基础目录路径，而不是文件");
        string fullPath = Path.GetFullPath(Path.Combine(Path.GetFullPath(parent), dirName));
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
        _fullPath = fullPath;
        _name = dirName;
    }

    /// <summary>当前容器完整路径（绝对路径）。</summary>
    public string GetSysAbsPath() => _fullPath;

    /// <summary>容器所处的文件系统路径。</summary>
    public string GetContainerPath() => _fullPath;

    /// <summary>获取该容器所属容器。</summary>
    protected VirtualContainer SetParent(VirtualContainer parent)
    {
        _parent = parent;
        return this;
    }

    /// <summary>容器在 OFD 包内的绝对路径（根为 "/"）。</summary>
    public ST_Loc GetAbsLoc()
    {
        if (_parent == this || _parent == null)
            return new ST_Loc("/");
        return _parent.GetAbsLoc().Cat(_name);
    }

    // ==================== 文件写入 ====================

    /// <summary>向容器加入文件（复制）；返回文件在容器中的绝对路径（null 表示无效输入）。</summary>
    public string? PutFileWithPath(string file)
    {
        if (file == null || !File.Exists(file) || Directory.Exists(file)) return null;
        string fileName = Path.GetFileName(file);
        string target = Path.Combine(_fullPath, fileName);

        // 已存在同名文件：内容一致则复用，否则加时间前缀防冲突（与 Java 一致）
        if (File.Exists(target) || Path.GetFullPath(target) == Path.GetFullPath(file))
        {
            if (File.Exists(target) && FileEquals(target, file))
                return target;
            target = Path.Combine(_fullPath, DateTime.Now.ToString("yyyyMMddHHmmss_") + fileName);
        }
        File.Copy(file, target, overwrite: true);
        return target;
    }

    /// <summary>向容器加入文件。</summary>
    public VirtualContainer PutFile(string file)
    {
        PutFileWithPath(file);
        return this;
    }

    /// <summary>直接写入流内容（同名覆盖）。</summary>
    public VirtualContainer AddRaw(string fileName, Stream input)
    {
        using var outStream = new FileStream(Path.Combine(_fullPath, fileName), FileMode.Create, FileAccess.Write);
        input.CopyTo(outStream);
        return this;
    }

    /// <summary>写入字节数据（同名覆盖）。</summary>
    public void PutFile(string fileName, byte[] data)
    {
        if (data == null || data.Length == 0) data = Array.Empty<byte>();
        File.WriteAllBytes(Path.Combine(_fullPath, fileName), data);
    }

    // ==================== 元素缓存 ====================

    /// <summary>向容器缓存元素对象（不落盘，flush 时写出）。</summary>
    public VirtualContainer PutObj(string fileName, OFDElement? element)
    {
        if (fileName == null || fileName.Length == 0) throw new ArgumentException("文件名不能为空");
        if (element == null) return this;
        _fileCache[fileName] = element.GetProxy();
        return this;
    }

    /// <summary>向容器缓存元素（底层 DomElement）。</summary>
    public VirtualContainer PutObj(string fileName, DomElement? element)
    {
        if (fileName == null || fileName.Length == 0) throw new ArgumentException("文件名不能为空");
        if (element == null) return this;
        _fileCache[fileName] = element;
        return this;
    }

    /// <summary>
    /// 通过路径获取元素（支持相对/绝对路径；绝对路径前缀不匹配返回 null）。
    /// </summary>
    public DomElement? GetObj(ST_Loc? loc)
    {
        if (loc == null || loc.IsEmpty()) return null;
        string[] dst = loc.Parts();

        if (loc.IsRootPath())
        {
            string[] current = GetAbsLoc().Parts();
            for (int i = 0; i < current.Length; i++)
            {
                if (i >= dst.Length || !current[i].Equals(dst[i]))
                    return null; // 处于不同前缀中
            }
            dst = dst.Skip(current.Length).ToArray();
        }
        return GetObjByParts(dst);
    }

    private DomElement? GetObjByParts(string[] relativeDst)
    {
        if (relativeDst == null || relativeDst.Length == 0) return null;
        if (relativeDst.Length == 1)
        {
            if (relativeDst[0].Length == 0) return null;
            try
            {
                return GetObjByFile(relativeDst[0]);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        try
        {
            var child = GetContainer(relativeDst[0], p => new VirtualContainer(p));
            return child.GetObjByParts(relativeDst.Skip(1).ToArray());
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>通过文件名获取元素对象（缓存优先；文件加载后记录摘要）。</summary>
    public DomElement GetObjByFile(string fileName)
    {
        if (fileName == null || fileName.Length == 0) throw new ArgumentException("文件名不能为空");
        if (_fileCache.TryGetValue(fileName, out var cached)) return cached;

        string file = GetFileOrThrow(fileName);
        var element = ElemCup.Inject(file);
        _fileSrcHash[fileName] = ObjectHash(element);
        _fileCache[fileName] = element;
        return element;
    }

    /// <summary>通过路径获取文件（绝对路径前缀不匹配返回 null；空路径返回当前容器）。</summary>
    public string? GetFile(ST_Loc? loc)
    {
        if (loc == null) return null;
        if (loc.IsEmpty()) return GetContainerPath();

        string[] dst = loc.Parts();
        if (loc.IsRootPath())
        {
            string[] current = GetAbsLoc().Parts();
            for (int i = 0; i < current.Length; i++)
            {
                if (i >= dst.Length || !current[i].Equals(dst[i]))
                    return null;
            }
            dst = dst.Skip(current.Length).ToArray();
        }
        if (dst.Length == 0) return GetContainerPath();

        string res = Path.Combine(new[] { _fullPath }.Concat(dst).ToArray());
        return File.Exists(res) ? res : null;
    }

    /// <summary>通过文件名获取文件路径（不存在抛 FileNotFoundException，对应 Java getFile(String)）。</summary>
    public string GetFile(string fileName) => GetFileOrThrow(fileName);

    /// <summary>通过文件名获取文件路径，不存在抛 FileNotFoundException。</summary>
    public string GetFileOrThrow(string fileName)
    {
        if (fileName == null || fileName.Length == 0) throw new ArgumentException("文件名为空");
        string res = Path.Combine(_fullPath, fileName);
        if (Directory.Exists(res) || !File.Exists(res))
            throw new FileNotFoundException($"无法在目录: {_fullPath} 中找到，文件 [ {fileName} ]");
        return res;
    }

    // ==================== 子容器 ====================

    /// <summary>获取（不存在则创建）子容器。</summary>
    public T ObtainContainer<T>(string name, Func<string, T> mapper) where T : VirtualContainer
    {
        if (name == null || name.Length == 0) throw new ArgumentException("容器名称（name）为空");
        if (mapper == null) throw new ArgumentException("容器构建对象（mapper）为空");

        if (_dirCache.TryGetValue(name, out var cached))
            return (T)cached;

        var ct = mapper(Path.Combine(_fullPath, name));
        ct.SetParent(this);
        _dirCache[name] = ct;
        return ct;
    }

    /// <summary>获取子容器（目录不存在抛 FileNotFoundException）。</summary>
    public T GetContainer<T>(string name, Func<string, T> mapper) where T : VirtualContainer
    {
        string p = Path.Combine(_fullPath, name);
        if (!Directory.Exists(p))
            throw new FileNotFoundException("容器内无法找名为：" + name + "目录");

        if (_dirCache.TryGetValue(name, out var cached))
            return (T)cached;

        var ct = mapper(p);
        ct.SetParent(this);
        _dirCache[name] = ct;
        return ct;
    }

    // ==================== 存在性/删除 ====================

    /// <summary>判断文件或缓存对象是否存在。</summary>
    public bool Exist(string? fileName)
    {
        if (fileName == null || fileName.Length == 0) return false;
        if (_fileCache.ContainsKey(fileName)) return true;
        string res = Path.Combine(_fullPath, fileName);
        return !Directory.Exists(res) && File.Exists(res);
    }

    /// <summary>删除当前容器中的文件（含缓存；带前缀校验防路径穿越）。</summary>
    public void DeleteFile(string? fileName)
    {
        if (fileName == null || fileName.Length == 0) return;
        _fileCache.Remove(fileName);
        string res = Path.Combine(_fullPath, fileName);
        if (Path.GetFullPath(res).StartsWith(_fullPath, StringComparison.Ordinal) && File.Exists(res))
            File.Delete(res);
    }

    /// <summary>删除整个容器目录并清空缓存。</summary>
    public void Clean()
    {
        try
        {
            if (Directory.Exists(_fullPath))
                Directory.Delete(_fullPath, recursive: true);
            _fileCache.Clear();
            _dirCache.Clear();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("容器删除异常: " + e.Message);
        }
    }

    // ==================== 落盘 ====================

    /// <summary>将缓存中的对象写入文件系统（仅写改动过的文件），并递归刷新子容器。</summary>
    public void Flush()
    {
        foreach (var (filename, element) in _fileCache)
        {
            if (!FileChanged(filename, element)) continue;
            ElemCup.DumpUpNS(element, Path.Combine(_fullPath, filename));
        }
        foreach (var container in _dirCache.Values)
            container.Flush();

        _fileCache.Clear();
        _dirCache.Clear();
    }

    /// <summary>刷新指定子容器。</summary>
    public VirtualContainer FlushContainerByName(string? name)
    {
        if (name == null || name.Trim().Length == 0) return this;
        if (_dirCache.TryGetValue(name, out var container))
            container.Flush();
        return this;
    }

    /// <summary>刷新指定文件。</summary>
    public VirtualContainer FlushFileByName(string? name)
    {
        if (name == null || name.Trim().Length == 0) return this;
        if (_fileCache.TryGetValue(name, out var element) && FileChanged(name, element))
            ElemCup.DumpUpNS(element, Path.Combine(_fullPath, name));
        return this;
    }

    // ==================== 内部 ====================

    private byte[] ObjectHash(DomElement element) => SHA256.HashData(ElemCup.Dump(element));

    private bool FileChanged(string filename, DomElement element)
    {
        if (!_fileSrcHash.TryGetValue(filename, out var srcHash)) return true;
        return !SHA256.HashData(ElemCup.Dump(element)).SequenceEqual(srcHash);
    }

    private static bool FileEquals(string a, string b)
    {
        var bytesA = File.ReadAllBytes(a);
        var bytesB = File.ReadAllBytes(b);
        return bytesA.SequenceEqual(bytesB);
    }

    /// <summary>关闭容器（等同于 Flush，与 Java close 一致）。</summary>
    public void Dispose() => Flush();
}
