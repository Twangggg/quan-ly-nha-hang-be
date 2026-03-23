using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Helpers;
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
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GetIngredientById
{
    public class GetIngredientByIdHandler
        : IRequestHandler<GetIngredientByIdQuery, Result<GetIngredientByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetIngredientByIdHandler> _logger;

        public GetIngredientByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            IMessageService messageService,
            ILogger<GetIngredientByIdHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetIngredientByIdResponse>> Handle(
            GetIngredientByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GetIngredientById for {IngredientId}",
                request.IngredientId
            );

            try
            {
                var cacheKey = string.Format(CacheKey.InventoryIngredientById, request.IngredientId);
                var cached = await _cacheService.GetAsync<GetIngredientByIdResponse>(
                    cacheKey,
                    cancellationToken
                );
                if (cached is not null)
                {
                    _logger.LogInformation(
                        "End handling GetIngredientById for {IngredientId} (from cache)",
                        request.IngredientId
                    );
                    return Result<GetIngredientByIdResponse>.Success(cached);
                }

                var response = await _unitOfWork
                    .Repository<Ingredient>()
                    .Query()
                    .AsNoTracking()
                    .Where(x => x.IngredientId == request.IngredientId)
                    .ProjectTo<GetIngredientByIdResponse>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(cancellationToken);

                if (response == null)
                {
                    _logger.LogWarning("Ingredient {IngredientId} not found", request.IngredientId);
                    return Result<GetIngredientByIdResponse>.NotFound(
                        _messageService.GetMessage(MessageKeys.Ingredient.NotFound)
                    );
                }

                _logger.LogInformation(
                    "End handling GetIngredientById for {IngredientId}",
                    request.IngredientId
                );
                await _cacheService.SetAsync(
                    cacheKey,
                    response,
                    CacheTTL.Inventory,
                    cancellationToken
                );
                return Result<GetIngredientByIdResponse>.Success(response);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while getting ingredient {IngredientId}",
                    request.IngredientId
                );
                throw;
            }
        }
    }
}
