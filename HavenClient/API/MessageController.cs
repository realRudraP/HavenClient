// API/MessageController.cs
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

        // POST /api/message - Receive a message from a client
        [HttpPost]
        public IActionResult PostMessage([FromBody] Message message)
        {
            // Basic validation
            if (message == null || string.IsNullOrWhiteSpace(message.Text) || string.IsNullOrWhiteSpace(message.SenderNickname))
            {
                return BadRequest("Invalid message format. Requires Text and SenderNickname.");
            }

            // Process it (add to store, notify local UI)
            _messageService.ProcessIncomingMessage(message);

            // Let the client know it was received.
            // Could return the assigned ID/Timestamp if desired: Ok(new { messageId = processedMessage.Id })
            return Ok("Message received by host.");
        }

        // GET /api/message?since={id} - Client polls for new messages
        [HttpGet]
        public ActionResult<IEnumerable<Message>> GetMessages([FromQuery] long since = 0) // Default to 0 to get all initially
        {
            var newMessages = _messageService.GetMessagesSince(since);
            return Ok(newMessages);
        }
    }

    // Add a new controller for user info
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly MessageService _messageService; // Assumes MessageService manages users too

        public UsersController(MessageService messageService)
        {
            _messageService = messageService;
        }

        // GET /api/users - Get list of active users
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetUsers()
        {
            var users = _messageService.GetActiveUsers();
            return Ok(users);
        }
    }
}