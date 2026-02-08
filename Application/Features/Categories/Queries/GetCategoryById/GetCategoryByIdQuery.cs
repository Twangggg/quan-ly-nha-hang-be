using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Categories.Queries.GetCategoryById
{
    public record GetCategoryByIdQuery(Guid CategoryId) : IRequest<Result<GetCategoryByIdResponse>>;
}
