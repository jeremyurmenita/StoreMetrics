using System.Net;
using System.Net.Mail;

namespace StoreMetrics.Services
{
    public class EmailSender
    {
        private readonly string _gmail;
        private readonly string _appPassword;

        public EmailSender(IConfiguration config)
        {
            _gmail = config["EmailSettings:Gmail"]!;
            _appPassword = config["EmailSettings:AppPassword"]!;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_gmail, _appPassword)
            };

            var mail = new MailMessage(_gmail, to, subject, body)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mail);
        }
    }
}
