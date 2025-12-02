using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.Models
{
    public class CartItemModel
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public int CartId { get; set; }
        public CartModel Cart { get; set; } = null!;

        [Required]
        public int ProductVariantId { get; set; }
        public ProductVariantModel ProductVariant { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }

        public float? UnitPrice { get; set; }
    }
}
