using Serilog;

namespace Rolayther.Services
{
    // Implementazione provvisoria: registra l'evento nei log invece di inviare una email reale.
    // Il resto del codice (MasterService, futuri servizi) dipende solo da IEmailService,
    // quindi per passare a Brevo basterà creare una nuova classe che la implementa
    // e cambiare la registrazione in Program.cs, senza toccare altro.
    public class ConsoleEmailService : IEmailService
    {
        public Task SendMasterApprovedEmailAsync(string toEmail, string masterName)
        {
            Log.Information("[EMAIL STUB] Richiesta Master approvata per {Name} -> invierei conferma a {Email}", masterName, toEmail);
            return Task.CompletedTask;
        }

        public Task SendMasterRejectedEmailAsync(string toEmail, string masterName, string? reason)
        {
            Log.Information("[EMAIL STUB] Richiesta Master rifiutata per {Name} -> invierei notifica a {Email} (motivo: {Reason})", masterName, toEmail, reason ?? "non specificato");
            return Task.CompletedTask;
        }
    }
}
