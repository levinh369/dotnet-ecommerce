using CloudinaryDotNet.Actions;
using ProjectTest1.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.Models
{
    public class OrderNotificationModel
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public OrderModel? Order { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public Guid? UserId { get; set; }
        [ForeignKey("UserId")]
        public UserModel? User { get; set; }
        public string? Url { get; set; }
        public NotificationType Type { get; set; } // e.g., "OrderUpdate", "Promotion"
    }
}
