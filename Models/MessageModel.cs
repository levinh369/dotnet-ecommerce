
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProjectTest1.Models
{
    public class MessageModel
    {
        [Key]
        public int Id { get; set; }

        // Liên kết đến Conversation
        public int ConversationId { get; set; }

        // Ai gửi (buyer hoặc admin)
        public Guid SenderId { get; set; }
        public UserModel? Sender { get; set; }

        [Required]
        public string? Content { get; set; }
        public int? ReplyToMessageId { get; set; }

        // 👉 Navigation đến tin nhắn gốc
        [ForeignKey("ReplyToMessageId")]
        public MessageModel? ReplyToMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tin nhắn đã đọc chưa (cho admin hoặc buyer)
        public bool IsRead { get; set; } = false;
        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public ProductModel? Product { get; set; }
        // Navigation property
        [ForeignKey("ConversationId")]
        public ConversationModel? Conversation { get; set; }
    }
}
