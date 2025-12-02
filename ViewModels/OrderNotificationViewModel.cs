using ProjectTest1.Enums;
using ProjectTest1.Models;

namespace ProjectTest1.ViewModels
{
    public class OrderNotificationViewModel
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? Url { get; set; }
        
        public Guid UserId { get; set; }
        public string? Image { get; set; }
        public NotificationType Type { get; set; }
    }
}
