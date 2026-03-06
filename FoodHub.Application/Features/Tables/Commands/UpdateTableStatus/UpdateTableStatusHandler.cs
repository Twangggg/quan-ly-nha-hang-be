using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Commands.UpdateTableStatus
{
    /// <summary>
    /// Handler for updating the status of a table.
    /// It retrieves the table from the database using the provided TableId, updates its status, and saves the changes.
    /// If the update is successful, it returns the updated table information in a response object.
    /// If the table is not found, it returns a failure result with an appropriate error message.
    /// </summary>
    public class UpdateTableStatusHandler : IRequestHandler<UpdateTableStatusCommand, Result<UpdateTableStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Constructor for UpdateTableStatusHandler.
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="mapper"></param>
        /// <param name="currentUserService"></param>
        /// <param name="messageService"></param>
        /// <param name="cacheService"></param>
        public UpdateTableStatusHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Handles the update of a table's status.
        /// It first checks if the table exists, then updates its status and saves the changes to the database.
        /// After saving, it removes relevant cache entries to ensure data consistency.
        /// Finally, it maps the updated table to a response object and returns it wrapped in a success result.
        /// If the table is not found, it returns a failure result with an appropriate error message.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<UpdateTableStatusResponse>> Handle(UpdateTableStatusCommand request, CancellationToken cancellationToken)
        {
            // Retrieve the table from the database
            var tableRepository = _unitOfWork.Repository<Table>();
            var table = tableRepository
                .Query()
                .Include(t => t.Area)
                .FirstOrDefault(t => t.TableId == request.TableId);
            if (table is null)
            {
                var errorMessage = _messageService.GetMessage(MessageKeys.Table.NotFound, request.TableId);
                return Result<UpdateTableStatusResponse>.NotFound(errorMessage);
            }

            // Get the current user's ID to set as the auditor for the update
            Guid? auditorId = null;
            if (Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                auditorId = userId;
            }

            // Update the table's status and audit information
            table.Status = request.Status;
            table.UpdatedBy = auditorId;
            table.UpdatedAt = DateTime.UtcNow;

            // Update the table in the database and save changes
            tableRepository.Update(table);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _cacheService.RemoveAsync(CacheKey.AreaList, cancellationToken);
            await _cacheService.RemoveByPatternAsync(string.Format(CacheKey.TableList), cancellationToken);
            await _cacheService.RemoveByPatternAsync(string.Format(CacheKey.TableListByArea, table.AreaId), cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.TableById, request.TableId), cancellationToken);

            // Commit the transaction after successful update
            var response = _mapper.Map<UpdateTableStatusResponse>(table);
            return Result<UpdateTableStatusResponse>.Success(response);
        }
    }
}
