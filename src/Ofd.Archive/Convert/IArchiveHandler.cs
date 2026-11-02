using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert;

/// <summary>
/// OFD-A 转换处理器（Java ArchiveHandler）：执行一项「去技术化」操作，就地修改 OFDDir 中的文件。
/// <para>按编排顺序执行；实现应无状态、可重复调用。</para>
/// </summary>
public interface IArchiveHandler
{
    /// <summary>执行转换（结果写入 ofdDir，由调用方负责最终打包）。</summary>
    void Handle(OFDReader reader, OFDDir ofdDir);
}
