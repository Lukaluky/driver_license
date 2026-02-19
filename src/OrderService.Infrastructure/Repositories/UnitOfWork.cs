using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;

namespace OrderService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(
        AppDbContext context,
        IApplicationRepository applications,
        IUserRepository users)
    {
        _context = context;
        Applications = applications;
        Users = users;
    }

    public IApplicationRepository Applications { get; }
    public IUserRepository Users { get; }

    public async Task AddLicenceCardAsync(LicenceCard card)
        => await _context.LicenceCards.AddAsync(card);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
