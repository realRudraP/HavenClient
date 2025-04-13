 using HavenClient.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Concurrent; // For thread-safe collections
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HavenClient.Services
{
    public class MessageService
    {
        public event EventHandler<Message>? UIMessageReceived; // Renamed for clarity

        private DispatcherQueue? _dispatcherQueue;
        private readonly ConcurrentBag<Message> _messages = new ConcurrentBag<Message>();
        private long _messageCounter = 0;
        private readonly ConcurrentDictionary<string, DateTime> _connectedUsers = new ConcurrentDictionary<string, DateTime>(); // Nickname -> Last Seen

         private readonly TimeSpan _userTimeout = TimeSpan.FromMinutes(5);


        public void Initialize(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
             var cleanupTimer = new Timer(CleanupInactiveUsers, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        }

         public void ProcessIncomingMessage(Message message)
        {
             message.Id = Interlocked.Increment(ref _messageCounter);
            message.Timestamp = DateTime.UtcNow; // Use UTC for consistency
            message.IsSentByMe = false; // This is received from network

            _messages.Add(message);

             if (!string.IsNullOrWhiteSpace(message.SenderNickname))
            {
                _connectedUsers.AddOrUpdate(message.SenderNickname, DateTime.UtcNow, (key, oldvalue) => DateTime.UtcNow);
            }


             _dispatcherQueue?.TryEnqueue(() =>
            {
                UIMessageReceived?.Invoke(this, message);
            });

           }

         public Message CreateAndProcessLocalMessage(string text, string hostNickname)
        {
            var message = new Message
            {
                Id = Interlocked.Increment(ref _messageCounter),
                SenderNickname = hostNickname,
                Text = text,
                Timestamp = DateTime.UtcNow, // Set UTC time here
             };
            _messages.Add(message); // Add the original message to the main store
            _connectedUsers.AddOrUpdate(hostNickname, DateTime.UtcNow, (key, oldvalue) => DateTime.UtcNow);

             _dispatcherQueue?.TryEnqueue(() =>
            {
                   var messageForUi = new Message
                {
                    Id = message.Id, // Use the same ID
                    SenderNickname = message.SenderNickname,
                    Text = message.Text, // Copy the text
                    Timestamp = message.Timestamp, // Copy the timestamp (still UTC at this point)
                    IsSentByMe = true // Set flag specifically for this UI instance
                };
 
                 UIMessageReceived?.Invoke(this, messageForUi);
            });

            return message; // Return the original message added to the store
        }

         public IEnumerable<Message> GetMessagesSince(long lastKnownId)
        {
              return _messages.Where(m => m.Id > lastKnownId).OrderBy(m => m.Id);
        }

         public IEnumerable<string> GetActiveUsers()
        {
             CleanupInactiveUsers(null);
            return _connectedUsers.Keys.ToList(); // Return a snapshot
        }

        private void CleanupInactiveUsers(object? state)
        {
            var cutoff = DateTime.UtcNow - _userTimeout;
            var inactiveUsers = _connectedUsers.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();

            foreach (var user in inactiveUsers)
            {
                _connectedUsers.TryRemove(user, out _);
                Console.WriteLine($"Removed inactive user: {user}");
              }
        }
    }
}