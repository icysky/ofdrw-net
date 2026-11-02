using Ofd.Core.BasicType;

namespace Ofd.Archive.Check;

/// <summary>
/// OFD-A 不合规项描述（Java ArchiveViolation）：描述文档中一处不符合 GB/T 42133-2022 的位置与原因。
/// </summary>
public class ArchiveViolation
{
    /// <summary>违规严重程度。</summary>
    public enum Severity
    {
        /// <summary>错误：违反 OFD-A 核心约束，必须修复。</summary>
        Error,

        /// <summary>警告：不符合标准建议，但可接受。</summary>
        Warn,

        /// <summary>信息：提示性信息。</summary>
        Info,
    }

    /// <summary>规则标识（如 DOC_TYPE、IMAGE_FORMAT）。</summary>
    private readonly string _ruleName;

    private readonly Severity _severity;
    private readonly string _description;
    private readonly ST_Loc? _location;
    private readonly string? _actualValue;
    private readonly string? _expectedValue;

    public ArchiveViolation(string ruleName, Severity severity, string description,
        ST_Loc? location = null, string? actualValue = null, string? expectedValue = null)
    {
        _ruleName = ruleName ?? throw new ArgumentException("规则标识不能为 null");
        _severity = severity;
        _description = description ?? throw new ArgumentException("描述不能为 null");
        _location = location;
        _actualValue = actualValue;
        _expectedValue = expectedValue;
    }

    public string GetRuleName() => _ruleName;

    public Severity GetSeverity() => _severity;

    public string GetDescription() => _description;

    public ST_Loc? GetLocation() => _location;

    public string? GetActualValue() => _actualValue;

    public string? GetExpectedValue() => _expectedValue;

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('[').Append(_severity.ToString().ToUpperInvariant()).Append("] ");
        sb.Append(_ruleName).Append(": ").Append(_description);

        if (_location != null) sb.Append(" (位置: ").Append(_location).Append(')');
        if (_actualValue != null) sb.Append(" 实际: ").Append(_actualValue);
        if (_expectedValue != null) sb.Append(" 期望: ").Append(_expectedValue);

        return sb.ToString();
    }
}
