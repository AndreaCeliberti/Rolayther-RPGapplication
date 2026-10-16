using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    public class GenreRequestDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        // Facoltativo alla creazione: l'immagine si carica come file subito dopo (POST /Genre/{genreId}/Image)
        public string? ImageUrl { get; set; }
        
    }
}
