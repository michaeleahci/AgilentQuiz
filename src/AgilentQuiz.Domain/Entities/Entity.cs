namespace AgilentQuiz.Domain.Entities;

/// <summary>
/// 实体基类
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// 主键
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
