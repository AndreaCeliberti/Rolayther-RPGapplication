using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Rolayther.Services.Mail
{
    // Impostazioni del sistema di posta. Si leggono dalla sezione "Email" di appsettings oppure dalle
    // variabili d'ambiente (su Render: Email__Provider, Email__Brevo__ApiKey, Email__FromEmail, ...).
    //
    // Ogni valore ha un default pensato per lo sviluppo: senza configurare nulla le email NON partono
    // davvero, vengono salvate come file HTML in App_Data/emails (da aprire nel browser) e scritte nel log.
    public class EmailOptions
    {
        public const string SectionName = "Email";

        // "Console" (sviluppo: salva l'anteprima su disco) oppure "Brevo" (invio reale via API web)
        public string Provider { get; set; } = "Console";

        // Mittente. Con Brevo deve essere un mittente/dominio verificato nel pannello Brevo.
        public string FromEmail { get; set; } = "no-reply@rolayther.local";
        public string FromName { get; set; } = "Rolayther";

        // Casella che riceve le risposte degli utenti ("rispondi a questa email"). Vuoto = nessun Reply-To:
        // in quel caso le email NON invitano a rispondere.
        public string? ReplyToEmail { get; set; }

        // Indirizzo del sito (frontend): serve a costruire i link nelle email, es. https://rolayther.onrender.com
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

        // Dove stanno le immagini delle email (cartella public/email del frontend). Vuoto = FrontendBaseUrl.
        public string? AssetsBaseUrl { get; set; }

        // Per quanti giorni resta valido il link di conferma email
        public int ConfirmationTokenDays { get; set; } = 3;

        // Se true, chi non ha confermato l'email non può iscriversi alle sessioni né crearne (l'Admin è escluso)
        public bool RequireConfirmedEmail { get; set; } = true;

        // Antispam sul "reinvia conferma": pausa minima tra due invii e massimo invii all'ora per indirizzo
        public int ResendCooldownSeconds { get; set; } = 60;
        public int ResendMaxPerHour { get; set; } = 5;

        // Cartella delle anteprime (solo provider Console). Relativa alla cartella del progetto.
        public string PreviewFolder { get; set; } = "App_Data/emails";

        public BrevoOptions Brevo { get; set; } = new();

        public bool IsBrevo => string.Equals(Provider?.Trim(), "Brevo", StringComparison.OrdinalIgnoreCase);
        public bool IsConsole => string.Equals(Provider?.Trim(), "Console", StringComparison.OrdinalIgnoreCase);

        // Indirizzi senza slash finale, pronti per essere concatenati ("/confirm-email", "/email/header.jpg", ...)
        public string FrontendUrl => TrimUrl(FrontendBaseUrl);
        public string AssetsUrl => string.IsNullOrWhiteSpace(AssetsBaseUrl) ? FrontendUrl : TrimUrl(AssetsBaseUrl);

        public bool CanReply => !string.IsNullOrWhiteSpace(ReplyToEmail);

        private static string TrimUrl(string? url) => (url ?? string.Empty).Trim().TrimEnd('/');
    }

    public class BrevoOptions
    {
        // Chiave API v3 di Brevo (Impostazioni > SMTP e API > Chiavi API). SEGRETA: mai nel repository,
        // solo in variabile d'ambiente (Email__Brevo__ApiKey) o nei secrets locali.
        public string ApiKey { get; set; } = string.Empty;

        public string Endpoint { get; set; } = "https://api.brevo.com/v3/smtp/email";
    }

    // Controlla le impostazioni all'avvio: meglio un errore chiaro subito che email che non partono in silenzio.
    public class EmailOptionsValidator : IValidateOptions<EmailOptions>
    {
        public ValidateOptionsResult Validate(string? name, EmailOptions options)
        {
            var errors = new List<string>();

            if (!options.IsBrevo && !options.IsConsole)
                errors.Add($"Email:Provider \"{options.Provider}\" non valido: usa \"Console\" oppure \"Brevo\".");

            if (!IsHttpUrl(options.FrontendBaseUrl))
                errors.Add("Email:FrontendBaseUrl deve essere un indirizzo completo http(s), ad esempio https://rolayther.onrender.com");

            if (!string.IsNullOrWhiteSpace(options.AssetsBaseUrl) && !IsHttpUrl(options.AssetsBaseUrl))
                errors.Add("Email:AssetsBaseUrl, se indicato, deve essere un indirizzo completo http(s).");

            if (options.ConfirmationTokenDays is < 1 or > 30)
                errors.Add("Email:ConfirmationTokenDays deve essere tra 1 e 30.");

            if (options.ResendCooldownSeconds < 0 || options.ResendMaxPerHour < 1)
                errors.Add("Email:ResendCooldownSeconds non può essere negativo e Email:ResendMaxPerHour deve essere almeno 1.");

            if (!IsEmailAddress(options.FromEmail))
                errors.Add("Email:FromEmail non è un indirizzo email valido.");

            if (!string.IsNullOrWhiteSpace(options.ReplyToEmail) && !IsEmailAddress(options.ReplyToEmail))
                errors.Add("Email:ReplyToEmail non è un indirizzo email valido.");

            if (options.IsBrevo)
            {
                if (string.IsNullOrWhiteSpace(options.Brevo?.ApiKey))
                    errors.Add("Email:Brevo:ApiKey mancante: con Email:Provider = \"Brevo\" serve la chiave API (variabile d'ambiente Email__Brevo__ApiKey).");

                if (!IsHttpUrl(options.Brevo?.Endpoint))
                    errors.Add("Email:Brevo:Endpoint non è un indirizzo valido.");
            }

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }

        private static bool IsHttpUrl(string? value) =>
            Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool IsEmailAddress(string? value) =>
            !string.IsNullOrWhiteSpace(value)
            && MailAddress.TryCreate(value.Trim(), out var address)
            && string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
