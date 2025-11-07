using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManager.DBContext;
using TaskManager.DTOs;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly TaskManagerDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<User> userManager, 
            TaskManagerDbContext dbContext, 
            IConfiguration configuration)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _configuration = configuration;
        }
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if(userExists != null)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new {Status = "Error", Message = "User already exists!"});
            }
            User user = new User()
            {
                Email = model.Email,
                UserName = model.Email,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if(!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                                       new { Status = "Error", Message = "User creation failed! Please check user details and try again." });
            }
            await _userManager.AddToRoleAsync(user, "User");
            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password)) {
                var accessToken = await GenerateAccessToken(user);
                var refreshToken = await GenerateRefreshToken(user);
                return Ok(new TokenResponse
                {
                    AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
                    RefreshToken = refreshToken.Token
                });
            }
            return Unauthorized(new { Message = "Invalid email or password"});
        }
        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest token)
        {
            var storedToken = _dbContext.RefreshTokens.FirstOrDefault(t => t.Token == token.RefreshToken);
            if(storedToken == null || storedToken.IsRevoked || storedToken.ExpiresOn < DateTime.UtcNow)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token"});
            }
            if(storedToken.UserId == null)
            {
                return Unauthorized(new { Message = "Invalid refresh token user"});
            }
            if(storedToken.IsRevoked)
            {
                return Unauthorized(new { Message = "Refresh token has been revoked"});
            }
            if(storedToken.ExpiresOn < DateTime.UtcNow)
            {
                return Unauthorized(new { Message = "Refresh token has expired"});
            }
            var user = await _userManager.FindByIdAsync(storedToken.UserId);
            if(user == null)
            {
                return Unauthorized(new { Message = "User not found"});
            }
            var newAccessToken = await GenerateAccessToken(user);
            var newRefreshToken = await GenerateRefreshToken(user);
            storedToken.IsRevoked = true;
            _dbContext.RefreshTokens.Update(storedToken);
            return Ok(new TokenResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken.Token
            });
        }
        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest token)
        {
            var storedToken = _dbContext.RefreshTokens.FirstOrDefault(t => t.Token == token.RefreshToken);
            if(storedToken == null)
            {
                return Ok(new { Message = "Logged out successfully"});
            }
            storedToken.IsRevoked = true;
            _dbContext.RefreshTokens.Update(storedToken);
            await _dbContext.SaveChangesAsync();
            return Ok(new { Message = "Logged out successfully"});
        }
        private async Task<JwtSecurityToken> GenerateAccessToken(User user)
        {
            var userRoles = await  _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new("UserId", user.Id),
                new("Email", user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            foreach (var userRole in userRoles) { 
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
            var tokenDuration = Convert.ToDouble(_configuration["JWT:AccessTokenInMinutes"]);
            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(tokenDuration),
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
            return token;
        } 
        private async Task<RefreshToken> GenerateRefreshToken(User user)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var tokenString = Convert.ToBase64String(randomNumber);
            var tokenDuration = Convert.ToDouble(_configuration["JWT:RefreshTokenDurationInDays"]);
            var refreshToken = new RefreshToken
            {
                Token = tokenString,
                ExpiresOn = DateTime.UtcNow.AddDays(tokenDuration),
                UserId = user.Id
            };
            await _dbContext.RefreshTokens.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();
            return refreshToken;
        }
    }
}
