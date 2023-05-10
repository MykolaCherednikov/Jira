using ChatServer.Data;
using ChatServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Versioning;

namespace ChatServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
        private readonly ChatServerContext _context;

        public ChatsController(ChatServerContext context)
        {
            _context = context;
            _context.Chat.Include(x=>x.Messages).ToList();
        }

        [HttpGet("GetChatByID")]
        public async Task<ActionResult<Chat>> GetChatById(int id_chat)
        {
            var chat = await _context.Chat.FirstOrDefaultAsync(x => x.id_chat == id_chat);
            if (chat == null)
            {
                string message = "Not Found";
                return StatusCode(400, message);
            }
            return chat;
        }

        [HttpGet("GetChatsByUserId")]
        public async Task<ActionResult<IEnumerable<Chat>>> GetChatsByUserId(int id_user)
        {
            
            var chatsFirst = await _context.UserToChat.Where(x => x.rk_id_user == id_user).Select(x => x.RkIdChatNavigation).ToListAsync();

            if (chatsFirst.Count == 0)
            {
                string message = "Not Found";
                return StatusCode(400, message);
            }



            return chatsFirst;
        }


    }
}
