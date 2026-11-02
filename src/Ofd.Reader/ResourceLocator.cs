using Ofd.Core.BasicType;
using Ofd.Core.Dom;
using Ofd.Pkg.Container;

namespace Ofd.Reader;

/// <summary>
/// 资源定位器（Java ResourceLocator）：以“工作目录栈”解析相对/绝对路径，
/// 支持 Save/Restore 现场保护，按路径段选择对应虚拟容器类型。
/// </summary>
public class ResourceLocator
{
    /// <summary>资源容器（带缓存）。</summary>
    private readonly OFDDir _ofdDir;

    /// <summary>当前工作目录（首元素恒为 "/"）。</summary>
    private readonly List<string> _workDir = new() { "/" };

    /// <summary>保存的路径栈（栈顶为最新的 Save）。</summary>
    private readonly List<List<string>> _savedStack = new();

    public ResourceLocator(OFDDir ofdDir)
    {
        _ofdDir = ofdDir ?? throw new ArgumentException("虚拟容器（ofdDir）为空");
    }

    /// <summary>通过虚拟容器创建定位器，并切换路径到该容器目录。</summary>
    public ResourceLocator(VirtualContainer vc)
    {
        if (vc == null) throw new ArgumentException("虚拟容器（vc）为空");

        VirtualContainer p = vc;
        while (!ReferenceEquals(p, p.Parent))
            p = p.Parent;

        if (p is not OFDDir dir)
            throw new ArgumentException("根路径不是 OFDDir");

        _ofdDir = dir;
        Cd(vc.GetAbsLoc().Loc);
    }

    /// <summary>获取所属 OFD 容器。</summary>
    public OFDDir GetOFDDir() => _ofdDir;

    /// <summary>保存当前工作路径（入栈）。</summary>
    public ResourceLocator Save()
    {
        _savedStack.Insert(0, new List<string>(_workDir));
        return this;
    }

    /// <summary>还原最近一次保存的工作路径（无保存时无副作用）。</summary>
    public ResourceLocator Restore()
    {
        if (_savedStack.Count == 0) return this;
        _workDir.Clear();
        _workDir.AddRange(_savedStack[0]);
        _savedStack.RemoveAt(0);
        return this;
    }

    /// <summary>转换路径对象为绝对路径字符串（null 返回当前目录）。</summary>
    public string ToAbsolutePath(ST_Loc? path) => path == null ? Pwd() : ToAbsolutePath(path.Loc);

    /// <summary>路径转换为绝对路径（绝对路径直接归一化，相对路径基于当前目录）。</summary>
    public string ToAbsolutePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return Pwd();

        var workDirCopy = new List<string>(_workDir);
        if (path.StartsWith('/'))
        {
            workDirCopy.Clear();
            workDirCopy.Add("/");
        }

        foreach (var raw in path.Split('/'))
        {
            string item = raw.Trim();
            if (item.Length == 0 || item == ".")
            {
                continue; // 当前目录
            }
            if (item == "..")
            {
                if (workDirCopy.Count > 0) workDirCopy.RemoveAt(workDirCopy.Count - 1);
                if (workDirCopy.Count == 0) workDirCopy.Add("/");
            }
            else
            {
                workDirCopy.Add(item);
            }
        }
        return Pwd(workDirCopy);
    }

    /// <summary>重置工作路径到根目录。</summary>
    public ResourceLocator RestWd() => Cd("/");

    /// <summary>切换到指定虚拟容器目录。</summary>
    public ResourceLocator Cd(VirtualContainer vc)
    {
        if (vc == null) throw new ArgumentException("改变目录的容器对象（vc）为空");
        return Cd(vc.GetAbsLoc().Loc);
    }

    /// <summary>改变目录（路径不存在抛 ErrorPathException）。</summary>
    public ResourceLocator Cd(string path) => Cd(_workDir, path);

    /// <summary>在给定工作目录副本上改变目录（路径不存在抛 ErrorPathException）。</summary>
    public ResourceLocator Cd(List<string> workDir, string? path)
    {
        if (string.IsNullOrEmpty(path)) return this;

        path = path.Trim();
        if (path == "/")
        {
            workDir.Clear();
            workDir.Add("/");
            return this;
        }

        string absPath = ToAbsolutePath(path);
        string sysPath = Path.GetFullPath(_ofdDir.GetSysAbsPath() + absPath);
        if (!Directory.Exists(sysPath))
            throw new ErrorPathException("无法切换路径到" + path + "，目录不存在。");

        workDir.Clear();
        workDir.Add("/");
        foreach (var raw in absPath.Split('/'))
        {
            string item = raw.Trim();
            if (item.Length == 0) continue;
            workDir.Add(item);
        }
        return this;
    }

    /// <summary>判断路径是否存在（绝对路径直接拼接根目录，相对路径基于工作目录）。</summary>
    public bool Exist(string? path)
    {
        if (path == null) return false;
        string ofwTmp = _ofdDir.GetSysAbsPath();
        string fullPath = path.StartsWith('/')
            ? Path.GetFullPath(ofwTmp + path)
            : Path.GetFullPath(Path.Combine(ofwTmp, Pwd().TrimStart('/'), path));
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    /// <summary>判断指定工作目录路径是否存在。</summary>
    public bool Exist(List<string> workDir)
    {
        string full = Path.GetFullPath(_ofdDir.GetSysAbsPath() + Pwd(workDir));
        return File.Exists(full) || Directory.Exists(full);
    }

    /// <summary>判断指定工作目录路径是否存在且为目录。</summary>
    public bool DirExist(List<string> workDir)
    {
        string full = Path.GetFullPath(_ofdDir.GetSysAbsPath() + Pwd(workDir));
        return Directory.Exists(full);
    }

    /// <summary>打印当前工作目录（Print Work Directory）。</summary>
    public string Pwd() => Pwd(_workDir);

    /// <summary>拼接工作目录路径（末尾目录不加 "/"）。</summary>
    public string Pwd(IReadOnlyList<string> workDir)
    {
        if (workDir.Count == 1) return "/";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < workDir.Count; i++)
        {
            string item = workDir[i].Trim();
            if (item.Length == 0) continue;
            sb.Append(item);
            // 最后一个路径不加 "/"
            if (item != "/" && i != workDir.Count - 1) sb.Append('/');
        }
        return sb.ToString();
    }

    /// <summary>获取以当前路径为基础的容器内绝对路径。</summary>
    public ST_Loc GetAbsTo(ST_Loc? to)
    {
        if (to == null) return new ST_Loc(Pwd());

        string loc = to.Loc;
        if (loc.StartsWith('/')) return new ST_Loc(loc);

        var wd = new List<string>(_workDir);
        string fileName;
        int indexOf = loc.LastIndexOf('/');
        if (indexOf != -1)
        {
            Cd(wd, loc[..(indexOf + 1)]);
            fileName = loc[(indexOf + 1)..];
        }
        else
        {
            fileName = loc;
        }

        string p = Pwd(wd);
        loc = p.EndsWith('/') ? p + fileName : p + "/" + fileName;
        return new ST_Loc(loc);
    }

    /// <summary>根据路径获取对应的资源对象（不存在抛 FileNotFoundException）。</summary>
    public T Get<T>(ST_Loc loc, Func<DomElement, T> mapper) => Get(loc.Loc, mapper);

    /// <summary>根据路径获取对应的资源对象（不存在抛 FileNotFoundException）。</summary>
    public T Get<T>(string loc, Func<DomElement, T> mapper)
    {
        if (string.IsNullOrWhiteSpace(loc)) throw new FileNotFoundException("路径为空（loc）");

        var wd = new List<string>(_workDir);
        string fileName;
        int indexOf = loc.LastIndexOf('/');
        if (indexOf != -1)
        {
            Cd(wd, loc[..(indexOf + 1)]);
            fileName = loc[(indexOf + 1)..];
        }
        else
        {
            fileName = loc;
        }

        var vc = GetContainer(wd);
        var element = vc.GetObjByFile(fileName);
        return mapper(element);
    }

    /// <summary>获取路径下的文件（不存在抛 FileNotFoundException）。</summary>
    public string GetFile(ST_Loc stLoc) => GetFile(stLoc.Loc);

    /// <summary>获取路径下的文件（不存在抛 FileNotFoundException）。</summary>
    public string GetFile(string loc)
    {
        if (string.IsNullOrWhiteSpace(loc)) throw new FileNotFoundException("路径为空（loc）");

        var wd = new List<string>(_workDir);
        string fileName;
        int indexOf = loc.LastIndexOf('/');
        if (indexOf != -1)
        {
            Cd(wd, loc[..(indexOf + 1)]);
            fileName = loc[(indexOf + 1)..];
        }
        else
        {
            fileName = loc;
        }

        var vc = GetContainer(wd);
        return vc.GetFileOrThrow(fileName);
    }

    /// <summary>通过路径段序列获取容器（每段按名称前缀分派容器类型）。</summary>
    public VirtualContainer GetContainer(List<string> workDir)
    {
        VirtualContainer vc = _ofdDir;
        foreach (var item in workDir)
        {
            if (item == "/") continue;

            if (item.StartsWith("Doc_", StringComparison.Ordinal))
                vc = vc.GetContainer(item, p => new DocDir(p));
            else if (item == "Signs")
                vc = vc.GetContainer(item, p => new SignsDir(p));
            else if (item.StartsWith("Sign_", StringComparison.Ordinal))
                vc = vc.GetContainer(item, p => new SignDir(p));
            else if (item == "Pages")
                vc = vc.GetContainer(item, p => new PagesDir(p));
            else if (item.StartsWith("Page_", StringComparison.Ordinal))
                vc = vc.GetContainer(item, p => new PageDir(p));
            else if (item == "Res")
                vc = vc.GetContainer(item, p => new ResDir(p));
            else if (item.StartsWith("Temp_", StringComparison.Ordinal))
                vc = vc.GetContainer(item, p => new TempsDir(p));
            else
                vc = vc.GetContainer(item, p => new VirtualContainer(p));
        }
        return vc;
    }

    /// <summary>根据容器路径获取虚拟容器（获取后缓存整个链路）。</summary>
    public VirtualContainer GetContainer(string containerPath)
    {
        Save();
        try
        {
            Cd(containerPath);
            return GetContainer(_workDir);
        }
        finally
        {
            Restore();
        }
    }

    public override string ToString() => Pwd();
}
