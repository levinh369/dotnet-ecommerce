using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProjectTest1.Models
{
    public class ConversationModel
    {
        [Key]
        public int Id { get; set; }

        // Liên kết đến buyer (UserId)
        public Guid BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        public UserModel? Buyer { get; set; }
        // Tên hoặc title chat (tùy muốn)
        public string? Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public List<MessageModel> Messages { get; set; } = new List<MessageModel>();
    }
}