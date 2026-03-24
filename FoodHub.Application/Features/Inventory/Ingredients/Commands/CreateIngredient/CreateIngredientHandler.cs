using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Features.Inventory.Ingredients;
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

namespace FoodHub.Application.Features.Inventory.Ingredients.Commands.CreateIngredient
{
    public class CreateIngredientHandler
        : IRequestHandler<CreateIngredientCommand, Result<CreateIngredientResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CreateIngredientHandler> _logger;

        public CreateIngredientHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICacheService cacheService,
            ILogger<CreateIngredientHandler> logger,
            ICurrentUserService currentUserService
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
            _currentUserService = currentUserService;
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
                var settingsRepo = _unitOfWork.Repository<InventorySettings>();
                var defaultLowStockThreshold =
                    await settingsRepo
                        .Query()
                        .Select(x => x.DefaultLowStockThreshold)
                        .FirstOrDefaultAsync(cancellationToken);

                // Check if Name exists
                var nameExists = await repo.AnyAsync(x =>
                    x.Name.ToLower() == request.Name.ToLower()
                );
                if (nameExists)
                {
                    return Result<CreateIngredientResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Ingredient.NameExists),
                        ResultErrorType.Conflict
                    );
                }

                var generatedCode = await IngredientCodeGenerator.GenerateAsync(repo, request.Name);

                // Prevent manual initialization of stock and cost; both start at zero and are
                // managed through stock operations elsewhere in the system.
                Guid? auditorId = null;
                if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
                {
                    auditorId = parsedUserId;
                }

                var lowStockThreshold = request.UseDefaultLowStockThreshold
                    ? defaultLowStockThreshold
                    : request.LowStockThreshold;
                var ingredient = Ingredient.Create(
                    generatedCode,
                    request.Name,
                    request.BaseUnit,
                    lowStockThreshold,
                    0,
                    0,
                    request.Description,
                    auditorId
                );

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    await repo.AddAsync(ingredient);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                    await _cacheService.RemoveByPatternAsync("inventory:", cancellationToken);

                    var response = new CreateIngredientResponse
                    {
                        IngredientId = ingredient.IngredientId,
                        Code = ingredient.Code,
                        Name = ingredient.Name,
                        BaseUnit = ingredient.BaseUnit,
                        CurrentStock = ingredient.CurrentStock,
                        CostPrice = ingredient.CostPrice,
                        LowStockThreshold = ingredient.LowStockThreshold,
                        StockStatus = ingredient.GetStockStatus(),
                        Description = ingredient.Description,
                        CreatedAt = ingredient.CreatedAt,
                        CreatedBy = ingredient.CreatedBy,
                        UpdatedBy = ingredient.UpdatedBy,
                    };

                    _logger.LogInformation(
                        "End handling CreateIngredient for IngredientId={IngredientId}",
                        ingredient.IngredientId
                    );
                    return Result<CreateIngredientResponse>.Success(response);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
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
