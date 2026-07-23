using Foveo.API.Common;
using Foveo.API.Extensions;
using Foveo.Application.Services;

namespace Foveo.API.Endpoints;

internal sealed class MediaEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder rtb)
    {
        var grp = rtb.MapGroup("/api/media")
            .WithTags("Media")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Single streamed upload: the file is the request body; metadata rides in the query string.
        grp.MapPost("", async (
            HttpContext ctx,
            MediaUploadService uploads,
            string fileName,
            string? uploaderName) =>
        {
            var contentType = ctx.Request.ContentType ?? "application/octet-stream";
            var length = ctx.Request.ContentLength;
            if (length is null or <= 0)
                return Results.Problem("A non-empty file body with a Content-Length is required.",
                    statusCode: StatusCodes.Status400BadRequest);

            var result = await uploads.StoreUploadAsync(
                fileName, contentType, length.Value, uploaderName, ctx.Request.Body, ctx.RequestAborted);

            return result.ToOk();
        });
    }
}
