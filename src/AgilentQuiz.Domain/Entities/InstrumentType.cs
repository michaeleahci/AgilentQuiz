using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 仪器类型（A/B/C 等）
/// </summary>
public class InstrumentType : Entity
{
    /// <summary>
    /// 类型名称
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// 类型编码（唯一）
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// 状态
    /// </summary>
    public InstrumentTypeStatus Status { get; private set; } = InstrumentTypeStatus.Enabled;

    /// <summary>
    /// 该类型下的仪器集合
    /// </summary>
    public ICollection<Instrument> Instruments { get; private set; } = new List<Instrument>();

    private InstrumentType() { }

    public InstrumentType(string name, string code, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("仪器类型名称不能为空", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("仪器类型编码不能为空", nameof(code));

        Name = name;
        Code = code;
        Description = description;
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("仪器类型名称不能为空", nameof(name));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        Status = InstrumentTypeStatus.Disabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        Status = InstrumentTypeStatus.Enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
