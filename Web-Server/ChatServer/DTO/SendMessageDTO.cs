using System.ComponentModel.DataAnnotations;

namespace ChatServer.DTO
{
    public class SendMessageDTO
    {

        public string? text_message { get; set; }

        public int id_chat { get; set; }
    }
}
