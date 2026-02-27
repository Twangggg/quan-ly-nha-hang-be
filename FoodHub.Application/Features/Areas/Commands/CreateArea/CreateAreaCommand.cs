using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Areas.Commands.CreateArea
{
    public record CreateAreaCommand : IRequest<Result<GetAreaByIdResponse>>
    {
        public required string Name { get; init; }
        public required string CodePrefix { get; init; }
        public AreaType Type { get; init; }
        public string? Description { get; init; }
    }
}
