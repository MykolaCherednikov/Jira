using ChatServer.Data;
using ChatServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Net.WebSockets;
using System.Text;

namespace ChatServer.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class WebSocketController : ControllerBase
    {
        private readonly ChatServerContext _context;

        private static readonly Dictionary<int, List<WebSocket>> _connections = new();

        public WebSocketController(ChatServerContext context)
        {
            _context = context;
        }

        [Route("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await Echo(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            int id_user = Convert.ToInt32(System.Text.Encoding.UTF8.GetString(buffer));

            if (!_connections.ContainsKey(id_user))
            {
                _connections.Add(id_user, new());
                _connections[id_user].Add(webSocket);
            }
            else
            {
                _connections[id_user].Add(webSocket);
            }

            buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                string message = System.Text.Encoding.UTF8.GetString(buffer);

                string[] data = message.Split(";");
                int id_chat = Convert.ToInt32(data[0]);
                string msg = data[1];

                var users_list = await _context.UserToChat.Where(x => x.rk_id_chat == id_chat).Select(x => x.RkIdUserNavigation).ToListAsync();
                List<int> id_user_list = new List<int>();
                foreach (var user in users_list)
                {
                    id_user_list.Add(user.id_user);
                }

                var online_user_list = _connections.Keys.Intersect(id_user_list);
                foreach (var key in online_user_list)
                {
                    foreach (var webSockett in _connections[key])
                    {
                        if (webSockett.State == WebSocketState.Open)
                        {
                            await webSockett.SendAsync(
                            new ArraySegment<byte>(Encoding.UTF8.GetBytes(_context.User.FirstOrDefault(x => x.id_user == id_user).nickname+";"+id_chat + ";" + msg + ";")),
                            receiveResult.MessageType,
                            receiveResult.EndOfMessage,
                            CancellationToken.None);
                        }
                    }
                }
                Message mess = new Message();
                mess.rk_chat = id_chat;
                mess.rk_user = id_user;
                mess.text_message = msg;
                mess.data_time = DateTime.Now;

                await _context.Message.AddAsync(mess);
                await _context.SaveChangesAsync();

                buffer = new byte[1024 * 4];
            }

            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
    }
}
