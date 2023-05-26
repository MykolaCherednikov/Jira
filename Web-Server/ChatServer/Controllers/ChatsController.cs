using ChatServer.Data;
using ChatServer.DTO;
using ChatServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;

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
            //_context.Chat.Include(x=>x.Messages).ToList();
        }

        /// <summary>
        /// Return Chats by User.
        /// </summary>
        /// <response code="400">
        /// Invalid auth_token:
        /// 
        ///     {
        ///         "code": 9007,
        ///         "message": "You are not authorized"
        ///     }
        /// </response>
        [Authorize]
        [HttpGet("GetChats")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorDTO))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ChatDTO>))]
        public async Task<ActionResult<IEnumerable<ChatDTO>>> GetChats()
        {
            int id_user = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value);

            var user = await _context.User.FirstOrDefaultAsync(x => x.id_user == id_user);
            if (user == null)
            {
                return BadRequest(new ErrorDTO(9007, "You are not authorized"));
            }

            var chats = await _context.UserToChat.Where(x => x.rk_id_user == user.id_user).Select(x => x.RkIdChatNavigation).ToListAsync();

            List<ChatDTO> result = new List<ChatDTO>();

            foreach (var chat in chats)
            {
                ChatDTO chatDTO = new ChatDTO();
                chatDTO.id_chat = chat.id_chat;
                chatDTO.name_chat = chat.name_chat;
                chatDTO.link = chat.link;
                chatDTO.rk_type_chat = chat.rk_type_chat;
                result.Add(chatDTO);
            }

            return Ok(result);
        }

        [Authorize]
        [HttpPost("CreateChat")]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorDTO))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChatDTO))]
        public async Task<ActionResult> CreateChat(ChatInputDTO chatInput)
        {
            int id_user = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value);
            Chat chat = new Chat();

            if (chatInput.rk_type_chat == 1)
            {
                if(_context.User.FirstOrDefault(x => x.id_user == chatInput.id_user) == null)
                {
                    return BadRequest();
                }

                chat = await CreatePrivateChat(id_user, chatInput.id_user);

                if(chat == null)
                {
                    return BadRequest();
                }
            }
            else if(chatInput.rk_type_chat == 2)
            {

            }
            else
            {
                return BadRequest();
            }

            ChatDTO result = new ChatDTO();

            result.id_chat = chat.id_chat;
            result.name_chat = chat.name_chat;
            result.rk_type_chat = chat.rk_type_chat;
            result.link = chat.link;

            var users_receiver_list = await _context.UserToChat.Where(c => c.rk_id_chat == chat.id_chat).Select(c => c.rk_id_user).ToListAsync();

            JObject chatDTO = new JObject();

            chatDTO.Add("type", WebSocketDataType.CreateChat.ToString());
            chatDTO.Add("data", JObject.FromObject(result));

            var message_json = JsonConvert.SerializeObject(chatDTO);

            foreach (var id_user_receiver in users_receiver_list)
            {
                /*if (!WebSocketController.connections.Keys.Contains(id_user_receiver) || id_user == id_user_receiver)
                    continue;*/

                if (!WebSocketController.connections.Keys.Contains(id_user_receiver))
                    continue;

                foreach (var webSocket in WebSocketController.connections[id_user_receiver])
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_json)),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                    }
                }
            }

            

            return Ok(result);
        }

        [HttpGet("GetAllChats")]
        public async Task<List<Chat>> GetAllChats()
        {
            return await _context.Chat.ToListAsync();
        }

        [HttpGet("GetChatById")]
        public async Task<Chat> GetAllChats(int id)
        {
            return await _context.Chat.FirstOrDefaultAsync(c => c.id_chat == id);
        }

        private async Task<Chat> CreatePrivateChat(int id_user_creator, int id_user_invited)
        {
            List<Chat> user_creator_chats = await _context.UserToChat.Where(x => x.rk_id_user == id_user_creator).Select(x => x.RkIdChatNavigation).Where(x => x.rk_type_chat == 1).ToListAsync();
            List<Chat> user_invited_chats = await _context.UserToChat.Where(x => x.rk_id_user == id_user_invited).Select(x => x.RkIdChatNavigation).Where(x => x.rk_type_chat == 1).ToListAsync();

            if(user_creator_chats.Intersect(user_invited_chats).Count() != 0)
            {
                return null;
            }

            string nickname_creator = _context.User.FirstOrDefault(x => x.id_user == id_user_creator).nickname;
            string nickname_invited = _context.User.FirstOrDefault(x => x.id_user == id_user_invited).nickname;

            Chat chat = new Chat();
            chat.name_chat = $"Приватний чат: {nickname_creator}|{nickname_invited}";
            chat.rk_type_chat = 1;

            Random rnd = new Random();
            string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder sb = new StringBuilder(Alphabet.Length - 1);
            int Position = 0;

            for (int i = 0; i < Alphabet.Length; i++)
            {
                Position = rnd.Next(0, Alphabet.Length - 1);
                sb.Append(Alphabet[Position]);
            }
            
            chat.link = sb.ToString();

            Chat new_chat = (await _context.Chat.AddAsync(chat)).Entity;
            await _context.SaveChangesAsync();

            UserToChat UTC_creator = new UserToChat();
            UserToChat UTC_invited = new UserToChat();
            UTC_creator.rk_id_chat = new_chat.id_chat;
            UTC_creator.rk_id_user = id_user_creator;
            UTC_invited.rk_id_chat = new_chat.id_chat;
            UTC_invited.rk_id_user = id_user_invited;
            

            await _context.UserToChat.AddAsync(UTC_creator);
            await _context.UserToChat.AddAsync(UTC_invited);
            await _context.SaveChangesAsync();

            return new_chat;
        }
    }
}
