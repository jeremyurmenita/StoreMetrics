using System.ComponentModel.DataAnnotations;

namespace StoreMetrics.ViewModels
{
    public class ProfileVm
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";

        [Display(Name = "Username")]
        public string Username { get; set; } = "";

        public string FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MiddleName))
                    return $"{FirstName} {LastName}";
                return $"{FirstName} {MiddleName} {LastName}";
            }
        }
    }
}