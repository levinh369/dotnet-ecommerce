using ProjectTest1.Enums;
namespace ProjectTest1.ViewModels
{
    public class VoucherViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public VoucherType Type { get; set; }
        public DiscountType DiscountType { get; set; }
        public VoucherStatus Status { get; set; }
        public float DiscountValue { get; set; }
        public float MinOrderValue { get; set; }
        public int UsedCount { get; set; }
        public int UsageLimit { get; set; }
        public int MaxPerUser { get; set; } 
        public float DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int UserClaimedCount { get; set; }
        public int? PointCost { get; set; }
    }
}
