using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.DTOs.Cases;
using Surgenius.Application.Interfaces.Cases;
using Surgenius.Infrastructure.Data.Context;
using Surgenius.Domain.Models;
using Surgenius.Application.Interfaces.Storage;

namespace Surgenius.Infrastructure.Services.Cases;

public class CaseService : ICaseService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _storageService;

    public CaseService(AppDbContext context, IFileStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<ApiResponse<CaseDto>> CreateCaseAsync(Guid userId, CreateCaseDto request)
    {
        var @case = new Case
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CaseType = request.CaseType,
            CreationDate = DateTime.UtcNow,
            PatientName = request.PatientName,
            PatientAge = request.PatientAge,
            PatientGender = request.PatientGender,
            PatientPhone = request.PatientPhone,
            Description = request.Description
        };

        _context.Cases.Add(@case);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return ApiResponse<CaseDto>.Failure($"Database error: {ex.Message}. Inner Exception: {ex.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<CaseDto>.Failure($"An error occurred: {ex.Message}");
        }

        return ApiResponse<CaseDto>.Success(new CaseDto
        {
            Id = @case.Id,
            CaseType = @case.CaseType,
            CreationDate = @case.CreationDate,
            PatientName = @case.PatientName,
            PatientAge = @case.PatientAge,
            PatientGender = @case.PatientGender,
            PatientPhone = @case.PatientPhone,
            Description = @case.Description
        }, "Case created successfully.");
    }

    public async Task<ApiResponse<IEnumerable<CaseDto>>> GetUserCasesAsync(Guid userId, bool isDoctor, bool isAdmin, string? searchTerm = null, string? stage = null)
    {
        IQueryable<Case> query;

        if (isAdmin)
        {
            // Admin sees ALL cases in the system
            query = _context.Cases.AsQueryable();
        }
        else if (isDoctor)
        {
            // Doctors see only their own cases
            query = _context.Cases.Where(c => c.UserId == userId);
        }
        else
        {
            // Student: find the Doctor linked to this student
            var student = await _context.Users
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null)
                return ApiResponse<IEnumerable<CaseDto>>.Failure("User not found.");

            if (student.DoctorId == null || student.Doctor == null)
                return ApiResponse<IEnumerable<CaseDto>>.Success(new List<CaseDto>(), "You are not linked to any Doctor.");

            // Check if Doctor's Invite Code is active
            if (!student.Doctor.IsInviteCodeActive)
                return ApiResponse<IEnumerable<CaseDto>>.Failure("Access denied by Doctor.");

            // Return that Doctor's cases
            query = _context.Cases.Where(c => c.UserId == student.DoctorId.Value);
        }

        // Apply Search Filter (Patient Name)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
           // query = query.Where(c => c.PatientName.Contains(searchTerm));

            query = query.Where(c => c.PatientName.ToLower().Contains(searchTerm.ToLower()));
        }

        // Apply Stage Filter (I, II, III)
        // A case is included if ANY of its scans have an analysis result matching the stage.
        if (!string.IsNullOrWhiteSpace(stage))
        {
            // Normalize stage input (e.g., "I" -> "Stage I")
            var stageLabel = stage.StartsWith("Stage", StringComparison.OrdinalIgnoreCase) ? stage : $"Stage {stage}";
            
            query = query.Where(c => c.Scans.Any(s => _context.AnalysisResults.Any(a => a.ScanId == s.Id && a.StageLabel == stageLabel)));
        }

        var cases = await query
            .OrderByDescending(c => c.CreationDate)
            .Select(c => new CaseDto
            {
                Id = c.Id,
                CaseType = c.CaseType,
                CreationDate = c.CreationDate,
                PatientName = c.PatientName,
                PatientAge = c.PatientAge,
                PatientGender = c.PatientGender,
                PatientPhone = c.PatientPhone,
                Description = c.Description,
                LatestStage = c.Scans
                    .SelectMany(s => _context.AnalysisResults.Where(a => a.ScanId == s.Id))
                    .Select(a => a.StageLabel)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<CaseDto>>.Success(cases);
    }

    public async Task<ApiResponse<bool>> ToggleStudentAccessAsync(Guid doctorId)
    {
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctorId);
        if (doctor == null)
            return ApiResponse<bool>.Failure("Doctor not found.");

        doctor.IsInviteCodeActive = !doctor.IsInviteCodeActive;
        
        await _context.SaveChangesAsync();
        
        return ApiResponse<bool>.Success(doctor.IsInviteCodeActive, $"Student access {(doctor.IsInviteCodeActive ? "enabled" : "disabled")} successfully.");
    }

    public async Task<ApiResponse<CaseDetailDto>> GetCaseByIdAsync(Guid userId, bool isDoctor, bool isAdmin, Guid caseId)
    {
        // Eager-load the associated scans so they're included in the response
        var @case = await _context.Cases
            .Include(c => c.Scans)
            .FirstOrDefaultAsync(c => c.Id == caseId);

        if (@case == null)
            return ApiResponse<CaseDetailDto>.Failure("Case not found.");

        if (isAdmin)
        {
            // Admin has unrestricted access to any case
        }
        else if (isDoctor)
        {
            // Doctors can only access their own cases
            if (@case.UserId != userId)
                return ApiResponse<CaseDetailDto>.Failure("Access denied. You do not own this case.");
        }
        else
        {
            // Students can access the case only if they are linked to the Doctor who owns it.
            var student = await _context.Users
                .AsNoTracking()
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null)
                return ApiResponse<CaseDetailDto>.Failure("Student account not found.");

            if (student.DoctorId == null || student.DoctorId != @case.UserId)
                return ApiResponse<CaseDetailDto>.Failure(
                    "Access denied. You are not linked to the Doctor who owns this case.");
            
            if (student.Doctor != null && !student.Doctor.IsInviteCodeActive)
                return ApiResponse<CaseDetailDto>.Failure("Access denied by Doctor.");
        }

        var dto = new CaseDetailDto
        {
            Id = @case.Id,
            CaseType = @case.CaseType,
            CreationDate = @case.CreationDate,
            PatientName = @case.PatientName,
            PatientAge = @case.PatientAge,
            PatientGender = @case.PatientGender,
            PatientPhone = @case.PatientPhone,
            Description = @case.Description,
            Scans = @case.Scans.Select(s => new ScanSummaryDto
            {
                Id = s.Id,
                ScanPath = s.ScanPath,
                ScanType = s.ScanType,
                UploadDate = s.UploadDate
            }).ToList()
        };

        return ApiResponse<CaseDetailDto>.Success(dto);
    }

    public async Task<ApiResponse<bool>> DeleteCaseAsync(Guid userId, Guid caseId)
    {
        // 1. Fetch the case including Scans
        var @case = await _context.Cases
            .Include(c => c.Scans)
            .FirstOrDefaultAsync(c => c.Id == caseId);

        if (@case == null)
            return ApiResponse<bool>.Failure("Case not found.");

        // 2. Ownership Check: Only the creator can delete
        if (@case.UserId != userId)
            return ApiResponse<bool>.Failure("Access denied. Only the owner of this case can delete it.");

        try
        {
            // 3. Collect paths for file system cleanup
            var filePathsToDelete = new List<string?>();
            
            foreach (var scan in @case.Scans)
            {
                filePathsToDelete.Add(scan.ScanPath);
                
                // Find associated AnalysisResult
                var analysis = await _context.AnalysisResults.FirstOrDefaultAsync(a => a.ScanId == scan.Id);
                if (analysis != null)
                {
                    filePathsToDelete.Add(analysis.MaskPath);
                    filePathsToDelete.Add(analysis.HighlightedPath);
                    // Model3DPath if dynamic...
                    
                    _context.AnalysisResults.Remove(analysis);
                }
                
                _context.Scans.Remove(scan);
            }

            // 4. Remove Case from DB
            _context.Cases.Remove(@case);
            
            // 5. Save Changes to DB
            await _context.SaveChangesAsync();

            // 6. Physical File Cleanup (Post-DB success)
            foreach (var path in filePathsToDelete)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    await _storageService.DeleteFileAsync(path);
                }
            }

            return ApiResponse<bool>.Success(true, "Case and all associated data deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Failure($"An error occurred during deletion: {ex.Message}");
        }
    }
}
