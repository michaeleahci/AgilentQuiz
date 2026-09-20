using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Instrument;
using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using AgilentQuiz.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 仪器管理应用服务实现
/// </summary>
public class InstrumentAppService : IInstrumentAppService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IInstrumentTypeRepository _instrumentTypeRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InstrumentAppService> _logger;

    public InstrumentAppService(
        IInstrumentRepository instrumentRepository,
        IInstrumentTypeRepository instrumentTypeRepository,
        IReservationRepository reservationRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<InstrumentAppService> logger)
    {
        _instrumentRepository = instrumentRepository;
        _instrumentTypeRepository = instrumentTypeRepository;
        _reservationRepository = reservationRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InstrumentResponse> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken = default)
    {
        var instrumentType = await _instrumentTypeRepository.GetByIdAsync(request.InstrumentTypeId, cancellationToken);
        if (instrumentType == null)
            throw new BusinessException(ErrorCodes.InstrumentTypeNotFound, "仪器类型不存在");

        var existing = await _instrumentRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing != null)
            throw new BusinessException(ErrorCodes.InstrumentCodeExists, "仪器编码已存在");

        var instrument = new Instrument(request.InstrumentTypeId, request.Name, request.Code);
        await _instrumentRepository.AddAsync(instrument, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(instrument, instrumentType.Name);
    }

    public async Task<InstrumentResponse> UpdateAsync(Guid id, UpdateInstrumentRequest request, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument == null)
            throw new BusinessException(ErrorCodes.InstrumentNotFound, "仪器不存在");

        instrument.Update(request.Name);
        _instrumentRepository.Update(instrument);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var type = await _instrumentTypeRepository.GetByIdAsync(instrument.InstrumentTypeId, cancellationToken);
        return MapToResponse(instrument, type?.Name ?? string.Empty);
    }

    /// <summary>
    /// 标记仪器故障：自动识别受影响预约并通知用户
    /// </summary>
    public async Task MarkFaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument == null)
            throw new BusinessException(ErrorCodes.InstrumentNotFound, "仪器不存在");

        instrument.MarkFault();
        _instrumentRepository.Update(instrument);

        // 查找受影响的有效预约并创建通知
        var affectedReservations = await _reservationRepository.GetActiveByInstrumentAsync(id, cancellationToken);
        foreach (var reservation in affectedReservations)
        {
            var notification = new Notification(
                reservation.UserId,
                NotificationType.InstrumentFault,
                $"您预约的仪器 {instrument.Code}({instrument.Name}) 已被标记为故障，预约时间段 {reservation.StartTime:yyyy-MM-dd HH:mm} ~ {reservation.EndTime:yyyy-MM-dd HH:mm} 受影响，请重新预约或改约。",
                reservation.Id);
            await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Instrument {Id} marked as fault, {Count} reservations affected", id, affectedReservations.Count);
    }

    /// <summary>
    /// 标记仪器报废：不允许新预约，自动处理已存在预约并通知
    /// </summary>
    public async Task MarkScrappedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument == null)
            throw new BusinessException(ErrorCodes.InstrumentNotFound, "仪器不存在");

        instrument.MarkScrapped();
        _instrumentRepository.Update(instrument);

        // 处理已存在的有效预约：取消这些预约项并通知用户
        var affectedReservations = await _reservationRepository.GetActiveByInstrumentAsync(id, cancellationToken);
        foreach (var reservation in affectedReservations)
        {
            reservation.CancelItems(new[] { instrument.Id });
            _reservationRepository.Update(reservation);

            var notification = new Notification(
                reservation.UserId,
                NotificationType.InstrumentScrapped,
                $"您预约的仪器 {instrument.Code}({instrument.Name}) 已被报废，相关预约已自动取消，请重新预约其他仪器。",
                reservation.Id);
            await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Instrument {Id} marked as scrapped, {Count} reservations affected", id, affectedReservations.Count);
    }

    public async Task RecoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument == null)
            throw new BusinessException(ErrorCodes.InstrumentNotFound, "仪器不存在");

        instrument.Recover();
        _instrumentRepository.Update(instrument);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<InstrumentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument == null)
            throw new BusinessException(ErrorCodes.InstrumentNotFound, "仪器不存在");

        var type = await _instrumentTypeRepository.GetByIdAsync(instrument.InstrumentTypeId, cancellationToken);
        return MapToResponse(instrument, type?.Name ?? string.Empty);
    }

    public async Task<IReadOnlyList<InstrumentResponse>> GetByTypeAsync(Guid instrumentTypeId, CancellationToken cancellationToken = default)
    {
        var instruments = await _instrumentRepository.GetByTypeAsync(instrumentTypeId, cancellationToken);
        var type = await _instrumentTypeRepository.GetByIdAsync(instrumentTypeId, cancellationToken);
        var typeName = type?.Name ?? string.Empty;

        return instruments.Select(i => MapToResponse(i, typeName)).ToList();
    }

    public async Task<IReadOnlyList<InstrumentUsageResponse>> GetUsageAsync(Guid? instrumentTypeId, CancellationToken cancellationToken = default)
    {
        var instruments = instrumentTypeId.HasValue
            ? await _instrumentRepository.GetByTypeAsync(instrumentTypeId.Value, cancellationToken)
            : (await _instrumentTypeRepository.GetAllAsync(cancellationToken))
                .SelectMany(t => t.Instruments)
                .ToList();

        var result = new List<InstrumentUsageResponse>();
        foreach (var instrument in instruments)
        {
            var activeReservations = await _reservationRepository.GetActiveByInstrumentAsync(instrument.Id, cancellationToken);
            result.Add(new InstrumentUsageResponse
            {
                InstrumentId = instrument.Id,
                InstrumentName = instrument.Name,
                InstrumentCode = instrument.Code,
                Status = instrument.Status,
                ActiveReservationCount = activeReservations.Count,
                UpcomingSlots = activeReservations.Select(r => new ReservationSlot
                {
                    ReservationId = r.Id,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    Phone = r.Phone
                }).ToList()
            });
        }

        return result;
    }

    private static InstrumentResponse MapToResponse(Instrument entity, string typeName) => new()
    {
        Id = entity.Id,
        InstrumentTypeId = entity.InstrumentTypeId,
        InstrumentTypeName = typeName,
        Name = entity.Name,
        Code = entity.Code,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
