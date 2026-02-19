using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly AppDbContext _context;

    public ApplicationRepository(AppDbContext context) => _context = context;

    public async Task<DriverApplication?> GetByIdAsync(Guid id)
        => await _context.Applications.FindAsync(id);

    public async Task<DriverApplication?> GetByIdWithDetailsAsync(Guid id)
        => await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Inspector)
            .Include(a => a.LicenceCard)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<(List<DriverApplication> Items, int TotalCount)> GetByApplicantIdAsync(
        Guid applicantId, int page, int pageSize)
    {
        var query = _context.Applications
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<DriverApplication> Items, int TotalCount)> GetPendingForReviewAsync(
        int page, int pageSize)
    {
        var query = _context.Applications
            .Where(a => a.Status == ApplicationStatus.ExternalChecksPassed)
            .OrderBy(a => a.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> HasActiveApplicationAsync(Guid applicantId, LicenceCategory category)
    {
        var activeStatuses = new[]
        {
            ApplicationStatus.Pending,
            ApplicationStatus.ExternalChecksInProgress,
            ApplicationStatus.ExternalChecksPassed,
            ApplicationStatus.AssignedToInspector,
            ApplicationStatus.Approved
        };

        return await _context.Applications.AnyAsync(a =>
            a.ApplicantId == applicantId &&
            a.Category == category &&
            activeStatuses.Contains(a.Status));
    }

    public async Task AddAsync(DriverApplication application)
        => await _context.Applications.AddAsync(application);

    public void Update(DriverApplication application)
        => _context.Applications.Update(application);
}
