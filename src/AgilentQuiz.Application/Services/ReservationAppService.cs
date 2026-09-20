using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Reservation;
using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Interfaces;
using AgilentQuiz.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 预约应用服务实现
/// </summary>
public class ReservationAppService : IReservationAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IInstrumentTypeRepository _instrumentTypeRepository;
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IReservationDomainService _reservationDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationAppService> _logger;

    public ReservationAppService(
        IUserRepository userRepository,
        IInstrumentTypeRepository instrumentTypeRepository,
        IInstrumentRepository instrumentRepository,
        IReservationRepository reservationRepository,
        IReservationDomainService reservationDomainService,
        IUnitOfWork unitOfWork,
        ILogger<ReservationAppService> logger)
    {
        _userRepository = userRepository;
        _instrumentTypeRepository = instrumentTypeRepository;
        _instrumentRepository = instrumentRepository;
        _reservationRepository = reservationRepository;
        _reservationDomainService = reservationDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ReservationResponse> CreateAsync(CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        // 幂等处理：若存在相同幂等键的预约，直接返回（只读查询可在事务外）
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _reservationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Idempotent request, returning existing reservation {Id}", existing.Id);
                return MapToResponse(existing);
            }
        }

        // 获取或创建用户（处理并发同手机号场景，捕获唯一索引冲突后重试查询）
        var user = await GetOrCreateUserAsync(request.Phone, cancellationToken);

        // 校验仪器类型（只读）
        var instrumentType = await _instrumentTypeRepository.GetByIdAsync(request.InstrumentTypeId, cancellationToken);
        if (instrumentType == null)
            throw new BusinessException(ErrorCodes.InstrumentTypeNotFound, "仪器类型不存在");
        if (instrumentType.Status == Domain.Enums.InstrumentTypeStatus.Disabled)
            throw new BusinessException(ErrorCodes.InstrumentTypeDisabled, "该仪器类型已被禁用");

        // 构建预约实体
        var reservation = new Reservation(
            user.Id,
            request.Phone,
            request.InstrumentTypeId,
            request.StartTime.ToUniversalTime(),
            request.EndTime.ToUniversalTime(),
            request.Remark,
            request.IdempotencyKey);

        var distinctInstrumentIds = request.InstrumentIds.Distinct().ToList();
        foreach (var instrumentId in distinctInstrumentIds)
        {
            reservation.AddItem(instrumentId);
        }

        // ===== 事务内执行冲突检测 + 插入（Serializable 隔离 + UPDLOCK 防止超卖）=====
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var validationError = await _reservationDomainService.ValidateReservationAsync(
                reservation, distinctInstrumentIds, cancellationToken);
            if (validationError != null)
                throw new BusinessException(ErrorCodes.InstrumentConflict, validationError);

            await _reservationRepository.AddAsync(reservation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Reservation created: {Id}", reservation.Id);
            return MapToResponse(reservation);
        }, cancellationToken);
    }

    /// <summary>
    /// 获取或创建用户。处理并发场景：两个请求同时为新手机号创建用户时，
    /// 依赖 Users.Phone 唯一索引保证只有一个插入成功，失败方重试查询。
    /// </summary>
    private async Task<User> GetOrCreateUserAsync(string phone, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
        if (user != null) return user;

        user = new User(phone);
        try
        {
            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (Exception)
        {
            // 并发下唯一索引冲突，重新查询
            var existing = await _userRepository.GetByPhoneAsync(phone, cancellationToken);
            if (existing != null) return existing;
            throw;
        }
    }

    public async Task<ReservationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(id, cancellationToken);
        if (reservation == null)
            throw new BusinessException(ErrorCodes.ReservationNotFound, "预约单不存在");

        return MapToResponse(reservation);
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetActiveByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetActiveByPhoneAsync(phone, cancellationToken);
        return reservations.Select(MapToResponse).ToList();
    }

    public async Task<IReadOnlyList<ReservationResponse>> GetHistoryByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetByPhoneAsync(phone, cancellationToken);
        return reservations.Select(MapToResponse).ToList();
    }

    public async Task<ReservationResponse> CancelAsync(Guid reservationId, CancelReservationRequest request, CancellationToken cancellationToken = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        if (reservation == null)
            throw new BusinessException(ErrorCodes.ReservationNotFound, "预约单不存在");

        if (reservation.Status != Domain.Enums.ReservationStatus.Pending)
            throw new BusinessException(ErrorCodes.ReservationCannotCancel, "仅待使用状态的预约可取消");

        if (request.InstrumentIds == null || request.InstrumentIds.Count == 0)
        {
            reservation.Cancel();
        }
        else
        {
            reservation.CancelItems(request.InstrumentIds);
        }

        _reservationRepository.Update(reservation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(reservation);
    }

    private static ReservationResponse MapToResponse(Reservation reservation)
    {
        var response = new ReservationResponse
        {
            Id = reservation.Id,
            Phone = reservation.Phone,
            InstrumentTypeId = reservation.InstrumentTypeId,
            InstrumentTypeName = reservation.InstrumentType?.Name ?? string.Empty,
            StartTime = reservation.StartTime,
            EndTime = reservation.EndTime,
            Status = reservation.Status,
            Remark = reservation.Remark,
            CreatedAt = reservation.CreatedAt,
            Items = reservation.Items.Select(item => new ReservationItemResponse
            {
                Id = item.Id,
                InstrumentId = item.InstrumentId,
                InstrumentName = item.Instrument?.Name ?? string.Empty,
                InstrumentCode = item.Instrument?.Code ?? string.Empty,
                Status = item.Status
            }).ToList()
        };
        return response;
    }
}
