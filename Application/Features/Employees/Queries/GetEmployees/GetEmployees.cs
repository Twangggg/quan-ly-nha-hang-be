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
            //public string Username { get; set; } = null!;
            public string FullName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Phone { get; set; } = null!;
            public string? Address { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public string Role { get; set; } = null!;
            public string Status { get; set; } = null!;
            public void Mapping(Profile profile)
            {
                profile.CreateMap<Employee, Response>()
                    .ForMember(d => d.Role,
                        opt => opt.MapFrom(s => s.Role.ToString()))
                    .ForMember(d => d.Status,
                        opt => opt.MapFrom(s => s.Status.ToString()));
            }
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

                // 1. Apply Global Search
                var searchableFields = new List<Expression<Func<Employee, string?>>>
                {
                    u => u.FullName,
                    u => u.EmployeeCode,
                    u => u.Phone,
                    u => u.Email
                };
                query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);

                // 2. Apply Filters
                var filterMapping = new Dictionary<string, Expression<Func<Employee, object>>>
                {
                    { "status", u => u.Status },
                    { "role", u => u.Role }
                };
                query = query.ApplyFilters(request.Pagination.Filters, filterMapping);

                // 3. Apply Multi-Sorting
                var sortMapping = new Dictionary<string, Expression<Func<Employee, object>>>
                {
                    {"username" , u => u.Username},
                    {"phone", u=> u.Phone },
                    {"email", u => u.Email},
                    {"fullname", u => u.FullName},
                    {"employeeCode", u => u.EmployeeCode },
                    {"createdAt", u => u.CreatedAt }
                };

                query = query.ApplySorting(
                    request.Pagination.OrderBy,
                    sortMapping,
                    u => u.EmployeeId);

                return await query
                    .ProjectTo<Response>(_mapper.ConfigurationProvider)
                    .ToPagedResultAsync(request.Pagination);
            }
        }
    }
}
