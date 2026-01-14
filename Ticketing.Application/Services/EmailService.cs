using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Ticketing.Application.Interfaces;

namespace Ticketing.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _emailFrom;
        private readonly string _emailPassword;
        private readonly string _templatePath;
        private readonly ILogger<EmailService> _logger;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.
        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            var gmailSetting = config.GetSection("GmailSettings");
            _smtpHost = gmailSetting["Host"];
            _smtpPort = int.Parse(gmailSetting["Port"]);
            _emailFrom = gmailSetting["Username"];
            _emailPassword = gmailSetting["AppPassword"];
            _templatePath = Path.GetFullPath(gmailSetting["TemplateFolder"]);
            _logger = logger;
        }

        private async Task<string> GetEmailTemplateAsync(string templateName)
        {
            string filePath = Path.Combine(_templatePath, $"{templateName}.Html");
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }

            Console.WriteLine("Email template not found at path:" + filePath, templateName);
            return string.Empty;
        }

        private string PopulateTemplate(string templateContent, Dictionary<string, string> replacements)
        {
            foreach (var replacement in replacements)
            {
                templateContent = templateContent.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);
            }
            return templateContent;
        }

        private async Task<bool> SendTemplatedEmailAsync(string toEmail, string subject, string templateName, Dictionary<string, string> replacements,
            string? fallbackTextBody = null,
            IFormFile? attachments = null)
        {
            if (string.IsNullOrEmpty(toEmail))
            {
                Console.WriteLine("Recipient email is null or empty.");
                return false;
            }

            string template = await GetEmailTemplateAsync(templateName);
            if (string.IsNullOrEmpty(template))
            {
                Console.WriteLine($"Email template '{templateName}' not found.");
                return false;
            }

            string 
            emailBody = PopulateTemplate(template, replacements);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Teekets", _emailFrom));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = emailBody,
                TextBody = fallbackTextBody
            };

            // ✅ Add attachments if provided
            if (attachments != null)
            {

                if (attachments.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await attachments.CopyToAsync(ms);
                    ms.Position = 0;
                    bodyBuilder.Attachments.Add(attachments.FileName, ms.ToArray(), ContentType.Parse(attachments.ContentType));
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls); // or None, if TLS not used
                await client.AuthenticateAsync(_emailFrom, _emailPassword); // comment if authentication is  not required (SecureSocketOptions.None)
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {toEmail}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send email to {email}: {error}", toEmail, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmail(string email, string firstName)
        {
            var replacements = new Dictionary<string, string>
            {
                { "UserEmail", email },
                { "UserName", firstName }
            };

            return await SendTemplatedEmailAsync(
                email,
                "Welcome To Teekets",
                "Welcome", //CHANGE TEMPLATE NAME OR CREATE TEMPLATE
                replacements
            );
        }
    }
}