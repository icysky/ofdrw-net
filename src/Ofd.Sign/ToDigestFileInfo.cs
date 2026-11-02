using Ofd.Core.BasicType;

namespace Ofd.Sign;

/// <summary>待计算杂凑值的文件信息（Java ToDigestFileInfo）。</summary>
public class ToDigestFileInfo
{
    /// <summary>文件在 OFD 虚拟容器中的绝对路径，如 "/Doc_0/Pages/Page_0/Content.xml"。</summary>
    private ST_Loc _absPath;

    /// <summary>待杂凑文件在文件系统中的路径。</summary>
    private string _sysPath;

    public ToDigestFileInfo(string absPath, string sysPath)
    {
        _absPath = new ST_Loc(absPath);
        _sysPath = sysPath;
    }

    public ST_Loc GetAbsPath() => _absPath;

    public void SetAbsPath(string absPath) => _absPath = new ST_Loc(absPath);

    public string GetSysPath() => _sysPath;

    public void SetSysPath(string sysPath) => _sysPath = sysPath;
}
