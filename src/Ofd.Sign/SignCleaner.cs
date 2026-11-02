using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Sign;

/// <summary>
/// 数字签名清除工具（Java SignCleaner）：删除全部文档的签名列表与签名容器。
/// <para>该操作会关闭 Reader。</para>
/// </summary>
public class SignCleaner
{
    private readonly OFDReader _reader;
    private readonly string _out;

    public SignCleaner(OFDReader reader, string outPath)
    {
        _reader = reader ?? throw new ArgumentException("OFD解析器(reader)为空");
        _out = outPath ?? throw new ArgumentException("输出位置(out)为空");
    }

    /// <summary>清除所有文档中的全部数字签名，并输出到指定位置。</summary>
    public void Clean()
    {
        var ofdDir = _reader.GetOFDDir();
        var ofd = ofdDir.GetOfd();
        var rl = _reader.GetResourceLocator();

        foreach (var docBody in ofd.GetDocBodies())
        {
            // 1. 获取并删除签名列表文件入口
            var signListFileLoc = docBody.GetSignatures();
            docBody.RemoveOFDElemByNames("Signatures");

            if (signListFileLoc == null) continue;

            string signaturesXmlFile = rl.GetFile(signListFileLoc);

            // 2/3. 若签名列表文件位于 Signs 目录，则清空该目录
            string parent = signListFileLoc.Parent();
            var container = rl.GetContainer(parent);
            if ("Signs".Equals(container.ContainerName, StringComparison.OrdinalIgnoreCase))
                container.Clean();

            // 不规范的文件：Signatures.xml 不在 Signs 目录下时单独删除
            if (File.Exists(signaturesXmlFile)) File.Delete(signaturesXmlFile);

            // 4. 打包输出
            ofdDir.Jar(Path.GetFullPath(_out));
        }
    }
}
