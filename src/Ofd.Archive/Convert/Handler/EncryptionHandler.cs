using Ofd.Crypto;
using Ofd.Crypto.Decryptor;
using Ofd.Pkg.Container;
using Ofd.Reader;

namespace Ofd.Archive.Convert.Handler;

/// <summary>
/// 处理器 0：文件解密（GB/T 42133-2022 6.16）—— 管道最前端执行。
/// <para>文档已加密但未配置解密器时**硬中断**，不继续后续流程。</para>
/// </summary>
public class EncryptionHandler : IArchiveHandler
{
    /// <summary>用户提供的 FEK 解密器。</summary>
    private readonly List<IUserFekDecryptor> _decryptors = new();

    /// <summary>添加 FEK 解密器。</summary>
    public EncryptionHandler AddDecryptor(IUserFekDecryptor? decryptor)
    {
        if (decryptor != null) _decryptors.Add(decryptor);
        return this;
    }

    /// <summary>是否已配置解密器。</summary>
    public bool HasDecryptor => _decryptors.Count > 0;

    public void Handle(OFDReader reader, OFDDir ofdDir) => DecryptInPlace(reader.GetWorkDir());

    /// <summary>
    /// 在解压目录上就地解密。
    /// <para>
    /// 必须在创建 <see cref="OFDReader"/> **之前**调用：加密包内没有 <c>OFD.xml</c>
    /// （被加密为 <c>ofd.dat</c> 等），Reader 构造会直接失败。
    /// </para>
    /// </summary>
    /// <exception cref="IOException">已加密但未配置解密器，或解密失败</exception>
    public void DecryptInPlace(string workDir)
    {
        string encryptionsPath = Path.Combine(workDir, "Encryptions.xml");

        // 未加密，放行
        if (!File.Exists(encryptionsPath)) return;

        // 已加密但未配置解密器 → 硬中断
        if (_decryptors.Count == 0)
        {
            throw new IOException("文档已加密但未配置解密器，无法继续转换。请通过 EncryptionHandler.AddDecryptor() 提供解密密钥。");
        }

        DecryptAndRestore(new OFDDir(workDir), workDir);
    }

    /// <summary>打包 → 解密 → 解压回路。</summary>
    private void DecryptAndRestore(OFDDir ofdDir, string workDir)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "ofd-arch-enc-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        string tempOfd = Path.Combine(tempDir, "src.ofd");
        string decrypted = Path.Combine(tempDir, "decrypted.ofd");

        try
        {
            // 1) 打包当前工作目录
            using (var fs = File.Create(tempOfd)) ofdDir.Jar(fs);

            // 2) 解密
            try
            {
                using var decryptor = new OFDDecryptor(tempOfd, decrypted);
                foreach (var d in _decryptors) decryptor.AddUser(d);
                decryptor.Decrypt();
            }
            catch (Exception e) when (e is not IOException)
            {
                throw new IOException("解密失败: " + e.Message, e);
            }

            // 3) 清空工作目录并解压回来
            ClearDirectory(workDir);
            ZipUtil.UnZipFiles(decrypted, workDir + Path.DirectorySeparatorChar);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch (IOException)
            {
                // 忽略
            }
        }
    }

    /// <summary>清空目录内容（保留目录本身）。</summary>
    private static void ClearDirectory(string dir)
    {
        foreach (var file in Directory.GetFiles(dir)) File.Delete(file);
        foreach (var sub in Directory.GetDirectories(dir)) Directory.Delete(sub, true);
    }
}
