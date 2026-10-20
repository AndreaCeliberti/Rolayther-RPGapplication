using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.DTOs.Request
{
    // Richiesta pubblica di diventare Master: i dati del Master più il consenso alla privacy.
    // MasterRequestDto resta com'è, perché lo usa anche la creazione fatta da un Admin.
    public class RegisterMasterRequestDto : MasterRequestDto, IPrivacyConsentRequest
    {
        public bool PrivacyConsent { get; set; }

        [MaxLength(20)]
        public string? PrivacyPolicyVersion { get; set; }
    }
}
