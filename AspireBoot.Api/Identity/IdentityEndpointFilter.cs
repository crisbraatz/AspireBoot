using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AspireBoot.Api.Primitives;

namespace AspireBoot.Api.Identity;

public class IdentityEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (object? argument in context.Arguments)
        {
            if (argument is null)
                continue;

            if (argument is not ListRequest &&
                argument.GetType().Namespace?.StartsWith("AspireBoot.Api.Identity", StringComparison.Ordinal)
                    is not true)
                continue;

            List<ValidationResult> validationResults = [];

            if (!Validator.TryValidateObject(argument, new ValidationContext(argument), validationResults, true))
                return Results.ValidationProblem(validationResults
                    .SelectMany(validationResult => validationResult.MemberNames.DefaultIfEmpty(string.Empty)
                        .Select(field =>
                            (Field: JsonNamingPolicy.CamelCase.ConvertName(field),
                                Error: validationResult.ErrorMessage!)))
                    .GroupBy(error => error.Field, StringComparer.Ordinal)
                    .ToDictionary(
                        key => key.Key,
                        value => value.Select(error => error.Error).ToArray(),
                        StringComparer.Ordinal));
        }

        return await next(context).ConfigureAwait(false);
    }
}
