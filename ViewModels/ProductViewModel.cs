using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; } // Dùng cho sửa, tạo mới có thể bỏ qua hoặc để 0

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Img { get; set; }

        public bool IsActive { get; set; } = true;

        public int Quantity { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0, float.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public float? Price { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        // Danh sách category cho dropdown
        public string? SelectedCategoryLevel1Id { get; set; }
        public string? SelectedCategoryLevel1Name { get; set; }
        public List<SelectListItem> CategoriesLevel1 { get; set; } = new();

        // Level 2
        public string? SelectedCategoryLevel2Id { get; set; }
        public string? SelectedCategoryLevel2Name { get; set; }
        public List<SelectListItem> CategoriesLevel2 { get; set; } = new();

        // Level 3
        public string? SelectedCategoryLevel3Id { get; set; }
        public string? SelectedCategoryLevel3Name { get; set; }
        public List<SelectListItem> CategoriesLevel3 { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        // Thêm để nhận ảnh chính upload
        public IFormFile? MainImageFile { get; set; }

        // Thêm để nhận nhiều ảnh phụ upload
        public List<IFormFile>? SubImagesFiles { get; set; }
        public string? ExistingSubImages { get; set; }
        public List<string> SubImages { get; set; } = new List<string>();
        public List<SizeViewModel> Sizes { get; set; } = new List<SizeViewModel>();
        public List<ColorViewModel> Colors { get; set; } = new List<ColorViewModel>();
        public List<ProductVariantViewModel> Variants { get; set; } = new List<ProductVariantViewModel>();
        public List<ColorImageViewModel> ColorImages { get; set; } = new();
        public int? SelectedVariantId { get; set; }
        public int ProductVariantId { get; set; }
        public List<ReviewViewModel> Reviews { get; set; } = new();

    }
}
