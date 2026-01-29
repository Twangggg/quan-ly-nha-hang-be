using MediatR;

namespace FoodHub.Application.Features.Employees.Queries.GetEmployeeById
{
    public record GetEmployeeByIdQuery(Guid Id) : IRequest<GetEmployeeByIdResponse>;
}
