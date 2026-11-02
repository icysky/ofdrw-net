namespace Ofd.Core.Crypto.Encryt;

/// <summary>可解密该次操作的用户（OFD 2.0 GMT0099 附录 C.1）。</summary>
public class UserInfo : OFDElement
{
    /// <summary>用户角色类型：文档管理员</summary>
    public const string UserTypeOwner = "Owner";

    /// <summary>用户角色类型：普通用户（默认值）</summary>
    public const string UserTypeUser = "User";

    public UserInfo(Dom.DomElement proxy) : base(proxy) { }

    public UserInfo() : base("UserInfo") { }

    /// <summary>【可选属性 OFD 2.0】设置 用户名称（null 删除属性）。</summary>
    public UserInfo SetUserName(string? userName)
    {
        if (userName == null)
        {
            RemoveAttr("UserName");
            return this;
        }
        return (UserInfo)AddAttribute("UserName", userName);
    }

    /// <summary>【可选属性 OFD 2.0】获取 用户名称，可能为 null。</summary>
    public string? GetUserName() => AttributeValue("UserName");

    /// <summary>【可选属性 OFD 2.0】设置 用户角色类型（Owner/User；null 删除属性）。</summary>
    public UserInfo SetUserType(string? userType)
    {
        if (userType == null)
        {
            RemoveAttr("UserType");
            return this;
        }
        return (UserInfo)AddAttribute("UserType", userType);
    }

    /// <summary>【可选属性 OFD 2.0】获取 用户角色类型，缺省 User。</summary>
    public string GetUserType() => AttributeValue("UserType") ?? UserTypeUser;

    /// <summary>【可选 OFD 2.0】设置 用户加解密公钥证书（Base64；null 删除元素）。</summary>
    public UserInfo SetUserCert(byte[]? userCert)
    {
        if (userCert == null)
        {
            RemoveOFDElemByNames("UserCert");
            return this;
        }
        return (UserInfo)AddOFDEntity("UserCert", Convert.ToBase64String(userCert));
    }

    /// <summary>【可选 OFD 2.0】获取 用户加解密公钥证书，可能为 null。</summary>
    public byte[]? GetUserCert()
    {
        var e = GetOFDElement("UserCert");
        return e == null ? null : Convert.FromBase64String(ElementText(e).Trim());
    }

    /// <summary>【必选 OFD 2.0】设置 文件对称加密的包装密钥（Base64；null 抛异常）。</summary>
    public UserInfo SetEncryptedWK(byte[] encryptedWk)
    {
        if (encryptedWk == null) throw new ArgumentException("文件对称加密的包装密钥（EncrypteWk）为空");
        return (UserInfo)SetOFDEntity("EncryptedWK", Convert.ToBase64String(encryptedWk));
    }

    /// <summary>【必选 OFD 2.0】获取 文件对称加密的包装密钥，缺失抛异常。</summary>
    public byte[] GetEncryptedWK()
    {
        var e = GetOFDElement("EncryptedWK");
        if (e == null) throw new InvalidOperationException("文件对称加密的包装密钥(EncryptedWK)不存在");
        return Convert.FromBase64String(ElementText(e).Trim());
    }

    /// <summary>
    /// 【可选 OFD 2.0】设置 初始化向量值（Base64；null 删除元素）。
    /// 注：Java 上游 null 分支删除后未 return，继续 Base64(null) 导致 NPE，C# 已修正。
    /// </summary>
    public UserInfo SetIVValue(byte[]? ivValue)
    {
        if (ivValue == null)
        {
            RemoveOFDElemByNames("IVValue");
            return this;
        }
        return (UserInfo)SetOFDEntity("IVValue", Convert.ToBase64String(ivValue));
    }

    /// <summary>【可选 OFD 2.0】获取 初始化向量值，缺省 16 个 0。</summary>
    public byte[] GetIVValue()
    {
        var e = GetOFDElement("IVValue");
        return e == null ? new byte[16] : Convert.FromBase64String(ElementText(e).Trim());
    }
}
