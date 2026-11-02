using System.Globalization;

namespace Ofd.Core.Crypto.Encryt;

/// <summary>
/// 解密入口文件（OFD 2.0 GMT0099-2020 附录 C.2）：根节点 Encryptions，
/// 文件固定名 "Encryptions.xml"，放置于 OFD 根目录。
/// </summary>
public class Encryptions : OFDElement
{
    public Encryptions(Dom.DomElement proxy) : base(proxy) { }

    public Encryptions() : base("Encryptions") { }

    /// <summary>【必选 OFD 2.0】增加 加密描述信息（多重加密形成多条；null 忽略）。</summary>
    public Encryptions AddEncryptInfo(CT_EncryptInfo? encryptInfo)
    {
        if (encryptInfo == null) return this;
        Add(encryptInfo);
        return this;
    }

    /// <summary>【必选 OFD 2.0】获取 加密描述信息列表。</summary>
    public List<CT_EncryptInfo> GetEncryptInfos() => GetOFDElements("EncryptInfo", e => new CT_EncryptInfo(e));

    /// <summary>返回当前容器内最大的加密操作标识数字（无法解析按 0）。</summary>
    public int MaxId()
    {
        int res = 0;
        foreach (var encryptInfo in GetEncryptInfos())
        {
            var id = encryptInfo.GetId();
            if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) && i > res)
                res = i;
        }
        return res;
    }
}
