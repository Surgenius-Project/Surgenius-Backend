namespace Surgenius.Application.Interfaces.Storage;

/// <summary>
/// Abstraction for local file storage operations.
/// Uses Stream primitives so the Application layer stays framework-agnostic.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves the file stream to the scans directory and returns the relative web path.
    /// </summary>
    Task<string> SaveScanAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Saves AI-generated masks or analysis visuals to the analysis directory.
    /// </summary>
    Task<string> SaveAnalysisImageAsync(Stream fileStream, string fileName);
}
