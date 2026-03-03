using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIinAsync(string iin);
    Task AddAsync(User user);
    void Update(User user);
}
