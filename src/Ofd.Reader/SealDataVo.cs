namespace Ofd.Reader;

/// <summary>签章数据（图片字节 + 类型）。</summary>
public class SealDataVo
{
    private byte[]? _sealBytes;
    private string? _sealType;

    public byte[]? GetSealBytes() => _sealBytes;

    public void SetSealBytes(byte[]? sealBytes) => _sealBytes = sealBytes;

    public string? GetSealType() => _sealType;

    public void SetSealType(string? sealType) => _sealType = sealType;
}
