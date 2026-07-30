using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace StoreMetrics.ViewModels
{
    public class RegisterVm : IValidatableObject
    {
        [Required]
        public string FirstName { get; set; } = "";

        // ✅ New field
        public string? MiddleName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be 11 digits.")]

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string Username { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Compare(nameof(Password)), DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            var passwordPattern = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*(),.?""':{}|<>]).{8,}$";

            if (!Regex.IsMatch(Password ?? "", passwordPattern))
            {
                errors.Add(new ValidationResult(
                    "Password must be at least 8 characters long and include an uppercase letter, lowercase letter, number, and special character.",
                    new[] { nameof(Password) }));
            }

            return errors;
        }
    }
}
