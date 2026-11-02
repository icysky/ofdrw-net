using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Check;

/// <summary>
/// OFD-A 检查规则（Java ArchiveRule）：验证文档某一约束是否满足 GB/T 42133-2022。
/// <para>实现应无状态、可重复调用；检查过程只读。</para>
/// </summary>
public interface IArchiveRule
{
    /// <summary>执行检查。</summary>
    /// <param name="reader">OFD 阅读器（资源/页面/附件等高级访问）</param>
    /// <param name="ofdDir">OFD 虚拟容器（底层文件与目录）</param>
    /// <returns>违规项列表（非 null，合规时为空）</returns>
    List<ArchiveViolation> Check(OFDReader reader, OFDDir ofdDir);
}
