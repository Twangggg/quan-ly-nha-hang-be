using AutoMapper;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Interfaces.Common;
using FoodHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodHub.Application.Features.PaymentMethods.Queries.GetPaymentMethods
{
    public class GetPaymentMethodsHandler
        : IRequestHandler<GetPaymentMethodsQuery, Result<List<GetPaymentMethodsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetPaymentMethodsHandler> _logger;

        public GetPaymentMethodsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetPaymentMethodsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<List<GetPaymentMethodsResponse>>> Handle(
            GetPaymentMethodsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting payment methods. ActiveOnly={ActiveOnly}", request.ActiveOnly);

            var query = _unitOfWork.Repository<PaymentMethodConfig>().Query();

            if (request.ActiveOnly == true)
            {
                query = query.Where(p => p.IsActive);
            }

            var items = await query
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);

            var response = _mapper.Map<List<GetPaymentMethodsResponse>>(items);
            return Result<List<GetPaymentMethodsResponse>>.Success(response);
        }
    }
}
