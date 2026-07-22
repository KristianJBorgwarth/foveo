using Foveo.Domain.Aggregates;
using Foveo.Domain.Common;
using Foveo.Application.Models;

namespace Foveo.Application.Contracts;

/// <summary>
/// Pure transform: given an uploaded original, produce a thumbnail and a browser-friendly
/// display copy and store them, returning their keys. Orchestration (loading the aggregate,
/// marking it ready, persisting) lives in the background worker, not here.
/// </summary>
public interface IMediaProcessor
{
    Task<Result<MediaDerivatives>> ProcessAsync(Media media, CancellationToken ct = default);
}
