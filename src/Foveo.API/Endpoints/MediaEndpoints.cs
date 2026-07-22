using Foveo.API.Common;
using Foveo.API.Extensions;
using Foveo.Application.Models;
using Foveo.Application.Services;

namespace Foveo.API.Endpoints;

internal sealed class MediaEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder rtb)
    {
        var grp = rtb.MapGroup("/api/media")
            .WithTags("Media")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Step 1: create Pending rows and hand back presigned PUT URLs (bytes go browser→store directly).
        grp.MapPost("/upload-tickets", async (
            CreateUploadTicketsRequest request,
            MediaUploadService uploads,
            CancellationToken ct) =>
        {
            var items = (request.Files ?? [])
                .Select(f => new UploadRequestItem(f.FileName, f.ContentType, f.SizeBytes))
                .ToList();

            var result = await uploads.RequestUploadsAsync(items, request.UploaderName, ct);
            return result.ToOk();
        });

        // Step 2: the client confirms the bytes landed; mark uploaded and enqueue processing.
        grp.MapPost("/{id:guid}/complete", async (
            Guid id,
            MediaUploadService uploads,
            CancellationToken ct) =>
        {
            var result = await uploads.CompleteUploadAsync(id, ct);
            return result.ToNoContent();
        });
    }
}
