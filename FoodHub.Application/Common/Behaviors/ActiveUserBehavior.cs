using FoodHub.Application.Common.Models;
using FoodHub.Application.Constants;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;

namespace FoodHub.Application.Common.Behaviors
{
    public class ActiveUserBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>, IMustBeActive // Chỉ áp dụng cho request có marker
        where TResponse : class
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public ActiveUserBehavior(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // 1. Lấy UserId từ Token
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return CreateFailureResponse(_messageService.GetMessage(MessageKeys.ActiveUserBehavior.Unauthorized));
            }

            // 2. Kiểm tra DB xem User có đang Active không
            // Dùng GetByIdAsync để tận dụng cache hoặc repo có sẵn
            var user = await _unitOfWork.Repository<Employee>().GetByIdAsync(userId);

            if (user == null || user.Status == EmployeeStatus.Inactive)
            {
                return CreateFailureResponse(_messageService.GetMessage(MessageKeys.ActiveUserBehavior.InActiveAccount) );
            }

            // 3. Nếu mọi thứ OK, cho phép đi tiếp vào Handler
            return await next();
        }

        // Helper để tạo Result<T>.Failure vì TResponse là generic
        private TResponse CreateFailureResponse(string message)
        {
            // Vì TResponse thường là Result<T>, ta dùng Reflection để gọi phương thức Failure của nó
            var resultType = typeof(TResponse);
            var failureMethod = resultType.GetMethod("Failure", new[] { typeof(string), typeof(ResultErrorType) });

            if (failureMethod != null)
            {
                return (failureMethod.Invoke(null, new object[] { message, ResultErrorType.Unauthorized }) as TResponse)!;
            }

            return default!;
        }
    }
}
