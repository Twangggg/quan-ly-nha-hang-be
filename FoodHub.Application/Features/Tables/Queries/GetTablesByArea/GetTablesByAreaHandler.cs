using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Application.Interfaces.Inventory;
using FoodHub.Application.Interfaces.Messaging;
using FoodHub.Application.Interfaces.Reporting;
using FoodHub.Application.Interfaces.External;
using FoodHub.Application.Interfaces.Security;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Tables.Queries.GetTablesByArea
{
    public class GetTablesByAreaHandler : IRequestHandler<GetTablesByAreaQuery, Result<List<GetTablesByAreaResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public GetTablesByAreaHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<List<GetTablesByAreaResponse>>> Handle(GetTablesByAreaQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKey.TableListByArea, request.AreaId);
            var cachedResult = await _cacheService.GetAsync<List<GetTablesByAreaResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return Result<List<GetTablesByAreaResponse>>.Success(cachedResult);
            }

            var tableRepository = _unitOfWork.Repository<Table>();
            var tables = await tableRepository.Query()
                .Include(t => t.Area)
                .Where(t => t.AreaId == request.AreaId)
                .OrderBy(t => t.TableNumber)
                .ProjectTo<GetTablesByAreaResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, tables, CacheTTL.Tables);

            return Result<List<GetTablesByAreaResponse>>.Success(tables);
        }
    }
}
