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
            CreationDate = DateTime.UtcNow
        };

        _context.Cases.Add(@case);
        await _context.SaveChangesAsync();

        return ApiResponse<CaseDto>.Success(new CaseDto
        {
            Id = @case.Id,
            CaseType = @case.CaseType,
            CreationDate = @case.CreationDate
        }, "Case created successfully.");
    }

    public async Task<ApiResponse<IEnumerable<CaseDto>>> GetUserCasesAsync(Guid userId)
    {
        var cases = await _context.Cases
            .Where(c => c.UserId == userId)
            .Select(c => new CaseDto
            {
                Id = c.Id,
                CaseType = c.CaseType,
                CreationDate = c.CreationDate
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<CaseDto>>.Success(cases);
    }

    public async Task<ApiResponse<CaseDto>> GetCaseByIdAsync(Guid userId, Guid caseId)
    {
        var @case = await _context.Cases
            .FirstOrDefaultAsync(c => c.Id == caseId && c.UserId == userId);

        if (@case == null)
            return ApiResponse<CaseDto>.Failure("Case not found or you don't have access.");

        return ApiResponse<CaseDto>.Success(new CaseDto
        {
            Id = @case.Id,
            CaseType = @case.CaseType,
            CreationDate = @case.CreationDate
        });
    }
}
