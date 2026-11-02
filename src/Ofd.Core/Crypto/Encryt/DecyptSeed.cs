namespace Ofd.Core.Crypto.Encryt;

/// <summary>
/// 密钥描述文件（GMT 0099-2020 C.2 图 C.1）：记录方案、算法和多用户/多角色、
/// 多密码或证书等关键解密信息。
/// </summary>
public class DecyptSeed : OFDElement
{
    public DecyptSeed(Dom.DomElement proxy) : base(proxy) { }

    public DecyptSeed() : base("DecyptSeed") { }

    /// <summary>【必选属性】设置 加密操作标识（空串抛异常）。</summary>
    public DecyptSeed SetId(string id)
    {
        if (id == null || id.Length == 0) throw new ArgumentException("加密操作标识(id)为空");
        return (DecyptSeed)AddAttribute("ID", id);
    }

    /// <summary>【必选属性】获取 加密操作标识，不存在 null。</summary>
    public string? GetId() => AttributeValue("ID");

    /// <summary>
    /// 【必选属性】设置 加密保护方案标识（字符串形式）。
    /// 注：Java 上游误用 attributeValue（读）而非 addAttribute（写），C# 已修正。
    /// </summary>
    public DecyptSeed SetEncryptCaseId(string encryptCaseId)
    {
        if (encryptCaseId == null) throw new ArgumentException("加密保护方案标识(id)为空");
        return (DecyptSeed)AddAttribute("EncryptCaseId", encryptCaseId);
    }

    /// <summary>【必选属性】设置 加密保护方案标识（枚举）。</summary>
    public DecyptSeed SetEncryptCaseId(ProtectionCaseID encryptCaseId) =>
        SetEncryptCaseId(encryptCaseId.ToIdValue());

    /// <summary>【必选属性】获取 加密保护方案标识，不存在 null。</summary>
    public string? GetEncryptCaseId() => AttributeValue("EncryptCaseId");

    /// <summary>【必选 OFD 2.0】增加 可解密该次操作的用户（null 抛异常）。</summary>
    public DecyptSeed AddUserInfo(UserInfo userInfo)
    {
        if (userInfo == null) throw new ArgumentException("可解密该次操作的用户(userInfo)为空");
        Add(userInfo);
        return this;
    }

    /// <summary>【必选 OFD 2.0】获取 可解密该次操作的用户列表。</summary>
    public List<UserInfo> GetUserInfos() => GetOFDElements("UserInfo", e => new UserInfo(e));

    /// <summary>【必选 OFD 2.0】设置 扩展参数节点（null 抛异常）。</summary>
    public DecyptSeed SetExtendParams(ExtendParams extendParams)
    {
        if (extendParams == null) throw new ArgumentException("扩展参数节点(ExtendParams)为空");
        Set(extendParams);
        return this;
    }

    /// <summary>【必选 OFD 2.0】获取 扩展参数节点（不存在时自动创建空节点，与 Java 一致）。</summary>
    public ExtendParams GetExtendParams()
    {
        var e = GetOFDElement("ExtendParams");
        if (e == null)
        {
            var emptyParams = new ExtendParams();
            SetExtendParams(emptyParams);
            return emptyParams;
        }
        return new ExtendParams(e);
    }
}
