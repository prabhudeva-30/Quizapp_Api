using Quizapp.ClassModels;

namespace Quizapp.Services.Interface
{
    public interface ITestEmailService
    {
        Task<string> SendEmail(Email MailData);
    }
}
