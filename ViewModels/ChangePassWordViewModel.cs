using System.ComponentModel.DataAnnotations;

namespace ProjectTest1.ViewModels
{
    public class ChangePassWordViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu"), MinLength(6)]
        [DataType(DataType.Password)]
        public string? NewPassWord { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("NewPassWord", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ComfirmPassWord{ get; set; }
    }
}
