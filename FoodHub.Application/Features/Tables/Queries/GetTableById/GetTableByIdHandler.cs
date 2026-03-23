using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Queries.GetTableById
{
    /// <summary>
    /// Handler for retrieving a table by its ID. It checks the cache first before querying the database. If the table is found, it maps the entity to a response DTO and caches the result for future requests.
    /// </summary>
    public class GetTableByIdHandler : IRequestHandler<GetTableByIdQuery, Result<GetTableByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor to inject dependencies for the GetTableByIdHandler, including database access, messaging, caching, and mapping services.
        /// </summary>
        /// <param name="unitOfWork"></param>
        /// <param name="messageService"></param>
        /// <param name="cacheService"></param>
        /// <param name="mapper"></param>
        public GetTableByIdHandler(IUnitOfWork unitOfWork, IMessageService messageService, ICacheService cacheService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _cacheService = cacheService;
            _mapper = mapper;
        }
        /// <summary>
        /// Handles the GetTableByIdQuery by first checking the cache for the requested table. If not found in cache, it queries the database, maps the result to a response DTO, caches it, and returns the result. If the table is not found in the database, it returns a NotFound result with an appropriate message.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Result<GetTableByIdResponse>> Handle(GetTableByIdQuery request, CancellationToken cancellationToken)
        {
            // Check cache first
            var cacheKey = string.Format(CacheKey.TableById, request.Id);
            var cachedTable = await _cacheService.GetAsync<GetTableByIdResponse>(cacheKey, cancellationToken);
            if (cachedTable != null)
            {
                return Result<GetTableByIdResponse>.Success(cachedTable);
            }

            // If not in cache, query the database
            var table = await _unitOfWork.Repository<Table>()
                .Query()
                .Include(t => t.Area)
                .Include(t => t.Orders)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == request.Id, cancellationToken);
            if (table == null)
            {
                return Result<GetTableByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Table.NotFound));
            }

            // Map the table entity to the response DTO
            var response = _mapper.Map<GetTableByIdResponse>(table);
            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Tables, cancellationToken);
            return Result<GetTableByIdResponse>.Success(response);
        }
    }
}
