using System.Text;
using Ofd.Core.BasicType;
using Ofd.Core.Crypto;
using Ofd.Core.Crypto.Encryt;
using Ofd.Core.Dom;
using Ofd.Core.Signatures.Sig;
using Xunit;
using CryptoProvider = Ofd.Core.Crypto.Encryt.Provider;

namespace Ofd.Tests.BasicStructure;

/// <summary>加密容器结构（OFD 2.0 GMT0099 附录 C）验证。</summary>
public class EncryptionStructureTests
{
    [Fact]
    public void ProtectionCaseId_mapping()
    {
        Assert.Equal(ProtectionCaseID.EncryptGMPassword, ProtectionCaseIDExtensions.GetInstance("1.1.1"));
        Assert.Equal(ProtectionCaseID.EncryptGMCert, ProtectionCaseIDExtensions.GetInstance("1.1.2"));
        Assert.Equal(ProtectionCaseID.Signature, ProtectionCaseIDExtensions.GetInstance("1.2.1"));
        Assert.Null(ProtectionCaseIDExtensions.GetInstance("9.9.9"));
        Assert.Equal("1.1.2", ProtectionCaseID.EncryptGMCert.ToIdValue());
    }

    [Fact]
    public void Encryptions_full_round_trip()
    {
        var info = new CT_EncryptInfo()
            .SetId("1")
            .SetProvider(new CryptoProvider().SetProviderName("OFDRW-Crypto"))
            .SetEncryptScope("Doc_0")
            .SetParameters(new Parameters().AddParameter("algo", "SM4"))
            .SetEncryptDate(new DateTime(2026, 10, 21, 9, 30, 0))
            .SetDecryptSeedLoc(new ST_Loc("Sec/Seed_1.xml"))
            .SetEntriesMapLoc("Sec/Entries_1.xml");

        var info2 = new CT_EncryptInfo()
            .SetId("2")
            .SetRelative("1")
            .SetProvider(new CryptoProvider().SetProviderName("OFDRW-Crypto"))
            .SetEncryptScope("Doc_0")
            .SetDecryptSeedLoc(new ST_Loc("Sec/Seed_2.xml"))
            .SetEntriesMapLoc(new ST_Loc("Sec/Entries_2.xml"));

        var encryptions = new Encryptions().AddEncryptInfo(info).AddEncryptInfo(info2).AddEncryptInfo(null);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(encryptions.Proxy));
        var parsed = new Encryptions(DomParser.Parse(xml));

        var list = parsed.GetEncryptInfos();
        Assert.Equal(2, list.Count);
        Assert.Equal(2, parsed.MaxId()); // 最大加密操作标识

        var p1 = list[0];
        Assert.Equal("1", p1.GetId());
        Assert.Equal("OFDRW-Crypto", p1.GetProvider().GetProviderName()); // 加密侧 Provider 用 Name 属性
        Assert.Equal("Doc_0", p1.GetEncryptScope());
        Assert.Equal("SM4", p1.GetParameters()!.GetParameter("algo")!.GetValue());
        Assert.Equal(new DateTime(2026, 10, 21, 9, 30, 0), p1.GetEncryptDate());
        Assert.Equal("2026-10-21T09:30:00", p1.GetEncryptDateStr());
        Assert.Equal("Sec/Seed_1.xml", p1.GetDecryptSeedLoc()!.ToString());
        Assert.Equal("Sec/Entries_1.xml", p1.GetEntriesMapLoc().ToString());

        Assert.Equal("1", list[1].GetRelative());
    }

    [Fact]
    public void DecyptSeed_writes_case_id_and_users()
    {
        var seed = new DecyptSeed()
            .SetId("1")
            .SetEncryptCaseId(ProtectionCaseID.EncryptGMCert) // 写入修复（上游误用读方法）
            .AddUserInfo(new UserInfo()
                .SetUserName("张三")
                .SetUserType(UserInfo.UserTypeOwner)
                .SetUserCert(new byte[] { 1, 2, 3 })
                .SetEncryptedWK(new byte[] { 4, 5, 6 })
                .SetIVValue(new byte[] { 7, 8 }))
            .AddUserInfo(new UserInfo().SetEncryptedWK(new byte[] { 9 }));

        Assert.Equal("1.1.2", seed.Proxy.Attr("EncryptCaseId")); // 属性确实写入

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(seed.Proxy));
        var parsed = new DecyptSeed(DomParser.Parse(xml));
        Assert.Equal("1", parsed.GetId());
        Assert.Equal("1.1.2", parsed.GetEncryptCaseId());

        var users = parsed.GetUserInfos();
        Assert.Equal(2, users.Count);
        Assert.Equal("张三", users[0].GetUserName());
        Assert.Equal(UserInfo.UserTypeOwner, users[0].GetUserType());
        Assert.Equal(new byte[] { 1, 2, 3 }, users[0].GetUserCert());
        Assert.Equal(new byte[] { 4, 5, 6 }, users[0].GetEncryptedWK());
        Assert.Equal(new byte[] { 7, 8 }, users[0].GetIVValue());

        Assert.Null(users[1].GetUserName());
        Assert.Equal(UserInfo.UserTypeUser, users[1].GetUserType()); // 缺省 User
        Assert.Null(users[1].GetUserCert());
        Assert.Equal(new byte[16], users[1].GetIVValue()); // 缺省 16 个 0

        // ExtendParams 不存在时自动创建（与 Java 一致）
        var extend = parsed.GetExtendParams();
        Assert.NotNull(parsed.GetOFDElement("ExtendParams"));
        extend.AddParameter("k", "v");
        Assert.Equal("v", extend.GetValue("k"));
    }

    [Fact]
    public void UserInfo_null_removes_and_validations()
    {
        var user = new UserInfo().SetEncryptedWK(new byte[] { 1 });
        user.SetUserName("u").SetUserName(null);
        Assert.Null(user.GetUserName());
        user.SetUserType("Owner").SetUserType(null);
        Assert.Equal(UserInfo.UserTypeUser, user.GetUserType());
        user.SetUserCert(new byte[] { 1 }).SetUserCert(null);
        Assert.Null(user.GetUserCert());
        // SetIVValue(null) 删除元素（上游 NPE bug 已修）
        user.SetIVValue(new byte[] { 1 }).SetIVValue(null);
        Assert.Null(user.GetOFDElement("IVValue"));
        Assert.Equal(new byte[16], user.GetIVValue());

        Assert.Throws<ArgumentException>(() => new UserInfo().SetEncryptedWK(null!));
        Assert.Throws<InvalidOperationException>(() => new UserInfo().GetEncryptedWK());

        Assert.Throws<ArgumentException>(() => new CT_EncryptInfo().SetId(""));
        Assert.Throws<InvalidOperationException>(() => new CT_EncryptInfo().GetProvider());
        Assert.Throws<InvalidOperationException>(() => new CT_EncryptInfo().GetEncryptScope());
        Assert.Throws<InvalidOperationException>(() => new CT_EncryptInfo().GetEntriesMapLoc());
    }

    [Fact]
    public void EncryptEntries_mapping_table()
    {
        var entries = new EncryptEntries()
            .SetId("1")
            .AddEncryptEntry("Doc_0/Document.xml", "Sec/E_Document.bin")
            .AddEncryptEntry("Doc_0/Pages/P0/Content.xml", "Sec/E_Content.bin", "Sec/Seed_P0.xml")
            .AddEncryptEntry(null);

        string xml = Encoding.UTF8.GetString(DomSerializer.Dump(entries.Proxy));
        var parsed = new EncryptEntries(DomParser.Parse(xml));

        Assert.Equal("1", parsed.GetId());
        var list = parsed.GetEncryptEntries();
        Assert.Equal(2, list.Count);
        Assert.Equal("Doc_0/Document.xml", list[0].GetPathA()!.ToString());
        Assert.Equal("Sec/E_Document.bin", list[0].GetEPath()!.ToString());
        Assert.Null(list[0].GetDecryptSeedLoc()); // 使用通用密钥数据
        Assert.Equal("Sec/Seed_P0.xml", list[1].GetDecryptSeedLoc()!.ToString());

        Assert.Throws<ArgumentException>(() => new EncryptEntry().SetPathA(""));
        Assert.Throws<ArgumentException>(() => new EncryptEntry().SetEPath(""));
        Assert.Throws<ArgumentException>(() => new EncryptEntry().SetPathA((ST_Loc)null!));
    }
}
