using Foveo.Domain.Common;

namespace Foveo.API.Extensions;

/// <summary>Maps the domain <see cref="Result"/> types onto minimal-API HTTP results.</summary>
public static class ResultExtensions
{
    public static IResult ToOk<T>(this Result<T> result)
        => result.Success ? Results.Ok(result.Value) : result.ToProblem();

    public static IResult ToNoContent(this Result result)
        => result.Success ? Results.NoContent() : result.ToProblem();

    public static IResult ToProblem(this Result result)
    {
        var error = result.Error ?? Error.Failure();
        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.BadRequest => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            });
    }
}
