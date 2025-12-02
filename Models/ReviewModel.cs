using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.Models
{
    public class ReviewModel
    {
        public int Id { get; set; }

        public int OrderDetailId { get; set; }
        public OrderDetailModel? OrderDetail { get; set; }
        // Liên kết trực tiếp tới ProductVariant
        public int ProductVariantId { get; set; }
        public ProductVariantModel? ProductVariant { get; set; }

        // Người dùng
        public Guid UserId { get; set; }
        public UserModel? User { get; set; }

        // Nội dung review
        [Required]
        [MaxLength(1000)]
        public string? Comment { get; set; }

        // Rating (1–5 sao)
        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? SellerReply { get; set; }
        public DateTime? SellerReplyAt { get; set; }
        public bool IsVisible { get; set; } = true;

    }
}
