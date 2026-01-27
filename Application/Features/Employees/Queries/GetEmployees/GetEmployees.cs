using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Application.Extensions.Pagination;
using FoodHub.Application.Extensions.Query;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployees
{
    public class GetEmployees
    {
        public record Query(PaginationParams Pagination) : IRequest<PagedResult<Response>>;
        public class Response : IMapFrom<Employee>
        {
            public Guid EmployeeId { get; set; }
            public string EmployeeCode { get; set; } = null!;
            public string Username { get; set; } = null!;
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Phone { get; set; } = null!;
            public string? Address { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public EmployeeRole Role { get; set; }
            public EmployeeStatus Status { get; set; }
        }

        public class Handler : IRequestHandler<Query, PagedResult<Response>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IMapper _mapper;

            public Handler(IUnitOfWork unitOfWork, IMapper mapper)
            {
                _unitOfWork = unitOfWork;
                _mapper = mapper;
            }

            public async Task<PagedResult<Response>> Handle(Query request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Employee>().Query();
                var sortMapping = new Dictionary<string, Expression<Func<Employee, object>>>
                {
                    {"username" , u => u.Username},
                    {"email", u => u.Email},
                    {"date", u => u.CreatedAt }
                };

                query = query.ApplySorting(
                    request.Pagination.SortBy,
                    request.Pagination.IsDescending,
                    sortMapping,
                    u => u.EmployeeId);

                return await query
                    .ProjectTo<Response>(_mapper.ConfigurationProvider)
                    .ToPagedResultAsync(request.Pagination);

            }
        }
    }
}
