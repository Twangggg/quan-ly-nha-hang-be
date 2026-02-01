using AutoMapper;
using FoodHub.Application.DTOs.Categories;
using FoodHub.Application.Interfaces;
using MediatR;

namespace FoodHub.Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreateCategoryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // TODO: Implement CreateCategory logic
            throw new NotImplementedException();
        }
    }
}
