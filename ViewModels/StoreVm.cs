using System.ComponentModel.DataAnnotations;

namespace StoreMetrics.ViewModels
{
    public class StoreVm
    {
        public string? Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; } = "";

        [Display(Name = "Building Number")]
        public string? BuildingNumber { get; set; }

        [Required]
        public string StreetName { get; set; } = "";

        [Required]
        public string Brgy { get; set; } = "";

        [Required]
        public string City { get; set; } = "";

        [Required]
        public string Province { get; set; } = "";

        [Required, RegularExpression(@"^\d{4}$", ErrorMessage = "Postal Code must be 4 digits.")]
        public string PostalCode { get; set; } = "";

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "The Audit Schedule field is required.")]
        [DataType(DataType.Date)]
        public DateTime? AuditDate { get; set; }
    }
}
