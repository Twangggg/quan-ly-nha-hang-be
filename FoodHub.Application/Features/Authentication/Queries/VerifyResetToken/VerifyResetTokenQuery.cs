using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Authentication.Queries.VerifyResetToken
{
    public record VerifyResetTokenQuery(string Token) : IRequest<Result<bool>>;
}
