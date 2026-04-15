using Microsoft.AspNetCore.Hosting;
using Surgenius.Application.Interfaces.Storage;

namespace Surgenius.Infrastructure.Services.Storage;

/// <summary>
/// Saves files to wwwroot/uploads/scans using a GUID-based unique filename.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private const string RelativeFolder = "uploads/scans";
    private readonly string _absoluteFolder;

    private const string AnalysisRelativeFolder = "uploads/analysis";
    private readonly string _analysisAbsoluteFolder;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        // Resolves to <project>/wwwroot/uploads/scans
        _absoluteFolder = Path.Combine(env.WebRootPath, "uploads", "scans");
        Directory.CreateDirectory(_absoluteFolder);

        // Resolves to <project>/wwwroot/uploads/analysis
        _analysisAbsoluteFolder = Path.Combine(env.WebRootPath, "uploads", "analysis");
        Directory.CreateDirectory(_analysisAbsoluteFolder);
    }

    public async Task<string> SaveScanAsync(Stream fileStream, string fileName)
    {
        // Generate a unique filename to prevent collisions
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var absolutePath = Path.Combine(_absoluteFolder, uniqueFileName);

        using var outputStream = new FileStream(absolutePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        // Return the relative web-accessible path
        return $"/{RelativeFolder}/{uniqueFileName}";
    }

    public async Task<string> SaveAnalysisImageAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var absolutePath = Path.Combine(_analysisAbsoluteFolder, uniqueFileName);

        using var outputStream = new FileStream(absolutePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        return $"/{AnalysisRelativeFolder}/{uniqueFileName}";
    }
}
