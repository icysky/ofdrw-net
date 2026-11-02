using Org.BouncyCastle.Crypto;

namespace Ofd.Gm.Support;

/// <summary>
/// 密钥派生函数（Java KDF）：GB/T 32918.3-2016 5.4.3。
/// </summary>
public static class KDF
{
    /// <summary>
    /// 密钥派生：K = Ha1 || Ha2 || ... ；Hai = Hv(Z || ct)。
    /// </summary>
    /// <param name="md">密码杂凑算法 Hv</param>
    /// <param name="z">输入比特串 Z</param>
    /// <param name="klen">预期密钥长度（字节）</param>
    public static byte[] Extend(IDigest md, byte[] z, int klen)
    {
        int v = md.GetDigestSize();
        var k = new byte[klen];

        // a) 初始化 32 位计数器 ct = 1
        int ct = 0x00000001;
        // b) i 从 1 到 ⌈klen/v⌉
        int len = klen / v + (klen % v == 0 ? 0 : 1);

        for (int i = 0; i < len; i++)
        {
            md.Reset();
            md.BlockUpdate(z, 0, z.Length);
            md.BlockUpdate(new[]
            {
                (byte)((uint)ct >> 24),
                (byte)((uint)ct >> 16),
                (byte)((uint)ct >> 8),
                (byte)ct,
            }, 0, 4);

            var hai = new byte[v];
            md.DoFinal(hai, 0);
            ct++;

            // c) 末尾不足一整块时只取最左边的剩余字节
            if (i + 1 == len && klen % v != 0)
                Array.Copy(hai, 0, k, v * (klen / v), klen - v * (klen / v));
            else
                Array.Copy(hai, 0, k, v * i, v);
        }

        return k;
    }
}
