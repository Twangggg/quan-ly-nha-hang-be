using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem
{
    public class GetOptionGroupsByMenuItemHandler
        : IRequestHandler<GetOptionGroupsByMenuItemQuery, Result<List<OptionGroupResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetOptionGroupsByMenuItemHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<OptionGroupResponse>>> Handle(
            GetOptionGroupsByMenuItemQuery request,
            CancellationToken cancellationToken
        )
        {
            var groups = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .Where(og => og.MenuItemId == request.MenuItemId)
                .Include(og => og.OptionItems)
                .ToListAsync(cancellationToken);

            var responses = groups
                .Select(og => new OptionGroupResponse
                {
                    OptionGroupId = og.OptionGroupId,
                    MenuItemId = og.MenuItemId,
                    Name = og.Name,
                    Type = (int)og.OptionType,
                    IsRequired = og.IsRequired,
                    OptionItems = og
                        .OptionItems.Select(oi => new OptionItemResponse
                        {
                            OptionItemId = oi.OptionItemId,
                            OptionGroupId = oi.OptionGroupId,
                            Label = oi.Label,
                            ExtraPrice = oi.ExtraPrice,
                        })
                        .ToList(),
                })
                .ToList();

            return Result<List<OptionGroupResponse>>.Success(responses);
        }
    }
}
