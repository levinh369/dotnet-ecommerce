namespace ProjectTest1.ViewModels
{
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? Sender { get; set; } // "admin" hoặc "buyer"
        public string? Content { get; set; }
        public int? ProductId { get; set; }
        public int? ReplyToMessageId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

    }
}
