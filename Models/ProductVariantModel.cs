using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace ProjectTest1.Models
{
    public class ProductVariantModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductVariantId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public ProductModel Product { get; set; } = null!;

        [Required]
        public int SizeId { get; set; }
        public SizeModel Size { get; set; } = null!;

        [Required]
        public int ColorId { get; set; }
        public ColorModel Color { get; set; } = null!;

        public int StockQuantity { get; set; }

        [StringLength(100)]
        public string? SKU { get; set; }

        [StringLength(250)]
        public string? ImageUrl { get; set; }
        public float? Price { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
