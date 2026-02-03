using FoodHub.Application.Common.Models;
using FoodHub.Application.DTOs.Options;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsByMenuItemHandler : IRequestHandler<GetOptionGroupsByMenuItemQuery, Result<List<OptionGroupDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOptionGroupsByMenuItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<OptionGroupDto>>> Handle(GetOptionGroupsByMenuItemQuery request, CancellationToken cancellationToken)
        {
            var groups = await _unitOfWork.Repository<OptionGroup>()
                .Query()
                .Where(og => og.MenuItemId == request.MenuItemId)
                .Include(og => og.OptionItems)
                .ToListAsync(cancellationToken);

            var dtos = groups.Select(og => new OptionGroupDto
            {
                OptionGroupId = og.OptionGroupId,
                Name = og.Name,
                Type = (int)og.Type,
                IsRequired = og.IsRequired,
                OptionItems = og.OptionItems.Select(oi => new OptionItemDto
                {
                    OptionItemId = oi.OptionItemId,
                    Label = oi.Label,
                    ExtraPrice = oi.ExtraPrice
                }).ToList()
            }).ToList();

            return Result<List<OptionGroupDto>>.Success(dtos);
        }
    }
}
