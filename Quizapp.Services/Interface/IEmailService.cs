using Quizapp.ClassModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizapp.Services.Interface
{
    public interface IEmailService
    {
        string SendEmail(Email MailData);
    }
}
