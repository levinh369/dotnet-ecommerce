using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.ViewModels
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required, Compare("Password")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
