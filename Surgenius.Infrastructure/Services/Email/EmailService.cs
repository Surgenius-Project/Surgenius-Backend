using Surgenius.Application.Interfaces.Email;

namespace Surgenius.Infrastructure.Services.Email;

public class EmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        // Mock email sending - log to console
        Console.WriteLine("\n[EMAIL MOCK]");
        Console.WriteLine($"To: {to}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine($"Body: {body}");
        Console.WriteLine("[END EMAIL MOCK]\n");

        return Task.CompletedTask;
    }
}
