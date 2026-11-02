using Ofd.Core.Signatures;
using Ofd.Gm.Sm2Strut;
using Ofd.Sign.Verify.Exceptions;

namespace Ofd.Sign.Verify.Container;

/// <summary>
/// GB/T 35275 数字签名验证容器（Java GBT35275ValidateContainer）。
/// </summary>
public class GBT35275ValidateContainer : IValidateContainer
{
    public void Validate(SigType type, string alg, byte[] tbsContent, byte[] signedValue)
    {
        if (type != SigType.Sign)
            throw new ArgumentException("签名类型(type)必须是 Sign，不支持电子印章验证");

        var verifyInfo = GBT35275Validate.Validate(alg, tbsContent, signedValue);
        if (!verifyInfo.Result)
            throw new InvalidSignedValueException(verifyInfo.Hit);
    }
}
