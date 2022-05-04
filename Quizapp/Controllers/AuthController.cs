using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.Common;
using Quizapp.Constants;
using Quizapp.Models;
using Quizapp.Services;



namespace Quizapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenservice;

        public AuthController(DataContext context, ITokenService tokenservice)
        {
            _context = context;
            _tokenservice = tokenservice;
        }

        [HttpPost(nameof(Login))]
        [AllowAnonymous]
        public ActionResult Login(LoginDto request)
        {
            bool isPasswordValid = false;
            User user = new User();
            LoginResponseDetails LoginDetails = new LoginResponseDetails();
            GeneralResponse response = new GeneralResponse();

            if (request.UserName == "")
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_USERNAME;
                return Ok(response);
                //throw new ValidationException(MessageConstants.INVALID_USERNAME);
            }
            if (request.Password == "")
            {
                response.Status = MessageConstants.FAILED;
                response.Message = MessageConstants.INVALID_PASSWORD;
                return Ok(response);
                //throw new ValidationException(MessageConstants.INVALID_PASSWORD);
            }

            user = _context.Users.Where(a => a.Email == request.UserName).FirstOrDefault();

            if (user != null)
            {
                isPasswordValid = PasswordManager.ValidatePassword(request.Password, user.Password, user.PasswordSalt);
                if (isPasswordValid)
                {
                    LoginDetails = new LoginResponseDetails
                    {
                        Name = user.Name,
                        Role = user.Role,
                        Token = _tokenservice.CreateToken(user)
                    };
                }
                else
                {
                    response.Status = MessageConstants.FAILED;
                    response.Message = MessageConstants.INCORRECT_PASSWORD;
                    return Ok(response);
                    //throw new ValidationException(MessageConstants.INCORRECT_PASSWORD);
                }
            }
            else
            {
                response.Status = MessageConstants.FAILED;
                response.Message = string.Format(MessageConstants.USER_NOT_FOUND_FOR_GIVEN_USERNAME, request.UserName);
                return Ok(response);
                // throw new DomainNotFoundException(String.Format(MessageConstants.USER_NOT_FOUND_FOR_GIVEN_USERNAME, request.UserName));
            }

            response.Data = LoginDetails;
            return Ok(response);
        }

        
    }
}
