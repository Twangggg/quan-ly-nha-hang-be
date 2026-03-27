using FoodHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FoodHub.Application.Features.Inventory.ImportBalance.Commands.Import;

public record ImportInventoryBalanceCommand(IFormFile File, bool ConfirmOverwrite = false)
    : IRequest<Result<ImportInventoryBalanceResponse>>;