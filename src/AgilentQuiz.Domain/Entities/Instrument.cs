using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 仪器设备
/// </summary>
public class Instrument : Entity
{
    /// <summary>
    /// 所属仪器类型ID
    /// </summary>
    public Guid InstrumentTypeId { get; private set; }

    /// <summary>
    /// 仪器名称
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// 仪器编码（唯一）
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// 状态
    /// </summary>
    public InstrumentStatus Status { get; private set; } = InstrumentStatus.Available;

    /// <summary>
    /// 乐观并发令牌
    /// </summary>
    public byte[] RowVersion { get; private set; } = null!;

    public InstrumentType InstrumentType { get; private set; } = null!;
    public ICollection<ReservationItem> ReservationItems { get; private set; } = new List<ReservationItem>();

    private Instrument() { }

    public Instrument(Guid instrumentTypeId, string name, string code)
    {
        if (instrumentTypeId == Guid.Empty)
            throw new ArgumentException("仪器类型ID无效", nameof(instrumentTypeId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("仪器名称不能为空", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("仪器编码不能为空", nameof(code));

        InstrumentTypeId = instrumentTypeId;
        Name = name;
        Code = code;
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("仪器名称不能为空", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFault()
    {
        Status = InstrumentStatus.Faulty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkScrapped()
    {
        Status = InstrumentStatus.Scrapped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Recover()
    {
        Status = InstrumentStatus.Available;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeReserved() => Status == InstrumentStatus.Available;
}
