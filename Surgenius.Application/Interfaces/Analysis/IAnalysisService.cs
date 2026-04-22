using System;
using System.Threading.Tasks;
using Surgenius.Application.DTOs.Analysis;
using Surgenius.Application.Models.Responses;
using Surgenius.Domain.Models;

namespace Surgenius.Application.Interfaces.Analysis;

public interface IAnalysisService
{
    Task<AnalysisResult> ProcessScanAsync(Guid scanId, Guid userId);

    /// <summary>
    /// Retrieve an existing analysis result for a given scan.
    /// Returns an ApiResponse wrapping an AnalysisReadDto.
    /// Access rules: Doctor must own the case; Student must be linked to the owning doctor.
    /// </summary>
    Task<ApiResponse<AnalysisReadDto>> GetAnalysisByScanAsync(Guid userId, bool isDoctor, Guid scanId);
}
