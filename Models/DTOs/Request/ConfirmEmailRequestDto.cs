using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    public class ConfirmEmailRequestDto
    {
        // Il token è il pezzo "?token=..." del link ricevuto per email
        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;
    }

    public class ResendConfirmationRequestDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}
