namespace Quizapp.ClassModels
{
    public class Email
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string CC { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

}