using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    // Registrazione pubblica di un Player: i dati del Player più il consenso alla privacy.
    // PlayerRequestDto resta com'è, perché lo usa anche la creazione fatta da un Admin.
    public class RegisterPlayerRequestDto : PlayerRequestDto, IPrivacyConsentRequest
    {
        public bool PrivacyConsent { get; set; }

        [MaxLength(20)]
        public string? PrivacyPolicyVersion { get; set; }
    }
}
