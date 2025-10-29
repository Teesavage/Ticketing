using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.IRespository
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Role> Roles { get; }


        Task Save();
        void ClearChangeTracker();
    }
}