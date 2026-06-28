using System.ComponentModel.DataAnnotations;

namespace BloodBank.Models
{
    public class RegisterViewModel
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName  { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? NationalId  { get; set; }
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-25);
        public string? Gender      { get; set; }
        public string? BloodType   { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Governorate { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
