using Ofd.Core.Signatures.Appearance;

namespace Ofd.Reader;

/// <summary>签章注释信息（已过时，改用 <see cref="StampAnnotEntity"/>）。</summary>
[Obsolete("请使用 StampAnnotEntity")]
public class StampAnnotVo
{
    private List<StampAnnot>? _stampAnnots;
    private byte[]? _imgByte;
    private string? _type;

    public List<StampAnnot>? GetStampAnnots() => _stampAnnots;

    public void SetStampAnnots(List<StampAnnot>? stampAnnots) => _stampAnnots = stampAnnots;

    public byte[]? GetImgByte() => _imgByte;

    public void SetImgByte(byte[]? imgByte) => _imgByte = imgByte;

    public string? GetType() => _type;

    public void SetType(string? type) => _type = type;
}
