namespace Rolayther.Services
{
    // Trasporto di basso livello: spedisce un messaggio GIÀ PRONTO (HTML + testo semplice).
    // Cosa scrivere e come impaginarlo lo decide EmailNotificationService con i modelli in Services/Mail.
    //
    // Regola importante: SendAsync NON lancia mai eccezioni. Restituisce true se il messaggio è stato
    // accettato dal servizio di posta, false altrimenti (l'errore viene scritto nel log): un problema
    // di posta non deve mai far fallire una registrazione, un'approvazione o un'iscrizione.
    //
    // Implementazioni: ConsoleEmailService (sviluppo, salva l'anteprima su file) e BrevoEmailService (invio reale).
    public interface IEmailService
    {
        Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }

    // Tag: etichetta breve del tipo di email (es. "session-joined"), usata per i log e per le statistiche di Brevo.
    public sealed record EmailMessage(
        string ToEmail,
        string? ToName,
        string Subject,
        string HtmlBody,
        string TextBody,
        string? Tag = null);
}
