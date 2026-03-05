using FoodHub.Application.Common.Models;
using FoodHub.Application.Features.Areas.Queries.GetAreaById;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Features.Areas.Commands.UpdateArea
{
    public record UpdateAreaCommand : IRequest<Result<GetAreaByIdResponse>>
    {
        public Guid AreaId { get; init; }
        public required string Name { get; init; }
        public required string CodePrefix { get; init; }
        public string? Description { get; init; }
        public required AreaType Type { get; init; }
    }
}
