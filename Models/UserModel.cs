using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectTest1.Models
{
    public class UserModel
    {
        [Key]
        [Column("UserId")]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [Required]
        public string? FullName { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        public bool IsEmailConfirmed { get; set; } = false;

        public string? ResetToken { get; set; }
        public bool IsActive { get; set; } = true;
        public bool isDeleted { get; set; } = false;

        public DateTime? ResetTokenExpiry { get; set; }
        public int ? ProvinceId { get; set; }
        public int ? DistrictId { get; set; }
        public int ? WardId { get; set; }

        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ? AvatarUrl { get; set; }
    }
}
