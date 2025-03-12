using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Models
{
    public class CardImage
    {
        public Guid NewcomerId { get; set; }
        public string FullName { get; set; }
        public byte[] ImageData { get; set; }
        public string ImageMimeType { get; set; } = "image/png";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string[] DeliveryChannels { get; set; }
    }
}