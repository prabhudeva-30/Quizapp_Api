using Microsoft.Extensions.Configuration;
using Quizapp.ClassModels;
using Quizapp.Constants;
using Quizapp.Services.Interface;
using System.Net;
using System.Net.Mail;

namespace Quizapp.Services.Implementation
{
    public class EmailService : IEmailService
    {

        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;

        }

        public string SendEmail(Email MailData)
        {
            EmailSettings EmailSettings = new EmailSettings()
            {
                Host = _configuration.GetSection("MailSettings:Host").Value,
                Port = Convert.ToInt32(_configuration.GetSection("MailSettings:Port").Value),
                UserName = _configuration.GetSection("MailSettings:UserName").Value,
                Password = _configuration.GetSection("MailSettings:Password").Value
            };
            MailData.From = MailData?.From != "" ? MailData.From : _configuration.GetSection("MailSettings:Mail").Value;

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(MailData.From);
            msg.To.Add(MailData.To);
            msg.Subject = MailData.Subject;
            msg.Body = MailData.Body;
            msg.Priority = MailPriority.High;
            msg.IsBodyHtml = true;

            if (MailData.CC != "")
                msg.CC.Add(MailData.CC);


            using (SmtpClient client = new SmtpClient())
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(EmailSettings.UserName, EmailSettings.Password);
                client.Host = EmailSettings.Host;
                client.Port = EmailSettings.Port;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Send(msg);
            }

            return MessageConstants.SUCCESS;
        }


    }


}
