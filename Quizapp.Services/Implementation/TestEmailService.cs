using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Quizapp.ClassModels;
using Quizapp.Constants;
using Quizapp.Services.Interface;

namespace Quizapp.Services.Implementation
{
    public class TestEmailService : ITestEmailService
    {
        private readonly IConfiguration _configuration;

        public TestEmailService(IConfiguration configuration)
        {
            _configuration = configuration;

        }

        public async Task<string> SendEmail(Email MailData)
        {
            EmailSettings EmailSettings = new EmailSettings()
            {
                Host = _configuration.GetSection("TestMailSettings:Host").Value,
                Port = Convert.ToInt32(_configuration.GetSection("TestMailSettings:Port").Value),
                UserName = _configuration.GetSection("TestMailSettings:UserName").Value,
                Password = _configuration.GetSection("TestMailSettings:Password").Value
            };
            MailData.From = MailData?.From != "" ? MailData.From : _configuration.GetSection("MailSettings:Mail").Value;

            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(EmailSettings.UserName);
            email.To.Add(MailboxAddress.Parse(MailData.To));
            email.Subject = MailData.Subject;
            var builder = new BodyBuilder();

            builder.HtmlBody = MailData.Body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            smtp.Connect(EmailSettings.Host, EmailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(EmailSettings.UserName, EmailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);

            return MessageConstants.SUCCESS;
        }
    }
}
