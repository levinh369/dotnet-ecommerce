namespace ProjectTest1.ViewModels
{
    public class ProductVariantViewModel
    {
        public int ProductVariantId { get; set; }
        public int? ProductId { get; set; }
        public int? ColorId { get; set; }
        public int? SizeId { get; set; }
        public string? ColorName { get; set; }
        public string? SizeName { get; set; }
        public int StockQuantity { get; set; }
        public string? SKU { get; set; }
        public bool IsSelected { get; set; }
        public float? Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }
    public class ProductVariantPageViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public List<ColorViewModel>? Colors { get; set; }
        public List<SizeViewModel>? Sizes { get; set; }
        
        public List<ProductVariantViewModel>? Variants { get; set; }
    }
    
    public class ColorImageViewModel
    {
        public int ColorId { get; set; }
        public IFormFile? ImageFile { get; set; }
    }

}
