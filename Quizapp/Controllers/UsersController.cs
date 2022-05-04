#nullable disable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.ClassModels;
using Quizapp.Common;
using Quizapp.Constants;
using Quizapp.Constants.Constants;
using Quizapp.Models;
using Quizapp.Services;
using Quizapp.Services.Interface;

namespace Quizapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenservice;
        private readonly ITestEmailService _emailService;

        public UsersController(DataContext context, ITokenService tokenservice, ITestEmailService emailService)
        {
            _context = context;
            _tokenservice = tokenservice;
            _emailService = emailService;
        }


        [HttpGet(nameof(GetAllCandidates)), Authorize(Roles = RolesConstants.ADMIN)]
        public async Task<ActionResult<IEnumerable<UserFullDetails>>> GetAllCandidates()
        {
            GeneralResponse response = new GeneralResponse();
            List<UserDetailsDTO> UsersList = new List<UserDetailsDTO>();

            UsersList = await _context.Users
                .Where(x => x.Role == RolesConstants.CANDIDATE)
                .Select(a => new UserDetailsDTO
                {
                    Name = a.Name,
                    Email = a.Email,
                    UserId = a.UserId
                }).ToListAsync();

            if (UsersList != null)
            {
                response.Data = UsersList;
            }
            else
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.DATA_NOT_AVAILABLE;
            }

            return Ok(response);
        }

        [HttpGet(nameof(GetCandidatesScore)), Authorize(Roles = RolesConstants.ADMIN)]
        public ActionResult<IEnumerable<UserFullDetails>> GetCandidatesScore()
        {
            GeneralResponse response = new GeneralResponse();
            List<UserFullDetails> UsersList = new List<UserFullDetails>();

            var UsersDataList = from a in _context.Users
                                from b in _context.QuizResults
                                where a.Role == RolesConstants.CANDIDATE && a.UserId == b.UserId
                                select new UserFullDetails
                                {
                                    Name = a.Name,
                                    Email = a.Email,
                                    UserId = a.UserId,
                                    Score = b.Score,
                                    AttemptedDate = b.AttemptedDate
                                };

            //  List<UserFullDetails> listofusers = new List<UserFullDetails>();
            //_context.Users.Where(z => z.Role == RolesConstants.CANDIDATE).ToList().ForEach(a =>
            //  {
            //     var s = _context.QuizResults.ToList().Find(ab => ab.UserId == a.UserId);
            //      if (s != null)
            //      {
            //          listofusers.Add(
            //           new UserFullDetails
            //          {
            //              Name = a.Name,
            //              Email = a.Email,
            //              UserId = a.UserId,
            //              Score = s.Score,
            //              AttemptedDate = s.AttemptedDate
            //          });
            //      }
            //  });


            UsersList = UsersDataList.Select(a => a).ToList();

            if (UsersList != null)
            {
                response.Data = UsersList;
            }
            else
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.DATA_NOT_AVAILABLE;
            }
            return Ok(response);
        }

        [HttpPost(nameof(AddCandidate)), Authorize(Roles = RolesConstants.ADMIN)]
        public async Task<ActionResult> AddCandidate(AddUserDTO user)
        {
            GeneralResponse response = new GeneralResponse();
            string PasswordHash;
            string PasswordSalt;
            bool IsEmailAvailable = false;
            int UserId = _tokenservice.GetUserId();

            #region Validations
            if (user?.CandidateName == "")
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_NAME;
                return Ok(response);
            }
            if (user?.Email == "")
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_EMAIL;
                return Ok(response);
            }
            if (user?.Password == "")
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_PASSWORD;
                return Ok(response);
            }

            IsEmailAvailable = this.IsEmailAvailable(user.Email);
            if (IsEmailAvailable)
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.EMAIL_EXSISTS;
                return Ok(response);
            }
            #endregion Validations

            PasswordManager.CreatePassword(user.Password, out PasswordSalt, out PasswordHash);

            User userDetails = new User
            {
                Name = user.CandidateName,
                Email = user.Email,
                Role = RolesConstants.CANDIDATE,
                Password = PasswordHash,
                PasswordSalt = PasswordSalt,
                CreatedBy = UserId
            };

            _context.Users.Add(userDetails);
            await _context.SaveChangesAsync();

            await SendLogCredentialsToUser(user);

            response.Message = string.Format(MessageConstants.USER_CREATION_SUCCESSFULLY, user.CandidateName);
            return Ok(response);
        }

        [HttpGet(nameof(CheckEmailAvailability)), Authorize(Roles = RolesConstants.ADMIN)]
        public ActionResult CheckEmailAvailability(string Email)
        {
            GeneralResponse response = new GeneralResponse();
            bool IsAvailable = IsEmailAvailable(Email);

            response.Status = !IsAvailable ? MessageConstants.SUCCESS : MessageConstants.FAILED;
            response.Message = !IsAvailable ? MessageConstants.AVAILABLE : MessageConstants.EMAIL_EXSISTS;

            return Ok(response);
        }

        [HttpGet(nameof(QuizAttemptStatus)), Authorize(Roles = RolesConstants.CANDIDATE)]
        public ActionResult QuizAttemptStatus()
        {
            GeneralResponse response = new GeneralResponse();
            int id = _tokenservice.GetUserId();
            bool IsAvailable = _context.QuizResults.Any(o => o.UserId == id);

            response.Status = !IsAvailable ? MessageConstants.SUCCESS : MessageConstants.FAILED;
            response.Message = !IsAvailable ? MessageConstants.CANDIDATE_NOT_ATTEMPTED_THE_QUIZ : MessageConstants.CANDIDATE_ATTEMPTED_THE_QUIZ;

            return Ok(response);
        }

        private bool IsEmailAvailable(string Email)
        {
            return _context.Users.Any(o => o.Email == Email);
        }

        private async Task<string> SendLogCredentialsToUser(AddUserDTO user)
        {
            string response = string.Empty;
            Email emailData = new Email()
            {
                From = string.Empty,
                To = user.Email,
                CC = string.Empty,
                Subject = EmailConstants.LOGIN_CREDENTIALS,
                Body = $"<html><body><div style='border: 3px solid red'>" +
                $"<div style='background-color: #FFF; text-align:center;padding: 20px'><img src='https://relevantz.com/wp-content/uploads/2022/03/Relevantz_Logo-on-Whitefinal.svg' width='200'/></div>" +
                $"<div style='background-color: #24125f;padding: 20px;color: #FFF;'><p style=''>Welcome {user.CandidateName} , </p><div >Please use the following link and credentials to start test.</div>" +
                $"<div style='text-align:center;padding: 20px;'><ul style='text-align: left;'><li style='padding: 5px'>Link : <span>https://localhost:3000</span></li><li style='padding: 5px'>Username : <span>{user.Email}</span></li><li style='padding: 5px'>Password : <span>{user.Password}</span></li></ul></div>" +
                $"<div style='text-align:center;padding: 20px;'><h3>Points to remember</h3><ul style='text-align: left;'><li style='padding: 5px'>You need to provide access for your camera and screen.</li><li style='padding: 5px'>We will be recording your activities, which will be used only for monitoring your test.</li><li style='padding: 5px'>Incase if there is any failure due to connectivity, you can again take the quiz from begining.</li></ul></div>" +
                $"<div>All the very best.</div> </div> <div style='text-align:center;background-color: red; padding: 5px;color: #FFF;'> copyright @ relevantz.com</div> </div>" +
                $"</body></html>"
            };

            response = await _emailService.SendEmail(emailData);
            return response;
        }

       
    }
}
