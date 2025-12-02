using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.Models
{
    public class ProductImageModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductImageId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public ProductModel Product { get; set; } = null!;

        [Required]
        [StringLength(250)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
