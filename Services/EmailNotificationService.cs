using Microsoft.Extensions.Options;
using Rolayther.Models.Entities;
using Rolayther.Services.Mail;
using Serilog;

namespace Rolayther.Services
{
    // Punto unico per spedire le email di Rolayther. Chi ha bisogno di mandare una mail (registrazione, approvazione
    // dei Master, iscrizioni, nuove sessioni) chiama un metodo di questa classe: qui si sceglie il modello, si
    // costruiscono i link e si passa il messaggio al trasporto (Brevo in produzione, file HTML in sviluppo).
    //
    // Ogni metodo restituisce true se il messaggio è stato accettato dal trasporto, e NON lancia mai eccezioni.
    public class EmailNotificationService
    {
        private readonly IEmailService _transport;
        private readonly EmailConfirmationTokenService _tokens;
        private readonly EmailContext _context;

        public EmailNotificationService(IEmailService transport, EmailConfirmationTokenService tokens, IOptions<EmailOptions> options)
        {
            _transport = transport;
            _tokens = tokens;
            _context = EmailContext.From(options.Value);
        }

        // Link che l'utente clicca per confermare l'indirizzo: porta alla pagina /confirm-email del frontend
        public string BuildConfirmationUrl(ApplicationUser user)
        {
            var token = _tokens.CreateToken(user.Id, user.Email ?? string.Empty);
            return _context.Url("/confirm-email?token=" + Uri.EscapeDataString(token));
        }

        // 1. Conferma registrazione (Player o richiesta Master)
        public Task<bool> SendEmailConfirmationAsync(ApplicationUser user, bool isMasterRequest, CancellationToken ct = default) =>
            SendAsync("confirm-email", user.Email, FullName(user.Name, user.Surname), ct,
                () => EmailTemplates.ConfirmEmail(_context, user.Name, BuildConfirmationUrl(user), isMasterRequest));

        // 2. Richiesta Master approvata (con messaggio facoltativo dell'amministratore)
        public Task<bool> SendMasterApprovedAsync(ApplicationUser user, string firstName, string? adminMessage, CancellationToken ct = default) =>
            SendAsync("master-approved", user.Email, FullName(user.Name, user.Surname), ct, () =>
            {
                // Se l'email non è ancora confermata il pulsante dell'email è il link di conferma:
                // senza, il nuovo Master non potrebbe creare sessioni
                var needsConfirmation = !user.EmailConfirmed;
                var actionUrl = needsConfirmation ? BuildConfirmationUrl(user) : _context.Url("/?login=1");
                return EmailTemplates.MasterApproved(_context, firstName, adminMessage, actionUrl, needsConfirmation);
            });

        // 3. Richiesta Master rifiutata (con la spiegazione)
        public Task<bool> SendMasterRejectedAsync(string toEmail, string firstName, string reason, CancellationToken ct = default) =>
            SendAsync("master-rejected", toEmail, firstName, ct,
                () => EmailTemplates.MasterRejected(_context, firstName, reason));

        // 4. Iscrizione confermata (al giocatore)
        public Task<bool> SendSessionJoinedAsync(SessionEmailInfo session, PlayerEmailInfo player, CancellationToken ct = default) =>
            SendAsync("session-joined", player.Email, FullName(player.Name, player.Surname), ct,
                () => EmailTemplates.SessionJoined(_context, player.Name, session));

        // 5. Nuovo giocatore iscritto (al Master): del giocatore si mostra solo il nickname, come nel resto del sito
        public Task<bool> SendSessionNewPlayerAsync(SessionEmailInfo session, PlayerEmailInfo player, CancellationToken ct = default) =>
            SendAsync("session-new-player", session.MasterEmail, session.MasterName, ct,
                () => EmailTemplates.SessionNewPlayer(_context, session.MasterName, player.NickName, session));

        // 6. Sessione creata (al Master)
        public Task<bool> SendSessionCreatedAsync(SessionEmailInfo session, bool createdByAdmin, CancellationToken ct = default) =>
            SendAsync("session-created", session.MasterEmail, session.MasterName, ct,
                () => EmailTemplates.SessionCreated(_context, session.MasterName, session, createdByAdmin));

        private async Task<bool> SendAsync(string tag, string? toEmail, string? toName, CancellationToken ct, Func<RenderedEmail> render)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Log.Warning("[EMAIL] {Tag}: destinatario senza indirizzo email, messaggio non inviato", tag);
                    return false;
                }

                var email = render();
                return await _transport.SendAsync(
                    new EmailMessage(toEmail.Trim(), toName, email.Subject, email.Html, email.Text, tag), ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EMAIL] {Tag}: errore nella preparazione dell'email per {To}", tag, toEmail);
                return false;
            }
        }

        private static string FullName(string? name, string? surname) =>
            $"{name} {surname}".Trim();
    }
}
