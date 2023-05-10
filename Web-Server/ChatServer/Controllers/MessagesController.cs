using ChatServer.Data;
using ChatServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ChatServerContext _context;

        public MessagesController(ChatServerContext context)
        {
            _context = context;
        }


        [HttpGet("GetUserFromIdMessage")]
        public async Task<ActionResult<Message>> GetUserFromIdMessage(int id_message)
        {
            var user = await _context.Message.FindAsync(id_message);
            return user;
        }

        [HttpGet("GetAllMessages")]
        public async Task<ActionResult<IEnumerable<Message>>> GetAllMessages()
        {
            var messages = await _context.Message.Include(x => x.RkChatNavigation).ToListAsync();

            return messages;
        }
    }
}
