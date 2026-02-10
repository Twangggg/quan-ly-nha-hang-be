using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Queries.GetCurrentUser
{
    public class GetCurrentUserQuery : IRequest<Result<CurrentUserResponse>>
    {
    }
}
