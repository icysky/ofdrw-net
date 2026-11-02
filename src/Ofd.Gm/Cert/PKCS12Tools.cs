using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using BcX509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Ofd.Gm.Cert;

/// <summary>PKCS#12 解析工具（Java PKCS12Tools）：读取私钥、证书链与用户证书。</summary>
public static class PKCS12Tools
{
    // ==================== 私钥 ====================

    /// <summary>从 P12 文件获取私钥（默认取第一个别名）。</summary>
    public static AsymmetricKeyParameter ReadPrvKey(string userP12, string pwd)
    {
        using var input = File.OpenRead(userP12);
        return ReadPrvKey(input, pwd);
    }

    /// <summary>从 P12 流获取私钥（默认取第一个别名）。</summary>
    public static AsymmetricKeyParameter ReadPrvKey(Stream rootKsIn, string pwd)
    {
        var store = Load(rootKsIn, pwd);
        string alias = FirstAlias(store);
        return store.GetKey(alias).Key;
    }

    /// <summary>从 P12 文件获取指定别名的私钥。</summary>
    public static AsymmetricKeyParameter ReadPrvKey(string userP12, string alias, string pwd)
    {
        using var input = File.OpenRead(userP12);
        return ReadPrvKey(input, alias, pwd);
    }

    /// <summary>从 P12 流获取指定别名的私钥。</summary>
    public static AsymmetricKeyParameter ReadPrvKey(Stream rootKsIn, string alias, string pwd)
    {
        var store = Load(rootKsIn, pwd);
        return store.GetKey(alias).Key;
    }

    // ==================== 证书链 ====================

    /// <summary>从 P12 流获取证书链（第一个为用户证书）。</summary>
    public static BcX509Certificate[] ReadCertChain(Stream rootKsIn, string pwd)
    {
        var store = Load(rootKsIn, pwd);
        return CertChain(store, FirstAlias(store));
    }

    /// <summary>从 P12 流获取指定别名的证书链（第一个为用户证书）。</summary>
    public static BcX509Certificate[] ReadCertChain(Stream rootKsIn, string alias, string pwd)
    {
        var store = Load(rootKsIn, pwd);
        return CertChain(store, alias);
    }

    // ==================== 用户证书 ====================

    /// <summary>从 P12 文件获取用户证书（证书链第一张）。</summary>
    public static BcX509Certificate ReadUserCert(string userP12, string pwd)
    {
        using var input = File.OpenRead(userP12);
        return ReadUserCert(input, pwd);
    }

    /// <summary>从 P12 流获取用户证书（证书链第一张）。</summary>
    public static BcX509Certificate ReadUserCert(Stream rootKsIn, string pwd) => ReadCertChain(rootKsIn, pwd)[0];

    /// <summary>从 P12 文件获取指定别名的用户证书。</summary>
    public static BcX509Certificate ReadUserCert(string userP12, string alias, string pwd)
    {
        using var input = File.OpenRead(userP12);
        return ReadUserCert(input, alias, pwd);
    }

    /// <summary>从 P12 流获取指定别名的用户证书。</summary>
    public static BcX509Certificate ReadUserCert(Stream rootKsIn, string alias, string pwd) =>
        ReadCertChain(rootKsIn, alias, pwd)[0];

    // ==================== 内部 ====================

    private static Pkcs12Store Load(Stream input, string pwd)
    {
        var store = new Pkcs12StoreBuilder().Build();
        store.Load(input, pwd.ToCharArray());
        return store;
    }

    private static string FirstAlias(Pkcs12Store store) => store.Aliases.First();

    private static BcX509Certificate[] CertChain(Pkcs12Store store, string alias) =>
        store.GetCertificateChain(alias).Select(e => e.Certificate).ToArray();
}
