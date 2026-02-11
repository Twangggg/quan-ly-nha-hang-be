using FluentValidation;
using MediatR;
namespace FoodHub.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // N?u có validators cho request này
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                // Ch?y t?t c? validators
                var validationResults = await Task.WhenAll(
                    _validators.Select(v =>
                        v.ValidateAsync(context, cancellationToken)));
                // L?y t?t c? l?i
                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();
                // N?u có l?i, throw exception
                if (failures.Count != 0)
                    throw new ValidationException(failures);
            }
            // N?u validation pass, ti?p t?c d?n Handler
            return await next();
        }
    }
}
