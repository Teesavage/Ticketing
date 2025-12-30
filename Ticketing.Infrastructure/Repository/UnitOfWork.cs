using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IGenericRepository<User> _users;
        private IGenericRepository<Role> _roles;
        private IGenericRepository<UserRole> _userRoles;
        private IGenericRepository<Event> _events;
        private IGenericRepository<Ticket> _tickets;
        private IGenericRepository<TicketType> _ticketTypes;
        private IGenericRepository<Country> _countries;
        private IGenericRepository<State> _states;

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public UnitOfWork(ApplicationDbContext context){
            _context = context;
        }
        public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);
        public IGenericRepository<Role> Roles => _roles ??= new GenericRepository<Role>(_context);
        public IGenericRepository<UserRole> UserRoles => _userRoles ??= new GenericRepository<UserRole>(_context);
        public IGenericRepository<Event> Events => _events ??= new GenericRepository<Event>(_context);
        public IGenericRepository<Ticket> Tickets => _tickets ??= new GenericRepository<Ticket>(_context);
        public IGenericRepository<TicketType> TicketTypes => _ticketTypes ??= new GenericRepository<TicketType>(_context);
        public IGenericRepository<Country> Countries => _countries ??= new GenericRepository<Country>(_context);
        public IGenericRepository<State> States => _states ??= new GenericRepository<State>(_context);



        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task Save()
        {
            await _context.SaveChangesAsync();
        }

        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }
    }
}