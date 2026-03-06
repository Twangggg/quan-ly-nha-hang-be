using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTable
{
    /// <summary>
    /// Handler for updating a table's information, including capacity and area assignment. It validates the existence of the table and area, updates the table's properties, and manages caching to ensure data consistency.
    /// </summary>
    public class UpdateTableHandler : IRequestHandler<UpdateTableCommand, Result<UpdateTableResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Constructor to inject dependencies for the UpdateTableHandler.
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="mapper"></param>
        /// <param name="currentUserService"></param>
        /// <param name="messageService"></param>
        /// <param name="cacheService"></param>
        public UpdateTableHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Handles the UpdateTableCommand to update a table's capacity and area. It checks for the existence of the table and area, updates the table's properties, saves changes to the database, and manages cache invalidation to ensure that subsequent reads reflect the updated data.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<UpdateTableResponse>> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
        {
            // Validate that the table exists
            var tableRepository = _unitOfWork.Repository<Table>();
            var table = await tableRepository
                .Query()
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.TableId == request.TableId, cancellationToken);
            if (table == null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Table.NotFound, request.TableId);
                return Result<UpdateTableResponse>.NotFound(errorMessage);
            }

            // Update the table's properties
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            table.TableNumber = request.TableNumber;
            table.Capacity = request.Capacity;
            table.AreaId = request.AreaId;
            table.UpdatedAt = DateTime.UtcNow;
            table.UpdatedBy = auditorId;

            // Update the table in the repository
            tableRepository.Update(table);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveByPatternAsync(CacheKey.TableList + "*", cancellationToken);
            await _cacheService.RemoveByPatternAsync(string.Format(CacheKey.TableListByArea, "*"), cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.TableById, request.TableId), cancellationToken);

            // Map the updated table to the response DTO
            var response = _mapper.Map<UpdateTableResponse>(table);
            return Result<UpdateTableResponse>.Success(response);
        }
    }
}
