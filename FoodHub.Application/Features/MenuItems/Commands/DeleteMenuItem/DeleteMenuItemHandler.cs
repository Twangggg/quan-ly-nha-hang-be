using AutoMapper;
using FoodHub.Application.Common.Constants;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
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
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public DeleteMenuItemHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<DeleteMenuItemResponse>> Handle(DeleteMenuItemCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.Repository<MenuItem>();

            var menuItem = await repo.Query()
                .Include(mi => mi.Category)
                .FirstOrDefaultAsync(mi => mi.MenuItemId == request.MenuItemId, cancellationToken);
            if (menuItem is null) return Result<DeleteMenuItemResponse>.NotFound(_messageService.GetMessage(MessageKeys.MenuItem.NotFound));

            menuItem.DeletedAt = DateTime.UtcNow;
            menuItem.UpdatedBy = Guid.TryParse(_currentUserService.UserId, out var userId) ? userId : null;

            await _unitOfWork.SaveChangeAsync();

            await _cacheService.RemoveByPatternAsync("menuitem:list", cancellationToken);
            await _cacheService.RemoveAsync(string.Format(CacheKey.MenuItemById, request.MenuItemId), cancellationToken);

            var response = _mapper.Map<DeleteMenuItemResponse>(menuItem);
            return Result<DeleteMenuItemResponse>.Success(response);
        }
    }
}
