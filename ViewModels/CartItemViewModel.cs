namespace ProjectTest1.ViewModels
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }

        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int Quantity { get; set; }

        public float? UnitPrice { get; set; }

        public float? TotalPrice => UnitPrice * Quantity;
    }
}
