using Microsoft.EntityFrameworkCore;
using Surgenius.Application.DTOs.Analysis;
using Surgenius.Application.Interfaces.Analysis;
using Surgenius.Application.Models.Responses;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;

namespace Surgenius.Infrastructure.Services.Analysis;

public class MockAnalysisService : IAnalysisService
{
    private readonly AppDbContext _context;

    public MockAnalysisService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisResult> ProcessScanAsync(Guid scanId, Guid userId, bool isAdmin)
    {
        // Verify the scan exists
        var scan = await _context.Scans.Include(s => s.Case).FirstOrDefaultAsync(s => s.Id == scanId);
        if (scan == null)
            throw new Exception("Scan not found.");

        if (!isAdmin && scan.Case.UserId != userId)
            throw new Exception("Unauthorized to analyze this scan.");

        // Simulate AI processing delay
        await Task.Delay(2000);

        // Generate mock result
        var random = new Random();
        int stageNumeric = random.Next(0, 4); // 0 to 3
        string stageLabel = stageNumeric switch
        {
            0 => "Stage I",
            1 => "Stage II",
            2 => "Stage III",
            3 => "Stage IV",
            _ => "Unknown"
        };
        
        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            ScanId = scanId,
            StageNumeric = stageNumeric,
            StageLabel = stageLabel,
            Confidence = Math.Round(random.NextDouble() * 0.5 + 0.5, 2), // 0.5 to 1.0
            TumorAreaPixels = random.Next(1000, 50000),
            
            // Mock paths simulating what AI/Storage would output
            MaskPath = $"/uploads/analysis/mock_mask_{scanId}.png",
            HighlightedPath = $"/uploads/analysis/mock_highlighted_{scanId}.png",
            Model3DPath = "/uploads/models/liver_placeholder.obj" // Use placeholder for 3D model
        };

        // Check if a result already exists to prevent duplicates (optional, based on logic)
        var existing = await _context.AnalysisResults.FirstOrDefaultAsync(a => a.ScanId == scanId);
        if (existing != null)
        {
            _context.AnalysisResults.Remove(existing);
        }

        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<ApiResponse<AnalysisReadDto>> GetAnalysisByScanAsync(Guid userId, bool isDoctor, bool isAdmin, Guid scanId)
    {
        // Load analysis result including the scan and its case so we can check ownership/linking
        var analysis = await _context.AnalysisResults
            .AsNoTracking()
            .Include(a => a.Scan)
                .ThenInclude(s => s.Case)
            .FirstOrDefaultAsync(a => a.ScanId == scanId);

        if (analysis == null)
            return ApiResponse<AnalysisReadDto>.Failure("Analysis result not found.");

        var @case = analysis.Scan?.Case;
        if (@case == null)
            return ApiResponse<AnalysisReadDto>.Failure("Associated case not found.");

        if (isAdmin)
        {
            // Admin has access to all analysis results
        }
        else if (isDoctor)
        {
            // Doctor must own the case
            if (@case.UserId != userId)
                return ApiResponse<AnalysisReadDto>.Failure("Access denied. You do not own this case.");
        }
        else
        {
            // Student must be linked to the Doctor who owns the case.
            var student = await _context.Users
                .AsNoTracking()
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null)
                return ApiResponse<AnalysisReadDto>.Failure("Student account not found.");

            if (student.DoctorId == null || student.DoctorId != @case.UserId)
                return ApiResponse<AnalysisReadDto>.Failure(
                    "Access denied. You are not linked to the Doctor who owns this case.");

            if (student.Doctor != null && !student.Doctor.IsInviteCodeActive)
                return ApiResponse<AnalysisReadDto>.Failure("Access denied by Doctor.");
        }

        var dto = MapToDto(analysis);
        return ApiResponse<AnalysisReadDto>.Success(dto);
    }

    private static AnalysisReadDto MapToDto(AnalysisResult a) => new()
    {
        Id = a.Id,
        ScanId = a.ScanId,
        StageNumeric = a.StageNumeric,
        StageLabel = a.StageLabel,
        Confidence = a.Confidence,
        TumorAreaPixels = a.TumorAreaPixels,
        MaskPath = a.MaskPath,
        HighlightedPath = a.HighlightedPath,
        Model3DPath = a.Model3DPath
    };
}
