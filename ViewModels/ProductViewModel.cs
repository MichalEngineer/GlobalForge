using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GlobalForge.Web.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa produktu jest wymagana")]
        [StringLength(200, ErrorMessage = "Nazwa nie może przekraczać 200 znaków")]
        [Display(Name = "Nazwa produktu")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opis jest wymagany")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Opis musi mieć od 10 do 2000 znaków")]
        [Display(Name = "Opis")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cena jest wymagana")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cena musi być większa niż 0")]
        [Display(Name = "Cena (PLN)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Ilość jest wymagana")]
        [Range(0, int.MaxValue, ErrorMessage = "Ilość nie może być ujemna")]
        [Display(Name = "Ilość")]
        public int Quantity { get; set; }

        [Display(Name = "Zdjęcie produktu")]
        public IFormFile? ImageFile { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Stan produktu jest wymagany")]
        [Display(Name = "Stan")]
        public string Condition { get; set; } = "New";

        [Required(ErrorMessage = "Kategoria jest wymagana")]
        [Display(Name = "Kategoria")]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
