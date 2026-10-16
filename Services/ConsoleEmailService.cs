using Microsoft.Extensions.Options;
using Rolayther.Services.Mail;
using Serilog;
using System.Globalization;
using System.Text;

namespace Rolayther.Services
{
    // Trasporto per lo SVILUPPO: non spedisce nulla. Salva ogni email come file HTML (più una copia in testo
    // semplice) in App_Data/emails: basta fare doppio clic sul file per vederla nel browser, con il suo
    // aspetto reale. Scrive anche una riga nel log con il testo, che contiene i link (es. quello di conferma).
    //
    // Si usa con Email:Provider = "Console" (è il valore predefinito).
    public class ConsoleEmailService : IEmailService
    {
        private readonly string _folder;

        public ConsoleEmailService(IWebHostEnvironment env, IOptions<EmailOptions> options)
        {
            var configured = options.Value.PreviewFolder;
            _folder = Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
        }

        public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                Directory.CreateDirectory(_folder);

                // Il nome file parte da dati controllabili da un utente (l'indirizzo email): lo si riduce a lettere,
                // numeri e trattini, così non può mai uscire dalla cartella delle anteprime.
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
                var unique = Guid.NewGuid().ToString("N")[..6];
                var baseName = $"{stamp}_{Slug(message.Tag ?? "mail")}_{Slug(message.ToEmail)}_{unique}";

                var htmlPath = Path.Combine(_folder, baseName + ".html");
                var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                await File.WriteAllTextAsync(htmlPath, message.HtmlBody, utf8, cancellationToken);
                await File.WriteAllTextAsync(Path.Combine(_folder, baseName + ".txt"), message.TextBody, utf8, cancellationToken);

                Log.Information("[EMAIL:Console] A: {To} | Oggetto: {Subject} | Anteprima: {Path}", message.ToEmail, message.Subject, htmlPath);
                Log.Information("[EMAIL:Console] Testo dell'email a {To}:{NewLine}{Text}", message.ToEmail, Environment.NewLine, message.TextBody);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EMAIL:Console] Impossibile salvare l'anteprima dell'email per {To}", message.ToEmail);
                return false;
            }
        }

        private static string Slug(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value.ToLowerInvariant())
            {
                if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
                    sb.Append(ch);
                else if (sb.Length > 0 && sb[^1] != '-')
                    sb.Append('-');
            }

            var slug = sb.ToString().Trim('-');
            if (slug.Length > 40) slug = slug[..40].Trim('-');
            return slug.Length == 0 ? "x" : slug;
        }
    }
}
