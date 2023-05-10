using ChatServer.Data;
using ChatServer.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ChatServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly JWTSettings _options;
        private readonly ChatServerContext _context;

        public TokenController(IOptions<JWTSettings> options, ChatServerContext context)
        {
            _options = options.Value;
            _context = context;
        }

        [HttpPost("GetToken")]
        public async Task<IActionResult> GetToken(UserLoginDTO userLoginDTO)
        {
            var user = await _context.User.FirstOrDefaultAsync(x => x.email == userLoginDTO.email);
            if (user == null)
            {
                string message = "Incorrect email";
                return NotFound(message);
            }
            if (user.password != userLoginDTO.password)
            {
                string message = "Incorrect password";
                return NotFound(message);
            }

            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, user.nickname),
                new Claim(ClaimTypes.Role, "User")
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)),
                notBefore: DateTime.UtcNow,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

            return Ok(new JwtSecurityTokenHandler().WriteToken(jwt));
        }
    }
}
