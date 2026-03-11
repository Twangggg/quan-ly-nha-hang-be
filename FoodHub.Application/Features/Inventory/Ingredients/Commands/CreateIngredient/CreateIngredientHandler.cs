using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient
{
    public class CreateIngredientHandler
        : IRequestHandler<CreateIngredientCommand, Result<CreateIngredientResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateIngredientHandler> _logger;

        public CreateIngredientHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<CreateIngredientHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<CreateIngredientResponse>> Handle(
            CreateIngredientCommand request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling CreateIngredient for Code={Code}, Name={Name}",
                request.Code,
                request.Name
            );

            try
            {
                var repo = _unitOfWork.Repository<Ingredient>();

                // Check if Code exists
                var codeExists = await repo.AnyAsync(x => x.Code.ToLower() == request.Code.ToLower());
                if (codeExists)
                {
                    return Result<CreateIngredientResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Ingredient.CodeExists),
                        ResultErrorType.Conflict
                    );
                }

                // Check if Name exists
                var nameExists = await repo.AnyAsync(x => x.Name.ToLower() == request.Name.ToLower());
                if (nameExists)
                {
                    return Result<CreateIngredientResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Ingredient.NameExists),
                        ResultErrorType.Conflict
                    );
                }

                var ingredient = Ingredient.Create(
                    request.Code,
                    request.Name,
                    request.Unit,
                    request.LowStockThreshold,
                    request.Description
                );

                await repo.AddAsync(ingredient);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Invalidate cache if needed
                // await _cacheService.RemoveAsync(CacheKey.IngredientList, cancellationToken);

                var response = new CreateIngredientResponse
                {
                    IngredientId = ingredient.IngredientId,
                    Code = ingredient.Code,
                    Name = ingredient.Name,
                    Unit = ingredient.Unit,
                    LowStockThreshold = ingredient.LowStockThreshold,
                    StockStatus = ingredient.StockStatus,
                    CreatedAt = ingredient.CreatedAt,
                };

                _logger.LogInformation(
                    "End handling CreateIngredient for IngredientId={IngredientId}",
                    ingredient.IngredientId
                );
                return Result<CreateIngredientResponse>.Success(response);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating ingredient Code={Code}, Name={Name}",
                    request.Code,
                    request.Name
                );
                throw;
            }
        }
    }
}
