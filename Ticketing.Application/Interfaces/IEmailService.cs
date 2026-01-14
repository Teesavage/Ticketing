namespace Ticketing.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmail(string email, string firstName);
    }
}