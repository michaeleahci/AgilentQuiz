using AgilentQuiz.Domain.Entities;

namespace AgilentQuiz.Domain.Interfaces;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
}
