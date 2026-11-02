using Org.BouncyCastle.Asn1;

namespace Ofd.Gm.Ses.Parse;

/// <summary>版本解析（Java VersionParser）：按顶层序列长度与内部 Header.version 判定 SES 版本。</summary>
public static class VersionParser
{
    /// <summary>从印章序列中提取 Header 中的 version 字段（失败抛异常）。</summary>
    private static int ExtractSealHeaderVersion(Asn1Sequence seq)
    {
        try
        {
            // eSealInfo -> SEQUENCE，第一个元素是 header -> SEQUENCE，第二个元素是 version
            var sealInfo = Asn1Sequence.GetInstance(seq[0]);
            var header = Asn1Sequence.GetInstance(sealInfo[0]);
            return DerInteger.GetInstance(header[1]).IntValueExact;
        }
        catch (Exception e)
        {
            throw new ArgumentException("未知的数据结构，无法匹配任何已知版本电子印章。", e);
        }
    }

    /// <summary>解析电子印章版本（入参可为 byte[] 或 Asn1 对象）。</summary>
    public static SESVersionHolder ParseSesSealVersion(object o)
    {
        var seq = Asn1Sequence.GetInstance(o);
        SESVersion version;
        if (seq.Count == 5)
        {
            // GM/T 0031-2025：印章信息/证书/算法标识/签名值/时间戳[可选]
            version = SESVersion.V5;
        }
        else if (seq.Count == 4)
        {
            // 可能是 V4（固定 4 字段）或 V5（无时间戳）
            version = ExtractSealHeaderVersion(seq) == 5 ? SESVersion.V5 : SESVersion.V4;
        }
        else if (seq.Count == 2)
        {
            // GM/T 0031-2014：印章信息 / 制章人签名信息
            version = SESVersion.V1;
        }
        else
        {
            throw new ArgumentException("未知的数据结构，无法匹配任何已知版本电子印章。");
        }
        return new SESVersionHolder(version, seq);
    }

    /// <summary>解析电子签章数据版本（入参可为 byte[] 或 Asn1 对象）。</summary>
    public static SESVersionHolder ParseSesSignatureVersion(object o)
    {
        var seq = Asn1Sequence.GetInstance(o);
        SESVersion version;
        if (seq.Count is >= 4 and <= 5)
        {
            // V4 与 V5 签章结构均为 4~5 字段，通过内部 TBS_Sign 的 eseal header version 区分
            try
            {
                var tbsSign = Asn1Sequence.GetInstance(seq[0]);
                var esealSeq = Asn1Sequence.GetInstance(tbsSign[1]);
                version = ExtractSealHeaderVersion(esealSeq) == 5 ? SESVersion.V5 : SESVersion.V4;
            }
            catch (Exception e)
            {
                throw new ArgumentException("无法解析电子签章数据版本", e);
            }
        }
        else if (seq.Count == 2)
        {
            // GM/T 0031-2014：待签章数据 / 签名值
            version = SESVersion.V1;
        }
        else
        {
            throw new ArgumentException("未知的数据结构，无法匹配任何已知版本电子签章数据。");
        }
        return new SESVersionHolder(version, seq);
    }
}
