using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // Consenso al trattamento dei dati personali, dato spuntando la casella in fase di iscrizione:
        // quando (UTC) e con quale versione dell'informativa privacy. Restano vuoti per gli account creati
        // da un Admin e per quelli iscritti prima che la casella esistesse.
        public DateTime? PrivacyConsentAt { get; set; }

        [MaxLength(20)]
        public string? PrivacyPolicyVersion { get; set; }
    }
}
