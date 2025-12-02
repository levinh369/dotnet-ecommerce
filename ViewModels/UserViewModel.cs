namespace ProjectTest1.ViewModels
{
    public class UserViewModel
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? addressDetail { get; set; }
        public string? Address { get; set; }
        public string? Role { get; set; }
        public int ? ProvinceId { get; set; }
        public int ? DistrictId { get; set; }
        public int ? WardId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDelete { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? password { get; set; }    
        public string? AvatarUrl { get; set; }
        public IFormFile? MainImageFile { get; set; }
    }
}
