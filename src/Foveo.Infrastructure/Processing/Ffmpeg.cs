using System.Diagnostics;

namespace Foveo.Infrastructure.Processing;

/// <summary>Thin wrapper over the ffmpeg binary. Requires ffmpeg (with HEIC decode) on PATH.</summary>
internal static class Ffmpeg
{
    public static async Task RunAsync(string arguments, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg exited {process.ExitCode}: {stderr}");
    }
}
