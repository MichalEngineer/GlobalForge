using System.ComponentModel.DataAnnotations;

namespace GlobalForge.Web.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Imię jest wymagane")]
        [StringLength(100)]
        [Display(Name = "Imię")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [StringLength(100)]
        [Display(Name = "Nazwisko")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
        [Display(Name = "Adres email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Numer telefonu")]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Adres")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Miasto")]
        public string? City { get; set; }

        [StringLength(20)]
        [Display(Name = "Kod pocztowy")]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        [Display(Name = "Kraj")]
        public string? Country { get; set; }
    }
}
