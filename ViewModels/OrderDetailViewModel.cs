namespace ProjectTest1.ViewModels
{
        public class OrderDetailItemViewModel
        {
            public int ProductId { get; set; }
            public string? ProductName { get; set; }
            public string? image { get; set; }
            public string? Size { get; set; }
            public string? Color { get; set; }
            public string? Description { get; set; }
            public int Quantity { get; set; }
            public double? UnitPrice { get; set; }
            public double? TotalPrice => Quantity * UnitPrice;  // tính luôn
            public int ProductVariantId { get; set; }   // id sản phẩm
            public bool HasReview { get; set; }
            public int OrderDetailId { get; set; }

    }
        public class OrderDetailViewModel
        {
            public int OrderId { get; set; }
            
            // Thông tin đơn
            public string? UserName { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string? Address { get; set; }
            public DateTime? ShipDate { get; set; }
            public string? Status { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? PaymentMethod { get; set; }
            public float? FinalAmount { get; set; }
            public float? DiscountValue { get; set; }

        // Tổng tiền
        public double? TotalAmount { get; set; }

            // Danh sách item
            public List<OrderDetailItemViewModel>? Items { get; set; }
        }
    
}
