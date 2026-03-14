using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.Inventory.Ingredients.Queries.GenerateIngredientCode
{
    public class GenerateIngredientCodeHandler
        : IRequestHandler<GenerateIngredientCodeQuery, Result<GenerateIngredientCodeResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GenerateIngredientCodeHandler> _logger;

        public GenerateIngredientCodeHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<GenerateIngredientCodeHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GenerateIngredientCodeResponse>> Handle(
            GenerateIngredientCodeQuery request,
            CancellationToken cancellationToken
        )
        {
            _logger.LogInformation(
                "Start handling GenerateIngredientCode for Name={Name}",
                request.Name
            );

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<GenerateIngredientCodeResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Ingredient.NameRequired)
                );
            }

            try
            {
                var repo = _unitOfWork.Repository<Ingredient>();
                var generatedCode = await IngredientCodeGenerator.GenerateAsync(repo, request.Name);

                return Result<GenerateIngredientCodeResponse>.Success(
                    new GenerateIngredientCodeResponse { Code = generatedCode }
                );
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while generating ingredient code for Name={Name}",
                    request.Name
                );
                throw;
            }
        }
    }
}
