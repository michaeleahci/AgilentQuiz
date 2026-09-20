using AgilentQuiz.Application.Common;
using AgilentQuiz.Application.DTOs.Instrument;
using AgilentQuiz.Domain.Entities;
using AgilentQuiz.Domain.Enums;
using AgilentQuiz.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgilentQuiz.Application.Services;

/// <summary>
/// 仪器类型管理应用服务实现
/// </summary>
public class InstrumentTypeAppService : IInstrumentTypeAppService
{
    private readonly IInstrumentTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InstrumentTypeAppService> _logger;

    public InstrumentTypeAppService(
        IInstrumentTypeRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<InstrumentTypeAppService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<InstrumentTypeResponse> CreateAsync(CreateInstrumentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing != null)
            throw new BusinessException(ErrorCodes.InstrumentTypeCodeExists, "仪器类型编码已存在");

        var instrumentType = new InstrumentType(request.Name, request.Code, request.Description);
        await _repository.AddAsync(instrumentType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("InstrumentType created: {Id}", instrumentType.Id);
        return MapToResponse(instrumentType);
    }

    public async Task<InstrumentTypeResponse> UpdateAsync(Guid id, UpdateInstrumentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var instrumentType = await _repository.GetByIdAsync(id, cancellationToken);
        if (instrumentType == null)
            throw new BusinessException(ErrorCodes.InstrumentTypeNotFound, "仪器类型不存在");

        instrumentType.Update(request.Name, request.Description);
        _repository.Update(instrumentType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(instrumentType);
    }

    public async Task DisableAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrumentType = await _repository.GetByIdAsync(id, cancellationToken);
        if (instrumentType == null)
            throw new BusinessException(ErrorCodes.InstrumentTypeNotFound, "仪器类型不存在");

        instrumentType.Disable();
        _repository.Update(instrumentType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InstrumentTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(MapToResponse).ToList();
    }

    public async Task<InstrumentTypeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrumentType = await _repository.GetByIdAsync(id, cancellationToken);
        if (instrumentType == null)
            throw new BusinessException(ErrorCodes.InstrumentTypeNotFound, "仪器类型不存在");

        return MapToResponse(instrumentType);
    }

    private static InstrumentTypeResponse MapToResponse(InstrumentType entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        Description = entity.Description,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
    };
}
