using ProjectTest1.Models;

namespace ProjectTest1.ViewModels
{
    public class SearchResultViewModel
    {
        public string? Keyword { get; set; }
        public List<ProductModel> Products { get; set; } = new List<ProductModel>();
        public float? MinPrice { get; set; }
        public float? MaxPrice { get; set; }
        public int? SelectedCategoryId { get; set; }
        public List<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
    }

}
