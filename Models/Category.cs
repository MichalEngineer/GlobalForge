using System.ComponentModel.DataAnnotations;

namespace GlobalForge.Web.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relacje
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
