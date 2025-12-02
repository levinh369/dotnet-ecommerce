namespace ProjectTest1.Models
{
    public class PointHistoryModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int PointsChange { get; set; }    // + cộng, - trừ
        public string Reason { get; set; } = ""; // "purchase", "redeem", "bonus", "admin_adjust"
        public int? ReferenceId { get; set; }    // Id đơn hàng hoặc voucher liên quan
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public UserModel User { get; set; } = null!;
    }
}
