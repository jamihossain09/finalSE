using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using MailKit.Security;

namespace finalSE.Service.Application
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:Password"];

            // Always write to a mock file for easy local development testing
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "mock-emails");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            
            string filePath = Path.Combine(folder, "emails.txt");
            string logContent = $"[EMAIL LOG - {DateTime.Now}]\nTo: {toEmail}\nSubject: {subject}\nBody: {body}\n-------------------------------------------------\n\n";
            await System.IO.File.AppendAllTextAsync(filePath, logContent);

            // Check if default placeholder credentials are used. If so, only simulate.
            if (string.IsNullOrEmpty(senderEmail) || senderEmail == "yourgmail@gmail.com" || 
                string.IsNullOrEmpty(password) || password == "YOUR_APP_PASSWORD")
            {
                return;
            }

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]
            ));

            email.To.Add(MailboxAddress.Parse(toEmail));

            email.Subject = subject;

            email.Body = new TextPart("html")
            {
                Text = body
            };

            using var smtp = new SmtpClient();

            var host = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var portStr = _config["EmailSettings:Port"];
            int port = int.TryParse(portStr, out int p) ? p : 587;

            SecureSocketOptions socketOptions;
            if (port == 465)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (port == 587)
            {
                socketOptions = SecureSocketOptions.StartTls;
            }
            else
            {
                socketOptions = SecureSocketOptions.Auto;
            }

            await smtp.ConnectAsync(host, port, socketOptions);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}