
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProjectTest1.Models
{
    public class ColorModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ColorId { get; set; }

        [Required]
        [StringLength(50)]
        public string ColorName { get; set; } = string.Empty;

        [StringLength(10)]
        public string? ColorCode { get; set; } // ví dụ #FF0000

        public bool IsActive { get; set; } = true;

        public ICollection<ProductVariantModel>? Variants { get; set; }
    }

}