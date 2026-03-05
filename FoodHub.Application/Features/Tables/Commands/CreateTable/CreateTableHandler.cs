using AutoMapper;
using FoodHub.Application.Common.Constants;
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
    /// <summary>
    /// Handler for creating a new table in the restaurant. It validates the input, checks for existing tables with the same code,
    /// </summary>
    public class CreateTableHandler
        : IRequestHandler<CreateTableCommand, Result<CreateTableResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateTableHandler> _logger;

        /// <summary>
        /// Constructor to inject dependencies for the CreateTableHandler, including database access, user context, messaging, caching, mapping, and logging services.
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="currentUserService"></param>
        /// <param name="messageService"></param>
        /// <param name="cacheService"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        public CreateTableHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<CreateTableHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Handles the creation of a new table by validating the request, checking for duplicates, ensuring the specified area exists, and then adding the new table to the database. It also manages caching and returns an appropriate response based on the outcome of the operation.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<CreateTableResponse>> Handle(
            CreateTableCommand request,
            CancellationToken cancellationToken
        )
        {
            // Validate the request data
            var tableRepo = _unitOfWork.Repository<Table>();
            var areaRepo = _unitOfWork.Repository<Area>();

            // Check if the specified area exists and is active
            var existingArea = await areaRepo
                .Query()
                .FirstOrDefaultAsync(a => a.AreaId == request.AreaId);
            if (existingArea is null)
            {
                _logger.LogWarning("Area with ID {AreaId} does not exist", request.AreaId);
                return Result<CreateTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Area.NotFound, request.AreaId),
                    ResultErrorType.NotFound
                );
            }

            // Không cho tạo bàn trong khu vực đang Inactive
            if (existingArea.Status == AreaStatus.Inactive)
            {
                _logger.LogWarning(
                    "Cannot create table — Area {AreaId} is Inactive",
                    request.AreaId
                );
                return Result<CreateTableResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Area.Inactive),
                    ResultErrorType.Conflict
                );
            }

            // Create a new table entity and populate its properties
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            // Generate the next table number for the specified area
            var tableNumber =
                await tableRepo
                    .Query()
                    .Where(t => t.AreaId == request.AreaId)
                    .MaxAsync(t => (int?)t.TableNumber)
                ?? 0;
            tableNumber++;

            // Log the incoming request for traceability
            _logger.LogInformation(
                "Handling CreateTableCommand for TableNumber: {TableNumber}",
                tableNumber
            );

            // Create the new table entity
            var newTable = new Table
            {
                TableId = Guid.NewGuid(),
                TableNumber = tableNumber,
                Capacity = request.Capacity,
                AreaId = request.AreaId,
                Status = TableStatus.Available,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = auditorId,
            };

            // Add the new table to the repository and save changes
            await tableRepo.AddAsync(newTable);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                string.Format(CacheKey.TableList),
                cancellationToken
            );
            await _cacheService.RemoveByPatternAsync(
                string.Format(CacheKey.TableListByArea, request.AreaId),
                cancellationToken
            );

            // Map the newly created table to the response DTO and return a success result
            var response = _mapper.Map<CreateTableResponse>(newTable);
            return Result<CreateTableResponse>.Success(response);
        }
    }
}
