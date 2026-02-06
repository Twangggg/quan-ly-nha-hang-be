using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public record GetAllCategoriesQuery() : IRequest<Result<List<GetCategoriesResponse>>>;
}
