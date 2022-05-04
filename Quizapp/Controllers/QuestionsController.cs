#nullable disable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.ClassModels;
using Quizapp.Constants;
using Quizapp.Constants.Constants;
using Quizapp.Models;
using Quizapp.Services;
using Quizapp.Services.Interface;

namespace Quizapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenservice;
        private readonly ITestEmailService _emailService;

        public QuestionsController(DataContext context, ITokenService tokenservice, ITestEmailService emailService)
        {
            _context = context;
            _tokenservice = tokenservice;
            _emailService = emailService;
        }


        [HttpGet(nameof(GetQuestions)), Authorize]
        public async Task<ActionResult<IEnumerable<QuestionsDTO>>> GetQuestions()
        {
            GeneralResponse response = new GeneralResponse();
            List<Question> questionsList = new List<Question>();
            List<QuestionsDTO> responseQuestions = new List<QuestionsDTO>();

            questionsList = await _context.Questions.ToListAsync();
            if (questionsList.Count > 0)
            {
                foreach (Question data in questionsList)
                {
                    List<OptionsDTO> options = new List<OptionsDTO>();

                    options.Add(new OptionsDTO { AnswerText = data.Option_A, IsCorrect = data.Answer.ToUpper() == "A" });
                    options.Add(new OptionsDTO { AnswerText = data.Option_B, IsCorrect = data.Answer.ToUpper() == "B" });

                    if (data.Option_C != null)
                        options.Add(new OptionsDTO { AnswerText = data.Option_C, IsCorrect = data.Answer.ToUpper() == "C" });
                    if (data.Option_D != null)
                        options.Add(new OptionsDTO { AnswerText = data.Option_D, IsCorrect = data.Answer.ToUpper() == "D" });

                    responseQuestions.Add(new QuestionsDTO
                    {
                        QuestionText = data.QuestionText,
                        QuestionId = data.QuestionId,
                        AnswerOptions = options
                    }
                    );
                }

                response.Data = responseQuestions;
            }
            else
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.DATA_NOT_AVAILABLE;
            }

            return Ok(response);
        }


        [HttpPost(nameof(SaveQuizAnswers)), Authorize(Roles = RolesConstants.CANDIDATE)]
        public ActionResult SaveQuizAnswers(QuizDetails quizDetails)
        {
            GeneralResponse response = new GeneralResponse();
            List<Question> questions = new List<Question>();
            int CorrectAnswersCount = 0;
            int CandidateId = _tokenservice.GetUserId();

            if (quizDetails == null || quizDetails.Answers.Count == 0)
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_INPUT_DATA;
                return Ok(response);
            }

            questions = _context.Questions.Where(qn => quizDetails.Answers.Select(a => a.QuestionId).Contains(qn.QuestionId)).ToList();

            if (questions.Count > 0)
            {
                for (int i = 0; i < questions.Count; i++)
                {
                    if (quizDetails.Answers.Any(a => a.QuestionId == questions[i].QuestionId && a.ChoosenOption == questions[i].Answer))
                    {
                        CorrectAnswersCount++;
                    }
                }
            }
            else
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.DATA_NOT_AVAILABLE;
                return Ok(response);
            }

            foreach (var data in quizDetails.Answers)
            {
                QuizAnswer tempQuizAnswer = new QuizAnswer { ChoosenAnswer = data.ChoosenOption, QuestionId = data.QuestionId, UserId = CandidateId };
                _context.QuizAnswers.Add(tempQuizAnswer);
            }
            _context.SaveChanges();

            _context.QuizResults.Add(new QuizResult { UserId = CandidateId, Score = CorrectAnswersCount });
            _context.SaveChanges();

            response.Message = MessageConstants.ANSWERS_SAVED_SUCCESSFULLY;
            return Ok(response);
        }


        [HttpPost(nameof(SaveQuizScore)), Authorize(Roles = RolesConstants.CANDIDATE)]
        public async Task<ActionResult> SaveQuizScore(int Score)
        {
            GeneralResponse response = new GeneralResponse();
            int CandidateId = _tokenservice.GetUserId();

            if (CandidateId == 0)
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_USER_ID;
                return Ok(response);
            }

            _context.QuizResults.Add(new QuizResult { UserId = CandidateId, Score = Score });
             _context.SaveChanges();

            await SendScoreToUser(Score);

            response.Message = MessageConstants.SCORE_SAVED_SUCCESSFULLY;
            return Ok(response);
        }
       

        [RequestFormLimits(ValueLengthLimit = Int32.MaxValue, MultipartBodyLengthLimit = Int32.MaxValue, MemoryBufferThreshold = Int32.MaxValue)]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [HttpPost(nameof(Upload))]
        public async Task<IActionResult> Upload()
        {
            GeneralResponse response = new GeneralResponse();

            if (Request.Form.Files[0] == null)
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.FILE_NOT_FOUND_IN_FORM;
                return Ok(response);
            }
            IFormFile FilePath = Request.Form.Files[0];
            string UploadedFileName = FilePath.FileName;
            string UploadedFileSavePath = Path.Combine(Environment.CurrentDirectory, "Docs", UploadedFileName);

            using (var filestream = new FileStream(UploadedFileSavePath, FileMode.Create, FileAccess.Write))
            {
                FilePath.CopyTo(filestream);
            }

            //var byteArray = await file.ReadAsByteArrayAsync();
            //var base64String = Convert.ToBase64String(byteArray);

            response.Message = MessageConstants.VIDEO_SAVED_SUCCESSFULLY;
            return Ok(response);
        }


        private async Task<string> SendScoreToUser(int score)
        {
            UserTokenDetails userTokenDetails = new UserTokenDetails();
            string response = string.Empty;

            userTokenDetails = _tokenservice.GetUserTokenDetails();

            Email emailData = new Email()
            {
                From = string.Empty,
                To = userTokenDetails.Email,
                CC = string.Empty,
                Subject = EmailConstants.SCORE_DETAILS,
                Body = $"<html><body><div style='border: 3px solid red'>" +
                $"<div style='background-color: #FFF; text-align:center;padding: 20px'><img src='https://relevantz.com/wp-content/uploads/2022/03/Relevantz_Logo-on-Whitefinal.svg' width='200'/></div>" +
                $"<div style='background-color: #24125f;padding: 20px;color: #FFF;'><p style=''>Hey {userTokenDetails.Name} , </p><div>You have scored {score} in the quizz test.</div></div>" +
                $"<div style='text-align:center;background-color: red; padding: 5px;color: #FFF;'> copyright @ relevantz.com</div>" +
                $"</div></body></html>"
            };

            response = await _emailService.SendEmail(emailData);
            return response;
        }
    }
}
