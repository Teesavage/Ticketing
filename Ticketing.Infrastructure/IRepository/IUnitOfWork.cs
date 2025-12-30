using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.IRespository
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<UserRole> UserRoles { get; }
        IGenericRepository<Event> Events { get; }
        IGenericRepository<Ticket> Tickets { get; }
        IGenericRepository<TicketType> TicketTypes { get; }
        IGenericRepository<Country> Countries { get; }
        IGenericRepository<State> States { get; }

        Task Save();
        void ClearChangeTracker();
    }
}