namespace Rolayther.Models.DTOs.Request
{
    // Una registrazione fatta dalla persona stessa: deve dare il consenso al trattamento dei dati personali
    // dopo aver letto l'informativa privacy. La versione dell'informativa letta arriva dal sito.
    public interface IPrivacyConsentRequest
    {
        bool PrivacyConsent { get; set; }
        string? PrivacyPolicyVersion { get; set; }
    }
}
