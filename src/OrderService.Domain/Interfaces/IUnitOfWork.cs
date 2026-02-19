using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IUnitOfWork
{
    IApplicationRepository Applications { get; }
    IUserRepository Users { get; }
    Task AddLicenceCardAsync(LicenceCard card);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
