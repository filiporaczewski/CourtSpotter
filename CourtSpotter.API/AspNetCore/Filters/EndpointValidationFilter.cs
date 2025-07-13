using FluentValidation;

namespace CourtSpotter.Filters;

public class EndpointValidationFilter<T>: IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public EndpointValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        
        if (argument is null)
        {
            return Results.Problem(
                title: "Invalid Request",
                detail: "Request parameters are required",
                statusCode: StatusCodes.Status400BadRequest
            );

        }

        var validationResult = await _validator.ValidateAsync(argument);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }
        
        return await next(context);
    }
}