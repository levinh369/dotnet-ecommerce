using ProjectTest1.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.ViewModels
{
    public class ConversationViewModel
    {
        public Guid BuyerId { get; set; }
        public string? Title { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<MessageViewModel> Messages { get; set; } = new List<MessageViewModel>();
    }
}
