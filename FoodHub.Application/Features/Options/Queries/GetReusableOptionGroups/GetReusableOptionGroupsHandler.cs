using FoodHub.Application.Common.Models;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Features.Options.Queries.GetOptionGroupsByMenuItem;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Options.Queries.GetReusableOptionGroups
{
    public class GetReusableOptionGroupsHandler
        : IRequestHandler<GetReusableOptionGroupsQuery, Result<PagedResult<OptionGroupResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReusableOptionGroupsHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResult<OptionGroupResponse>>> Handle(
            GetReusableOptionGroupsQuery request,
            CancellationToken cancellationToken
        )
        {
            var pagedGroups = await _unitOfWork
                .Repository<OptionGroup>()
                .Query()
                .AsNoTracking()
                .Where(og => og.MenuItemId == null) // REUSABLE
                .OrderBy(og => og.Name)
                .Select(og => new OptionGroupResponse
                {
                    OptionGroupId = og.OptionGroupId,
                    Name = og.Name,
                    Type = (int)og.OptionType,
                    IsRequired = og.IsRequired,
                    OptionItems = og
                        .OptionItems.OrderBy(oi => oi.Label)
                        .Select(oi => new OptionItemResponse
                        {
                            OptionItemId = oi.OptionItemId,
                            OptionGroupId = oi.OptionGroupId,
                            Label = oi.Label,
                            ExtraPrice = oi.ExtraPrice,
                        })
                        .ToList(),
                })
                .ToPagedResultAsync(request.PageNumber, request.PageSize);

            return Result<PagedResult<OptionGroupResponse>>.Success(pagedGroups);
        }
    }
}
