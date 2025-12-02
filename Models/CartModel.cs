using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.Models
{
    public class CartModel
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public Guid UserId { get; set; }   // AspNetUsers.Id (string nếu Identity mặc định)

        public UserModel User { get; set; } = null!;

        public ICollection<CartItemModel> CartItems { get; set; } = new List<CartItemModel>();

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}
