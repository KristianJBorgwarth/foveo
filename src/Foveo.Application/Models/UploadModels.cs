namespace Foveo.Application.Models;

/// <summary>One file a guest wants to upload, as declared before any bytes move.</summary>
public sealed record UploadRequestItem(string FileName, string ContentType, long SizeBytes);

/// <summary>
/// What the client needs to upload one file: the created media id, the presigned PUT URL,
/// and the content type it must send so the presigned signature matches.
/// </summary>
public sealed record UploadTicket(Guid MediaId, string UploadUrl, string ContentType);
