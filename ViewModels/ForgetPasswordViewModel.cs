using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.ViewModels
{
    public class ForgetPasswordViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
    }
}
