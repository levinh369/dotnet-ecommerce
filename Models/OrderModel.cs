using ProjectTest1.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.Models
{
    [Table("Orders")]
    public class OrderModel
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public UserModel? User { get; set; }
        [Required]
        public String? Email { get; set; }
        [Required]
        public String? Phone { get; set; }
       
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ShippedDate { get; set; }

        [Required, StringLength(255)]
        public string? ShippingAddress { get; set; }

        [Range(0, Double.MaxValue, ErrorMessage = "TotalAmount must be >= 0")]
        public float? TotalAmount { get; set; }
        public int? VoucherId { get; set; }
        public float DiscountValue { get; set; } = 0;
        public float FinalAmount { get; set; }
        public VoucherModel? Voucher { get; set; }

        [Required] // bắt buộc có giá trị (enum không thể null trừ khi dùng nullable)
        public StatusOrderEnum Status { get; set; }
        public StatusPaymentEnum PaymentStatus { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderDetailModel> OrderDetails { get; set; } = new List<OrderDetailModel>();

        public ICollection<OrderNotificationModel>? Notifications { get; set; }
    }
}
