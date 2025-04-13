 using Microsoft.AspNetCore.Mvc;
using HavenClient.Models;
using HavenClient.Services;
using System.Collections.Generic; // Required for IEnumerable

namespace HavenClient.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;

        public MessageController(MessageService messageService)
        {
            _messageService = messageService;
        }

         [HttpPost]
        public IActionResult PostMessage([FromBody] Message message)
        {
             if (message == null || string.IsNullOrWhiteSpace(message.Text) || string.IsNullOrWhiteSpace(message.SenderNickname))
            {
                return BadRequest("Invalid message format. Requires Text and SenderNickname.");
            }

             _messageService.ProcessIncomingMessage(message);

              return Ok("Message received by host.");
        }

         [HttpGet]
        public ActionResult<IEnumerable<Message>> GetMessages([FromQuery] long since = 0) // Default to 0 to get all initially
        {
            var newMessages = _messageService.GetMessagesSince(since);
            return Ok(newMessages);
        }
    }

     [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly MessageService _messageService; // Assumes MessageService manages users too

        public UsersController(MessageService messageService)
        {
            _messageService = messageService;
        }

         [HttpGet]
        public ActionResult<IEnumerable<string>> GetUsers()
        {
            var users = _messageService.GetActiveUsers();
            return Ok(users);
        }
    }
}