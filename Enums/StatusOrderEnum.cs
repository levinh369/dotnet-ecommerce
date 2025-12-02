namespace ProjectTest1.Enums
{
    public enum StatusOrderEnum
    {
        Pending = 1,     // Chờ xử lý
        Confirmed = 2,   // Đã xác nhận
        Shipping = 3,    // Đang giao hàng
        Delivered = 4,   // Hoàn thành
        Cancelled = 5    // Đã hủy
    }
    public enum StatusPaymentEnum
    {
        COD = 1,      // Chưa thanh toán
        VNPAY = 2,        // Đã thanh toán
        MOMO = 3     // Đã hoàn tiền
    }
    public enum NotificationType
    {
        Order=1,
        OrderConfirm=2,
        Comment=3,
        Reply=4,
        System=5
    }
    public enum VoucherStatus
    {
        Draft = 0,
        Active = 1,
        Inactive = 2,
        Expired = 3,
        Exhausted = 4,
        Deleted = 5
    }

    public enum VoucherType
    {
        Public = 0,
        Loyalty = 1
    }

    public enum DiscountType
    {
        Fixed = 0,
        Percent = 1
    }


}
