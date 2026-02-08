using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.MenuItems.Commands.DeleteMenuItem
{
    public class DeleteMenuItemHandler : IRequestHandler<DeleteMenuItemCommand, Result<DeleteMenuItemResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        public DeleteMenuItemHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<DeleteMenuItemResponse>> Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<MenuItem>();

            var menuItem = await repo.Query()
                .Include(mi => mi.Category)
                .FirstOrDefaultAsync(mi => mi.MenuItemId == request.MenuItemId, cancellationToken);
            if (menuItem is null) return Result<DeleteMenuItemResponse>.NotFound("Menu item is not found!");

            menuItem.DeletedAt = DateTime.UtcNow;
            menuItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync();

            var response = _mapper.Map<DeleteMenuItemResponse>(menuItem);
            return Result<DeleteMenuItemResponse>.Success(response);
        }
    }
}
