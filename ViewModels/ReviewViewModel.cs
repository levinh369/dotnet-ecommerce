namespace ProjectTest1.ViewModels
{
    public class ReviewViewModel
    {
        public int Id { get; set; }
        public string? Comment { get; set; }
        public int Rating { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrderDetailId { get; set; }
        public int ProductVariantId { get; set; }
        public string? SellerReply { get; set; }
        public DateTime? SellerReplyAt { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public bool IsVisible { get; set; }
        public string? avatarUrl { get; set; }
    }
    public class ReviewListViewModel
    {
        public int? OrderDetailId { get; set; }
        public int? ProductVariantId { get; set; }
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
    }

}
