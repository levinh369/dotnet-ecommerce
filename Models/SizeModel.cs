using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.Models
{

    public class SizeModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SizeId { get; set; }

        [Required]
        [StringLength(10)]
        public string SizeName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<ProductVariantModel>? Variants { get; set; }
    }
}
