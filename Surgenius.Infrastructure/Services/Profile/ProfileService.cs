using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Surgenius.Application.DTOs.Profile;
using Surgenius.Application.Interfaces.Profile;
using Surgenius.Application.Models.Responses;
using Surgenius.Domain.Models;
using Surgenius.Infrastructure.Data.Context;

namespace Surgenius.Infrastructure.Services.Profile;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public ProfileService(UserManager<ApplicationUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ApiResponse<ProfileReadDto>> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<ProfileReadDto>.Failure("User not found.");

        var dto = new ProfileReadDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber,
            Location = user.Location,
            UserType = user.UserType.ToString(),
            InviteCode = user.InviteCode,
            IsInviteCodeActive = user.IsInviteCodeActive
        };

        return ApiResponse<ProfileReadDto>.Success(dto);
    }

    public async Task<ApiResponse<ProfileReadDto>> UpdateProfileAsync(Guid userId, UpdateProfileDto request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<ProfileReadDto>.Failure("User not found.");

        user.FullName = request.FullName;
        user.PhoneNumber = request.Phone;
        user.Location = request.Location;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<ProfileReadDto>.Failure("Failed to update profile.", errors);
        }

        return await GetProfileAsync(userId);
    }

    public async Task<ApiResponse<IEnumerable<StudentDto>>> GetLinkedStudentsAsync(Guid doctorId)
    {
        var students = await _context.Users
            .Where(u => u.DoctorId == doctorId && u.UserType == Domain.Enums.UserType.Student)
            .Select(u => new StudentDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<StudentDto>>.Success(students);
    }

    public async Task<ApiResponse<bool>> RemoveStudentAsync(Guid doctorId, Guid studentId)
    {
        var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId && u.DoctorId == doctorId);
        if (student == null) return ApiResponse<bool>.Failure("Student not found or not linked to this doctor.");

        student.DoctorId = null;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Student unlinked successfully.");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return ApiResponse<bool>.Failure("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<bool>.Failure("Failed to change password.", errors);
        }

        return ApiResponse<bool>.Success(true, "Password changed successfully.");
    }

    public async Task<ApiResponse<string>> GetOrGenerateInviteCodeAsync(Guid doctorId)
    {
        var user = await _userManager.FindByIdAsync(doctorId.ToString());
        if (user == null) return ApiResponse<string>.Failure("Doctor not found.");

        // Return existing code if it exists
        if (!string.IsNullOrWhiteSpace(user.InviteCode))
        {
            return ApiResponse<string>.Success(user.InviteCode);
        }

        // Generate a new unique 6-character code
        string newCode;
        bool isUnique = false;
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        do
        {
            newCode = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            // Check if it's unique in the database
            isUnique = !await _context.Users.AnyAsync(u => u.InviteCode == newCode);
        } 
        while (!isUnique);

        // Save the new code
        user.InviteCode = newCode;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ApiResponse<string>.Failure("Failed to generate and save the invite code.");
        }

        return ApiResponse<string>.Success(newCode, "New invite code generated successfully.");
    }
}
