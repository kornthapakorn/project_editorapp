using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;

namespace EditProfileApp.Services
{
    public class EmailService
    {
        private readonly string _senderEmail;
        private readonly string _appPassword;

        public EmailService(IConfiguration configuration)
        {
            _senderEmail = configuration["EmailSettings:SenderEmail"];
            _appPassword = configuration["EmailSettings:AppPassword"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            MimeMessage email = new MimeMessage();
            email.From.Add(new MailboxAddress("CE Profile System", _senderEmail));
            email.To.Add(new MailboxAddress("Student", toEmail));
            email.Subject = subject;

            BodyBuilder builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using (SmtpClient smtp = new SmtpClient())
            {
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_senderEmail, _appPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
        }
    }
}