using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HavenClient.Models
{
    public class Message
    {
        public long Id { get; set; } 
        public string SenderNickname { get; set; } = "Unknown"; 
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsSentByMe { get; set; } 
    }
}
