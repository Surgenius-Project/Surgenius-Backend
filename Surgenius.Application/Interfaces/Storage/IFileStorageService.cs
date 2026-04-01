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
    /// <param name="fileStream">The stream containing the file data.</param>
    /// <param name="fileName">The original file name (used to extract the extension).</param>
    /// <returns>The relative path, e.g. /uploads/scans/abc123.jpg</returns>
    Task<string> SaveScanAsync(Stream fileStream, string fileName);
}
