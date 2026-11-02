using Ofd.Core.Signatures.Appearance;
using Ofd.Core.Signatures.Sig;
using Ofd.Gm.Ses.Parse;

namespace Ofd.Reader.Model;

/// <summary>OFD 中的签章信息（Java StampAnnotEntity）：签名保护信息 + 印章图片。</summary>
public class StampAnnotEntity
{
    /// <summary>签名要保护的原文及本次签名相关的信息。</summary>
    private readonly SignedInfo _signedInfo;

    /// <summary>多版本电子签章数据（Java SESVersionHolder）。</summary>
    private readonly SESVersionHolder? _sesVersionHolder;

    /// <summary>印章图片（可能是 OFD/PNG/GIF/SVG，按 ImgType 判断）。</summary>
    private byte[] _imageByte = Array.Empty<byte>();

    /// <summary>印章图片类型。</summary>
    private string? _imgType;

    /// <summary>创建签章实体（仅签名信息，无印章图片）。</summary>
    public StampAnnotEntity(SignedInfo signedInfo) => _signedInfo = signedInfo;

    /// <summary>创建签章实体并从 SES 数据中提取印章图片。</summary>
    public StampAnnotEntity(SESVersionHolder sesVersionHolder, SignedInfo signedInfo)
    {
        _signedInfo = signedInfo;
        _sesVersionHolder = sesVersionHolder;
        Init();
    }

    private void Init()
    {
        switch (_sesVersionHolder!.GetVersion())
        {
            case SESVersion.V1:
            {
                var sesSignature = _sesVersionHolder.GetInstanceV1()
                    ?? throw new BadOFDException("SES v1 签章数据无法解析");
                FillPicture(sesSignature.GetToSign()?.GetEseal()?.GetEsealInfo()?.GetPicture());
                break;
            }
            case SESVersion.V4:
            {
                var sesSignature = _sesVersionHolder.GetInstanceV4()
                    ?? throw new BadOFDException("SES v4 签章数据无法解析");
                FillPicture(sesSignature.GetToSign()?.GetEseal()?.GeteSealInfo()?.GetPicture());
                break;
            }
            case SESVersion.V5:
            {
                var sesSignature = _sesVersionHolder.GetInstanceV5()
                    ?? throw new BadOFDException("SES v5 签章数据无法解析");
                FillPicture(sesSignature.GetToSign()?.GetEseal()?.GeteSealInfo()?.GetPicture());
                break;
            }
            default:
                throw new NotSupportedException($"SES {_sesVersionHolder.GetVersion()} 印章图片提取尚未移植");
        }
    }

    private void FillPicture(Ofd.Gm.Ses.V1.SES_ESPictrueInfo? picture)
    {
        if (picture == null) return;
        _imgType = picture.GetType()?.GetString();
        _imageByte = picture.GetData()?.GetOctets() ?? Array.Empty<byte>();
    }

    private void FillPicture(Ofd.Gm.Ses.V5.SES_ESPictrueInfo? picture)
    {
        if (picture == null) return;
        _imgType = picture.GetType()?.GetString();
        _imageByte = picture.GetData()?.GetOctets() ?? Array.Empty<byte>();
    }

    /// <summary>签名的外观序列。</summary>
    public List<StampAnnot> GetStampAnnots() => _signedInfo.GetStampAnnots();

    /// <summary>图片数据。</summary>
    public byte[] GetImageByte() => _imageByte;

    /// <summary>印章图片类型。</summary>
    public string? GetImgType() => _imgType;

    /// <summary>获取图片字节流。</summary>
    public Stream GetImageStream() => new MemoryStream(_imageByte);

    /// <summary>获取多版本电子签章数据信息，未由 SES 构造时为 null。</summary>
    public SESVersionHolder? GetHolder() => _sesVersionHolder;

    /// <summary>获取签名要保护的原文及本次签名相关的信息。</summary>
    public SignedInfo GetSignedInfo() => _signedInfo;
}
