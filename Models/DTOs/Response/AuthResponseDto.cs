namespace Rolayther.Models.DTOs.Response
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        // false = l'utente non ha ancora cliccato il link di conferma ricevuto per email
        public bool EmailConfirmed { get; set; }
    }
}
