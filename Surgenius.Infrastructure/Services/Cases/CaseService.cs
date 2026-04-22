using Microsoft.EntityFrameworkCore;
using Surgenius.Application.Models.Responses;
using Surgenius.Application.DTOs.Cases;
using Surgenius.Application.Interfaces.Cases;
using Surgenius.Infrastructure.Data.Context;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Services.Cases;

public class CaseService : ICaseService
{
    private readonly AppDbContext _context;

    public CaseService(AppDbContext context)
    {
        _context = context;
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

    public async Task<ApiResponse<IEnumerable<CaseDto>>> GetUserCasesAsync(Guid userId, bool isAdmin)
    {
        // Admin sees ALL cases in the system; others see only their own
        var query = isAdmin
            ? _context.Cases.AsQueryable()
            : _context.Cases.Where(c => c.UserId == userId);

        var cases = await query
            .Select(c => new CaseDto
            {
                Id = c.Id,
                CaseType = c.CaseType,
                CreationDate = c.CreationDate,
                PatientName = c.PatientName,
                PatientAge = c.PatientAge,
                PatientGender = c.PatientGender,
                PatientPhone = c.PatientPhone,
                Description = c.Description
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<CaseDto>>.Success(cases);
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
            // ApplicationUser.DoctorId holds the ID of the student's supervising Doctor.
            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null)
                return ApiResponse<CaseDetailDto>.Failure("Student account not found.");

            if (student.DoctorId == null || student.DoctorId != @case.UserId)
                return ApiResponse<CaseDetailDto>.Failure(
                    "Access denied. You are not linked to the Doctor who owns this case.");
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
}
