using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Commands.DeleteTable
{
    /// <summary>
    /// Handler for deleting a table. It checks if the table exists, marks it as deleted, and updates the cache accordingly.
    /// </summary>
    public class DeleteTableHandler : IRequestHandler<DeleteTableCommand, Result<DeleteTableResponse>>
    {
        // Dependencies for database operations, mapping, user context, messaging, and caching
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Constructor to inject dependencies for the DeleteTableHandler.
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="mapper"></param>
        /// <param name="currentUserService"></param>
        /// <param name="messageService"></param>
        /// <param name="cacheService"></param>
        public DeleteTableHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Handles the deletion of a table. It first checks if the table exists, then marks it as deleted by setting the DeletedAt timestamp and UpdatedBy user. After saving changes to the database, it removes relevant cache entries to ensure data consistency. Finally, it maps the deleted table to a response object and returns it wrapped in a success result. If the table is not found, it returns a failure result with an appropriate error message.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<DeleteTableResponse>> Handle(DeleteTableCommand request, CancellationToken cancellationToken)
        {
            // Retrieve the table from the database using the provided TableId
            var tableRepository = _unitOfWork.Repository<Table>();
            var table = await tableRepository
                .Query()
                .Include(t => t.Area)
                .FirstOrDefaultAsync(t => t.TableId == request.TableId, cancellationToken);
            if (table is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Table.NotFound, request.TableId);
                return Result<DeleteTableResponse>.Failure(errorMessage);
            }

            // Get the current user's ID to set as the auditor for the update
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var parsedId))
            {
                auditorId = parsedId;
            }

            // Mark the table as deleted by setting the DeletedAt timestamp and UpdatedBy user
            table.DeletedAt = DateTime.UtcNow;
            table.UpdatedBy = auditorId;

            // Update the table in the repository and save changes to the database
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveByPatternAsync(string.Format(CacheKey.TableList), cancellationToken);
            await _cacheService.RemoveByPatternAsync(string.Format(CacheKey.TableListByArea, table.AreaId), cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.TableById, request.TableId), cancellationToken);

            // Map the deleted table to a response object and return it wrapped in a success result
            var response = _mapper.Map<DeleteTableResponse>(table);
            return Result<DeleteTableResponse>.Success(response);
        }
    }
}
