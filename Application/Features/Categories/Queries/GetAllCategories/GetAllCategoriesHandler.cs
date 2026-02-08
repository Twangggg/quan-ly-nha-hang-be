using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Common.Models;
using FoodHub.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<GetCategoriesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllCategoriesHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<List<GetCategoriesResponse>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _unitOfWork.Repository<Domain.Entities.Category>()
                .Query()
                .ProjectTo<GetCategoriesResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return Result<List<GetCategoriesResponse>>.Success(categories);
        }
    }
}
