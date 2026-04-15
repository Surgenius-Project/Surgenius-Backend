using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Interfaces.Analysis;
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

    public async Task<AnalysisResult> ProcessScanAsync(Guid scanId, Guid userId)
    {
        // Verify the scan exists
        var scan = await _context.Scans.Include(s => s.Case).FirstOrDefaultAsync(s => s.Id == scanId);
        if (scan == null)
            throw new Exception("Scan not found.");

        if (scan.Case.UserId != userId)
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
            Model3DPath = $"/uploads/models/mock_3d_model_{scanId}.obj"
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
}
