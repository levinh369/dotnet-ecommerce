
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ProjectTest1.Models
{
    public class CategoryModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        public int? ParentId { get; set; }
        public bool IsActive { get; set; } = true;

        public CategoryModel? Parent { get; set; }
        public ICollection<CategoryModel> Children { get; set; }=new List<CategoryModel>();
        public ICollection<ProductModel>? Products { get; set; }
    }

}