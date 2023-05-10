using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ChatServer.Data;
using ChatServer.Models;
using ChatServer.DTO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;

namespace ChatServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ChatServerContext _context;

        public UsersController(ChatServerContext context)
        {
            _context = context;
        }

        [HttpGet("GetByID")]
        public async Task<ActionResult<User>> GetUserByID(int id_user)
        {
            var user = await _context.User.FirstOrDefaultAsync(x => x.id_user == id_user);
            if (user == null)
            {
                string message = "Not Found";
                return StatusCode(400, message);
            }

            return user;
        }

        [HttpGet("GetByEmail")]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            var user = await _context.User.FirstOrDefaultAsync(x => x.email == email);
            if (user == null)
            {
                string message = "Not Found";
                return StatusCode(400, message);
            }
            return user;
        }

        /// <summary>
        /// Get data from login.
        /// </summary>
        [HttpPost("Login")]
        public async Task<ActionResult<User>> Login(UserLoginDTO user_request)
        {
            var user = await _context.User.FirstOrDefaultAsync(x => x.email == user_request.email);
            if(user == null)
            {
                string message = "Incorrect email";
                return StatusCode(400, message);
            }
            if(user.password != user_request.password)
            {
                string message = "Incorrect password";
                return StatusCode(400, message);
            }
            return user;
        }

        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(UserRegisterDTO user_request)
        {
            var user = await _context.User.FirstOrDefaultAsync(x => x.email == user_request.email);
            if (user != null)
            {
                string message = "User with this login alredy exsists";
                return StatusCode(400, message);
            }

            var tempuser = new User
            {
                nickname = user_request.nickname,
                email = user_request.email,
                password = user_request.password
            };
          
            _context.User.Add(tempuser);
            await _context.SaveChangesAsync();
            return await Login(new UserLoginDTO() { email = tempuser.email, password = tempuser.password});
        }

    }
}
