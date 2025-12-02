namespace ProjectTest1.Models
{
    public class UserVoucherModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int VoucherId { get; set; }
        public bool Claimed { get; set; } = false;
        public DateTime? ClaimedAt { get; set; }
        public bool Used { get; set; } = false;
        public DateTime? UsedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }

        // Navigation
        public UserModel User { get; set; } = null!;
        public VoucherModel Voucher { get; set; } = null!;
    }
}
