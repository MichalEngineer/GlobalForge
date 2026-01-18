using System.ComponentModel.DataAnnotations;

namespace GlobalForge.Web.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Imię jest wymagane")]
        [StringLength(100, ErrorMessage = "Imię nie może być dłuższe niż 100 znaków")]
        [Display(Name = "Imię")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [StringLength(100, ErrorMessage = "Nazwisko nie może być dłuższe niż 100 znaków")]
        [Display(Name = "Nazwisko")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
        [Display(Name = "Adres email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź hasło")]
        [Compare("Password", ErrorMessage = "Hasła nie są identyczne")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Numer telefonu (opcjonalnie)")]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Adres (opcjonalnie)")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Miasto (opcjonalnie)")]
        public string? City { get; set; }

        [StringLength(20)]
        [Display(Name = "Kod pocztowy (opcjonalnie)")]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        [Display(Name = "Kraj (opcjonalnie)")]
        public string? Country { get; set; }
    }
}
