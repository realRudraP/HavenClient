// Services/MessageService.cs
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

        // Timeout for inactive users (e.g., 5 minutes)
        private readonly TimeSpan _userTimeout = TimeSpan.FromMinutes(5);


        public void Initialize(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
            // Start a timer to periodically prune inactive users
            var cleanupTimer = new Timer(CleanupInactiveUsers, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        }

        // Called by API Controller when a message is received from a peer
        public void ProcessIncomingMessage(Message message)
        {
            // Assign unique ID and timestamp
            message.Id = Interlocked.Increment(ref _messageCounter);
            message.Timestamp = DateTime.UtcNow; // Use UTC for consistency
            message.IsSentByMe = false; // This is received from network

            _messages.Add(message);

            // Update user's last seen time
            if (!string.IsNullOrWhiteSpace(message.SenderNickname))
            {
                _connectedUsers.AddOrUpdate(message.SenderNickname, DateTime.UtcNow, (key, oldvalue) => DateTime.UtcNow);
            }


            // Notify the local UI (Host's UI)
            _dispatcherQueue?.TryEnqueue(() =>
            {
                UIMessageReceived?.Invoke(this, message);
            });

            // Note: With polling, there's nothing else to do here.
            // Clients will fetch this message on their next poll.
            // If using SSE/WebSockets, you'd push the message here.
        }

        // Called by the Host's UI when the host sends a message
        public Message CreateAndProcessLocalMessage(string text, string hostNickname)
        {
            var message = new Message
            {
                Id = Interlocked.Increment(ref _messageCounter),
                SenderNickname = hostNickname,
                Text = text,
                Timestamp = DateTime.UtcNow, // Set UTC time here
                                             // IsSentByMe = true; // We'll handle this specifically for the UI event below
            };
            _messages.Add(message); // Add the original message to the main store
            _connectedUsers.AddOrUpdate(hostNickname, DateTime.UtcNow, (key, oldvalue) => DateTime.UtcNow);

            // Notify the local UI immediately
            _dispatcherQueue?.TryEnqueue(() =>
            {
                // --- Create a COPY specifically for the UI event ---
                // This captures the values *now* and prevents issues if the
                // original 'message' object reference is somehow compromised later.
                var messageForUi = new Message
                {
                    Id = message.Id, // Use the same ID
                    SenderNickname = message.SenderNickname,
                    Text = message.Text, // Copy the text
                    Timestamp = message.Timestamp, // Copy the timestamp (still UTC at this point)
                    IsSentByMe = true // Set flag specifically for this UI instance
                };
                // -----------------------------------------------------

                // Invoke the event using the COPY
                UIMessageReceived?.Invoke(this, messageForUi);
            });

            return message; // Return the original message added to the store
        }

        // Called by API Controller for GET /api/messages
        public IEnumerable<Message> GetMessagesSince(long lastKnownId)
        {
            // Simple implementation: filter messages in memory
            // For larger scale, you'd query a database or more efficient store
            return _messages.Where(m => m.Id > lastKnownId).OrderBy(m => m.Id);
        }

        // Called by API Controller for GET /api/users
        public IEnumerable<string> GetActiveUsers()
        {
            // Prune inactive users before returning the list
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
                // Optional: Broadcast a "user left" message?
                // ProcessIncomingMessage(new Message { SenderNickname="System", Text=$"{user} left (timeout)."})
            }
        }
    }
}