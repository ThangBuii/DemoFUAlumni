using ApiAngular.Models;
using ApiAngular.Models.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiAngular.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Fualumni_demoContext _context;
        private readonly HttpClient _httpClient;

        public AccountController(IConfiguration configuration, Fualumni_demoContext context, HttpClient httpClient)
        {
            _configuration = configuration;
            _context = context;
            _httpClient = httpClient;
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequestDTO request)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == request.Email && u.Password == request.Password);
            if (user == null)
            {
                return Unauthorized("Invalid login attempt.");
            }
            var externalApiUrl = "http://fal-dev.eba-55qpmvbp.ap-southeast-1.elasticbeanstalk.com/api/auth/login"; // Replace with actual URL

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                username = "khanm",
                password = "123456"
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(externalApiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                return Unauthorized("Invalid login attempt.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ExternalLoginResponse>(responseContent);

            var token = GenerateJwtToken(user);
            return Ok(new { SystemToken = responseObject.Token,UserToken = token });
        }
        public class ExternalLoginResponse
        {
            public string Token { get; set; }
            public int UserRole { get; set; }
            public string SystemName { get; set; }
        }

        //[HttpPost("Register")]
        //public IActionResult Register(RegisterRequestDTO request)
        //{
        //    try
        //    {
        //        var user = _userService.RegisterUser(request);
        //        var token = GenerateJwtToken(user);
        //        return Ok(new { Token = token });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Conflict();
        //    }
        //}

        //[HttpPost("ChangePassword")]
        //public IActionResult ChangePassword(ChangePasswordRequestDTO request)
        //{
        //    var result = _userService.ChangePassword(request);
        //    if (!result)
        //    {
        //        return BadRequest("Password change failed.");
        //    }
        //    return Ok("Password changed successfully.");
        //}

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.StudentNumber.ToString()),
            new Claim("StudentNumber", user.StudentNumber.ToString())
        };

            var role = user.Role == 1 ? "Admin" : (user.Role == 2 ? "Staff" : "User");

            claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
