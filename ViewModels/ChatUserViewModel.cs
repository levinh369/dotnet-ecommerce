namespace ProjectTest1.ViewModels
{
    public class ChatUserViewModel
    {
        public Guid BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime SentTime { get; set; }
        public int ConversationId { get; set; }
        public bool isRead { get; set; }    
        public string AvatarUrl { get; set; }
    }
}
