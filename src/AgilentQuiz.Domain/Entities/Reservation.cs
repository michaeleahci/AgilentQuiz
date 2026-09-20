using AgilentQuiz.Domain.Enums;

namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 预约单（单次预约仅一种仪器类型，可包含该类型下多台仪器）
/// </summary>
public class Reservation : Entity
{
    /// <summary>
    /// 预约用户ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 联系手机号（冗余字段，便于按手机号查询）
    /// </summary>
    public string Phone { get; private set; } = null!;

    /// <summary>
    /// 仪器类型ID
    /// </summary>
    public Guid InstrumentTypeId { get; private set; }

    /// <summary>
    /// 预约开始时间（UTC）
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// 预约结束时间（UTC）
    /// </summary>
    public DateTime EndTime { get; private set; }

    /// <summary>
    /// 预约单状态
    /// </summary>
    public ReservationStatus Status { get; private set; } = ReservationStatus.Pending;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; private set; }

    /// <summary>
    /// 幂等键（创建预约时用于防重复提交）
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// 乐观并发令牌
    /// </summary>
    public byte[] RowVersion { get; private set; } = null!;

    public User User { get; private set; } = null!;
    public InstrumentType InstrumentType { get; private set; } = null!;
    public ICollection<ReservationItem> Items { get; private set; } = new List<ReservationItem>();

    private Reservation() { }

    public Reservation(
        Guid userId,
        string phone,
        Guid instrumentTypeId,
        DateTime startTime,
        DateTime endTime,
        string? remark = null,
        string? idempotencyKey = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID无效", nameof(userId));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("手机号不能为空", nameof(phone));
        if (instrumentTypeId == Guid.Empty)
            throw new ArgumentException("仪器类型ID无效", nameof(instrumentTypeId));
        if (endTime <= startTime)
            throw new ArgumentException("结束时间必须晚于开始时间", nameof(endTime));

        UserId = userId;
        Phone = phone;
        InstrumentTypeId = instrumentTypeId;
        StartTime = startTime;
        EndTime = endTime;
        Remark = remark;
        IdempotencyKey = idempotencyKey;
    }

    public void AddItem(Guid instrumentId)
    {
        if (Items.Any(i => i.InstrumentId == instrumentId))
            return;

        Items.Add(new ReservationItem(Id, instrumentId));
    }

    /// <summary>
    /// 取消整个预约单
    /// </summary>
    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
        foreach (var item in Items)
        {
            item.Cancel();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消预约单中的部分仪器
    /// </summary>
    public void CancelItems(IEnumerable<Guid> instrumentIds)
    {
        var ids = instrumentIds.ToHashSet();
        foreach (var item in Items.Where(i => ids.Contains(i.InstrumentId)))
        {
            item.Cancel();
        }

        // 若所有明细均已取消，则预约单整体取消
        if (Items.All(i => i.Status == ReservationItemStatus.Cancelled))
        {
            Status = ReservationStatus.Cancelled;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记为违约（由定时任务触发）
    /// </summary>
    public void MarkDefaulted()
    {
        Status = ReservationStatus.Defaulted;
        foreach (var item in Items.Where(i => i.Status == ReservationItemStatus.Pending))
        {
            item.MarkDefaulted();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记为已完成
    /// </summary>
    public void MarkCompleted()
    {
        Status = ReservationStatus.Completed;
        foreach (var item in Items.Where(i => i.Status == ReservationItemStatus.Pending))
        {
            item.MarkCompleted();
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 是否存在有效的（待使用/使用中）预约项
    /// </summary>
    public bool HasActiveItems() =>
        Items.Any(i => i.Status == ReservationItemStatus.Pending || i.Status == ReservationItemStatus.Completed);
}
