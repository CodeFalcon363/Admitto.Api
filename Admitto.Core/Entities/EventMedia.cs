using Admitto.Core.Models;

namespace Admitto.Core.Entities
{
    public class EventMedia
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string Url { get; set; } = null!;
        public MediaType Type { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
