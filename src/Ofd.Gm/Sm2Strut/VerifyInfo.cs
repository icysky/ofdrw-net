namespace Ofd.Gm.Sm2Strut;

/// <summary>签名验证信息（Java VerifyInfo）。</summary>
public class VerifyInfo
{
    /// <summary>验证是否通过。</summary>
    public bool Result;

    /// <summary>不通过时的错误信息。</summary>
    public string Hit;

    public VerifyInfo(bool result, string hit)
    {
        Result = result;
        Hit = hit;
    }

    /// <summary>获取错误返回值。</summary>
    public static VerifyInfo Err(string errDesp) => new(false, errDesp);

    /// <summary>获取验证成功状态。</summary>
    public static VerifyInfo Ok() => new(true, "");
}
