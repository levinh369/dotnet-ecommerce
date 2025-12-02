using ProjectTest1.Enums;

namespace ProjectTest1.ViewModels
{
    public class OrderViewModel
    {
        public Guid UserId { get; set; }                // khách hàng
        public int OrderId { get; set; }
        public String? userName { get; set; }
        public String? Email {  get; set; }
        public string? Phone { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ShipDate { get; set; }
        public DateTime? UpdatedAt { get; set; } // ngày tạo
        public string? ShippingAddress { get; set; }     // địa chỉ giao hàng
        public string? shippingProvinceName { get; set; }
        public string? shippingDistrictName { get; set; }
        public string? shippingWardName { get; set; }
        public string? Address { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? WardId { get; set; }
        public float? Amount { get; set; }          // tổng tiền
        public string? PaymentMethod { get; set; }
        public StatusOrderEnum? Status { get; set; } 
        public string? StatusName { get; set; }
        public List<OrderDetailItemViewModel>? Items { get; set; }
        public int? SelectedVoucherId { get; set; }
        public List<VoucherViewModel> Vouchers { get; set; } = new List<VoucherViewModel>();
        public float DiscountValue { get; set; }
        public float FinalAmount { get; set; }
        public int VoucherId { get; set; }

    }
}
