namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 实验人员（用户）
/// </summary>
public class User : Entity
{
    /// <summary>
    /// 联系手机号（唯一标识）
    /// </summary>
    public string Phone { get; private set; } = null!;

    /// <summary>
    /// 姓名
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// 违约禁用到期时间（UTC）。若当前时间早于此值，则禁止预约。
    /// </summary>
    public DateTime? BanExpiryTime { get; private set; }

    public ICollection<Reservation> Reservations { get; private set; } = new List<Reservation>();
    public ICollection<Notification> Notifications { get; private set; } = new List<Notification>();

    private User() { }

    public User(string phone, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("手机号不能为空", nameof(phone));

        Phone = phone;
        Name = name;
    }

    /// <summary>
    /// 是否已被禁用（违约惩罚期内）
    /// </summary>
    public bool IsBanned() => BanExpiryTime.HasValue && BanExpiryTime.Value > DateTime.UtcNow;

    /// <summary>
    /// 应用违约惩罚：禁用指定时长
    /// </summary>
    public void ApplyBan(TimeSpan duration)
    {
        BanExpiryTime = DateTime.UtcNow.Add(duration);
        UpdatedAt = DateTime.UtcNow;
    }
}
