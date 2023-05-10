using ChatServer.Data;
using ChatServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserToChatsController : ControllerBase
    {
        private readonly ChatServerContext _context;

        public UserToChatsController(ChatServerContext context)
        {
            _context = context;
            _context.UserToChat.Include(x => x.RkIdUserNavigation).ToList();
        }

        [HttpGet("GetAllUserToChat")]
        public async Task<ActionResult<IEnumerable<UserToChat>>> GetAllUserToChat()
        {
            var userToChats = await _context.UserToChat.ToListAsync();

            return userToChats;
        }

        [HttpGet("TEST")]
        public async Task<ActionResult<IEnumerable<Chat>>> TEST()
        {
            //var chatsFirst = await _context.UserToChat.FirstOrDefaultAsync();
            var chatsFirst = _context.UserToChat;
            var test = chatsFirst.ToList();
            var chatsSecond = chatsFirst.Select(x => x.RkIdChatNavigation);
            var test2 = chatsSecond.ToList();
            var chatsThird = await chatsSecond.ToListAsync();

            return chatsThird;
        }
    }
}
