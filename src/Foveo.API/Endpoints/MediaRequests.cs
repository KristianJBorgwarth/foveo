namespace Foveo.API.Endpoints;

/// <summary>Body of the presign request: the files a guest intends to upload, plus their optional name.</summary>
public sealed record CreateUploadTicketsRequest(string? UploaderName, IReadOnlyList<UploadFileRequest> Files);

public sealed record UploadFileRequest(string FileName, string ContentType, long SizeBytes);
