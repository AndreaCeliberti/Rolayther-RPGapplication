using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    public class GameRequestDto

    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        // Facoltativo alla creazione: la cover si carica come file subito dopo (POST /Game/{gameId}/Cover)
        public string? CoverImageUrl { get; set; }
    }
}
