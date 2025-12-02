namespace ProjectTest1.ViewModels
{
    public class CartViewModel
    {
        public int CartId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public List<CartItemViewModel> Items { get; set; } = new();

        public int TotalQuantity => Items.Sum(i => i.Quantity);

        public float? TotalAmount => Items.Sum(i => i.TotalPrice);
    }
}
