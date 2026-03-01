using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Queries.GetTableById
{
    public class GetTableByIdHandler : IRequestHandler<GetTableByIdQuery, Result<GetTableByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        public GetTableByIdHandler(IUnitOfWork unitOfWork, IMessageService messageService, ICacheService cacheService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _cacheService = cacheService;
            _mapper = mapper;
        }
        public async Task<Result<GetTableByIdResponse>> Handle(GetTableByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.TableById, request.Id);

            var cachedTable = await _cacheService.GetAsync<GetTableByIdResponse>(cacheKey, cancellationToken);
            if (cachedTable != null)
            {
                return Result<GetTableByIdResponse>.Success(cachedTable);
            }

            var table = await _unitOfWork.Repository<Table>()
                .Query()
                .Include(t => t.Area)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == request.Id, cancellationToken);
            if (table == null)
            {
                return Result<GetTableByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Table.NotFound));
            }

            //var response = new GetTableByIdResponse
            //{
            //    TableId = table.TableId,
            //    TableCode = table.TableCode,
            //    Capacity = table.Capacity,
            //    Area = table.Area.Name,
            //    Status = table.Status.ToString(),
            //    CreatedAt = table.CreatedAt,
            //    CreatedBy = table.CreatedBy,
            //    UpdatedAt = table.UpdatedAt,
            //    UpdatedBy = table.UpdatedBy,
            //    DeletedAt = table.DeletedAt
            //};

            var response = _mapper.Map<GetTableByIdResponse>(table);

            await _cacheService.SetAsync(cacheKey, response, CacheTTL.Tables, cancellationToken);
            return Result<GetTableByIdResponse>.Success(response);
        }
    }
}
