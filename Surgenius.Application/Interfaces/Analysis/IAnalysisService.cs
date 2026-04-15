using Surgenius.Domain.Models;

namespace Surgenius.Application.Interfaces.Analysis;

public interface IAnalysisService
{
    Task<AnalysisResult> ProcessScanAsync(Guid scanId, Guid userId);
}
