using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.ViewModels
{
    public class CategoryViewModel
    {
        public int? CategoryId { get; set; }
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        public string CategoryName { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        public bool IsActive { get; set; }

        public string? ParentName { get; set; }
        public string? ParentName2 { get; set; }
        public int? ParentLevel1Id { get; set; }
        public int? ParentLevel2Id { get; set; }
        public Boolean hasChildren { get; set; }
        public List<CategoryViewModel> Children { get; set; } = new();
        // ➕ Dùng để render dropdown danh sách danh mục cha
        public List<SelectListItem>? ParentCategoryList { get; set; }
        public List<SelectListItem>? ParentLevel2List { get; set; }
    }
}
