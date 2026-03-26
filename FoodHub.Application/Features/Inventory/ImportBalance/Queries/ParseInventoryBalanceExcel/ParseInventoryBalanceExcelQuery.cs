using FoodHub.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FoodHub.Application.Features.Inventory.ImportBalance.Queries.ParseInventoryBalanceExcel;

public record ParseInventoryBalanceExcelQuery(IFormFile File)
    : IRequest<Result<List<ParsedInventoryBalanceResponse>>>;
