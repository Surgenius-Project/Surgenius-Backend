using Microsoft.AspNetCore.Hosting;
using Surgenius.Application.Interfaces.Storage;

namespace Surgenius.Infrastructure.Services.Storage;

/// <summary>
/// Saves and deletes files from wwwroot/uploads using a GUID-based unique filename.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private const string ScansFolder = "uploads/scans";
    private const string AnalysisFolder = "uploads/analysis";

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _webRootPath = env.WebRootPath;
        
        // Ensure directories exist
        Directory.CreateDirectory(Path.Combine(_webRootPath, ScansFolder));
        Directory.CreateDirectory(Path.Combine(_webRootPath, AnalysisFolder));
    }

    public async Task<string> SaveScanAsync(Stream fileStream, string fileName)
    {
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var relativePath = Path.Combine(ScansFolder, uniqueFileName).Replace("\\", "/");
        var absolutePath = Path.Combine(_webRootPath, relativePath);

        using var outputStream = new FileStream(absolutePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        return $"/{relativePath}";
    }

    public async Task<string> SaveAnalysisImageAsync(Stream fileStream, string fileName)
    {
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var relativePath = Path.Combine(AnalysisFolder, uniqueFileName).Replace("\\", "/");
        var absolutePath = Path.Combine(_webRootPath, relativePath);

        using var outputStream = new FileStream(absolutePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        return $"/{relativePath}";
    }

    public async Task<bool> DeleteFileAsync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return false;

        try
        {
            var pathToDelete = relativePath;

            // If an absolute URL was stored (e.g. local Monster Server URL returned
            // by the augmented-scan proxy), extract only the path portion.
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out var uri))
            {
                pathToDelete = uri.AbsolutePath;
            }

            var cleanPath = pathToDelete.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var absolutePath = Path.Combine(_webRootPath, cleanPath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                return await Task.FromResult(true);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
