using System.ComponentModel.DataAnnotations;

namespace Identity.Api.Api;

public sealed class RequestValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        if (Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            return await next(context);
        }

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, member) => new { Member = member, Message = result.ErrorMessage ?? "Invalid value." })
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).Distinct().ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}