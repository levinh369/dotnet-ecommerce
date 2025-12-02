using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.Models
{
    public class OrderDetailModel
    {
        [Key]
        public int OrderDetailId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be >= 0")]
        public double? UnitPrice { get; set; }

        // Navigation
        public OrderModel? Order { get; set; }
        public ProductVariantModel? ProductVariant { get; set; }
    }
}
