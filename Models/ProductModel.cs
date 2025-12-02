using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.Models
{
    public class ProductModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Img { get; set; }
        public bool IsActive { get; set; } = true;
        [Required]
        public float? Price { get; set; }
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public CategoryModel Category { get; set; } = null!;
        public ICollection<ProductVariantModel> Variants { get; set; } = new List<ProductVariantModel>();

        public ICollection<ProductImageModel> Images { get; set; } = new List<ProductImageModel>();
        public ICollection<ProductColorImageModel>? ProductImages { get; set; } = new List<ProductColorImageModel>();

    }
}
