using ProjectTest1.Enums;
using System.ComponentModel.DataAnnotations;
namespace ProjectTest1.Models
{
    public class VoucherModel
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public VoucherType Type { get; set; }//kiểu voucher
        public DiscountType DiscountType { get; set; } //kiểu giảm giá
        public VoucherStatus Status { get; set; }   //trạng thái voucher
        public int? PointCost { get; set; } = 0;
        public float DiscountValue { get; set; } //giá trị giảm giá, ví dụ 100k hoặc 10%
        public float MinOrderValue { get; set; } = 0;
        public int UsageLimit { get; set; } = 0;
        public int UsedCount { get; set; } = 0;
        public int MaxPerUser { get; set; } = 0; //số lượng voucher tối đa mỗi người dùng có thể sử dụng

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
