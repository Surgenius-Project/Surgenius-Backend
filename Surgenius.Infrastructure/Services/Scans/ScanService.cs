using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Surgenius.Application.DTOs.Scans;
using Surgenius.Application.Interfaces.Scans;
using Surgenius.Application.Interfaces.Storage;
using Surgenius.Application.Models.Responses;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;

namespace Surgenius.Infrastructure.Services.Scans;

public class ScanService : IScanService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public ScanService(AppDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPLOAD  (Doctor only)
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<ApiResponse<ScanReadDto>> UploadScanAsync(Guid doctorId, UploadScanDto dto)
    {
        // 1. Validate the file stream and name
        if (dto.FileStream == null || dto.FileStream.Length == 0)
            return ApiResponse<ScanReadDto>.Failure("No file was provided or the file is empty.");

        if (string.IsNullOrWhiteSpace(dto.FileName))
            return ApiResponse<ScanReadDto>.Failure("File name is required.");

        // 2. Validate file size (Limit to 20MB)
        const long maxFileSize = 20 * 1024 * 1024; // 20 MB
        if (dto.FileStream.Length > maxFileSize)
            return ApiResponse<ScanReadDto>.Failure("File size exceeds the 20MB limit.");

        // 3. Validate file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".dcm", ".nii" };
        var fileExtension = Path.GetExtension(dto.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
            return ApiResponse<ScanReadDto>.Failure("Invalid file type. Only .jpg, .jpeg, .png, .dcm, and .nii are allowed.");

        // 4. Verify the case exists
        var @case = await _context.Cases
            .FirstOrDefaultAsync(c => c.Id == dto.CaseId);

        if (@case == null)
            return ApiResponse<ScanReadDto>.Failure("Case not found.");

        // 5. Verify the calling Doctor owns this case
        //    (Case.UserId stores the ID of the Doctor who created it)
        if (@case.UserId != doctorId)
            return ApiResponse<ScanReadDto>.Failure("Access denied. You do not own this case.");

        // 6. Persist the file to local storage
        var relativePath = await _fileStorage.SaveScanAsync(dto.FileStream, dto.FileName);

        // 7. Create and save the Scan record
        var scan = new Scan
        {
            Id = Guid.NewGuid(),
            CaseId = dto.CaseId,
            ScanPath = relativePath,
            ScanType = string.IsNullOrWhiteSpace(dto.ScanType) ? "General" : dto.ScanType,
            UploadDate = DateTime.UtcNow
        };

        _context.Scans.Add(scan);
        await _context.SaveChangesAsync();

        return ApiResponse<ScanReadDto>.Success(MapToDto(scan), "Scan uploaded successfully.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET BY CASE  (Doctor who owns the case  OR  Student linked to that Doctor)
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<ApiResponse<IEnumerable<ScanReadDto>>> GetScansByCaseAsync(
        Guid userId, bool isDoctor, Guid caseId)
    {
        // Load the case so we can check ownership
        var @case = await _context.Cases
            .FirstOrDefaultAsync(c => c.Id == caseId);

        if (@case == null)
            return ApiResponse<IEnumerable<ScanReadDto>>.Failure("Case not found.");

        if (isDoctor)
        {
            // Doctor must own the case
            if (@case.UserId != userId)
                return ApiResponse<IEnumerable<ScanReadDto>>.Failure(
                    "Access denied. You do not own this case.");
        }
        else
        {
            // Student must be linked to the Doctor who owns the case.
            // ApplicationUser.DoctorId stores the ID of the student's linked Doctor.
            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (student == null)
                return ApiResponse<IEnumerable<ScanReadDto>>.Failure("Student account not found.");

            if (student.DoctorId == null || student.DoctorId != @case.UserId)
                return ApiResponse<IEnumerable<ScanReadDto>>.Failure(
                    "Access denied. You are not linked to the Doctor who owns this case.");
        }

        // Fetch scans
        var scans = await _context.Scans
            .Where(s => s.CaseId == caseId)
            .Select(s => MapToDto(s))
            .ToListAsync();

        return ApiResponse<IEnumerable<ScanReadDto>>.Success(scans);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────────────────────────────────
    private static ScanReadDto MapToDto(Scan scan) => new()
    {
        Id = scan.Id,
        ScanPath = scan.ScanPath,
        UploadDate = scan.UploadDate,
        ScanType = scan.ScanType
    };
}
