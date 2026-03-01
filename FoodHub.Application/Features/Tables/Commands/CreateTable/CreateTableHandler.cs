using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public class CreateTableHandler : IRequestHandler<CreateTableCommand, Result<CreateTableResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateTableHandler> _logger;

        public CreateTableHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<CreateTableHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CreateTableResponse>> Handle(CreateTableCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling CreateTableCommand for TableCode: {TableCode}", request.TableCode);

            var tableRepo = _unitOfWork.Repository<Table>();
            var areaRepo = _unitOfWork.Repository<Area>();

            // Check if a table with the same code already exists
            var existingTable = await tableRepo.Query().AnyAsync(t => t.TableCode == request.TableCode);
            if (existingTable)
            {
                _logger.LogWarning("Table with code {TableCode} already exists", request.TableCode);
                return Result<CreateTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Table.CodeExists, request.TableCode),
                    ResultErrorType.Conflict);
            }

            // Check if the specified area exists
            var existingArea = await areaRepo.Query().AnyAsync(a => a.AreaId == request.AreaId);
            if (!existingArea)
            {
                _logger.LogWarning("Area with ID {AreaId} does not exist", request.AreaId);
                return Result<CreateTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Area.NotFound, request.AreaId),
                    ResultErrorType.NotFound);
            }

            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            var newTable = new Table
            {
                TableId = Guid.NewGuid(),
                TableCode = request.TableCode,
                Capacity = request.Capacity,
                AreaId = request.AreaId,
                Status = TableStatus.Available,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = auditorId
            };

            await tableRepo.AddAsync(newTable);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync("table:list", cancellationToken);

            var response = new CreateTableResponse
            {
                TableId = newTable.TableId,
                TableCode = newTable.TableCode,
                Capacity = newTable.Capacity,
                Status = newTable.Status,
                CreatedAt = newTable.CreatedAt,
                CreatedBy = newTable.CreatedBy
            };

            return Result<CreateTableResponse>.Success(response);
        }
    }
}
