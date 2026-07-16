using Foveo.API.Common;

internal sealed class MediaEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder rtb)
    {
        var grp = rtb.MapGroup("/media")
            .WithTags("Media")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grp.MapPost("", async(CancellationToken ct) =>
        {
            return Results.Ok();
        });
    }
}
