using Microsoft.Extensions.Options;
using Rolayther.Services.Mail;
using Serilog;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rolayther.Services
{
    // Trasporto di PRODUZIONE: invia le email con l'API web di Brevo (POST https://api.brevo.com/v3/smtp/email).
    //
    // Perché l'API web e non l'SMTP: sul piano gratuito di Render le porte SMTP (25, 465, 587) sono bloccate,
    // mentre le chiamate HTTPS funzionano. Brevo risponde 201 con { "messageId": "..." } se accetta il messaggio.
    //
    // Si attiva con Email:Provider = "Brevo" (serve Email:Brevo:ApiKey e un mittente verificato su Brevo).
    public class BrevoEmailService : IEmailService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient _http;
        private readonly EmailOptions _options;

        public BrevoEmailService(HttpClient http, IOptions<EmailOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.Brevo.ApiKey))
                {
                    Log.Error("[EMAIL:Brevo] Chiave API mancante (Email:Brevo:ApiKey): email a {To} non inviata", message.ToEmail);
                    return false;
                }

                var payload = new BrevoPayload
                {
                    Sender = new BrevoAddress(_options.FromEmail.Trim(), CleanName(_options.FromName)),
                    To = new[] { new BrevoAddress(message.ToEmail.Trim(), CleanName(message.ToName)) },
                    ReplyTo = _options.CanReply ? new BrevoAddress(_options.ReplyToEmail!.Trim(), CleanName(_options.FromName)) : null,
                    Subject = CleanLine(message.Subject),
                    HtmlContent = message.HtmlBody,
                    TextContent = message.TextBody,
                    Tags = string.IsNullOrWhiteSpace(message.Tag) ? null : new[] { CleanLine(message.Tag!) }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, _options.Brevo.Endpoint)
                {
                    Content = JsonContent.Create(payload, options: JsonOptions)
                };
                request.Headers.Add("api-key", _options.Brevo.ApiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("[EMAIL:Brevo] Inviata a {To} ({Tag}) id={MessageId}", message.ToEmail, message.Tag, ReadMessageId(body));
                    return true;
                }

                // Errore tipico: 400 { "code": "invalid_parameter", "message": "Sender is not valid" } (mittente non verificato),
                // oppure 401 (chiave API errata). Si registra tutto TRANNE la chiave.
                Log.Error("[EMAIL:Brevo] Brevo ha rifiutato l'email a {To} ({Tag}): HTTP {Status} {Body}",
                    message.ToEmail, message.Tag, (int)response.StatusCode, Truncate(body, 500));
                return false;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Log.Error("[EMAIL:Brevo] Timeout nell'invio dell'email a {To} ({Tag})", message.ToEmail, message.Tag);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[EMAIL:Brevo] Errore nell'invio dell'email a {To} ({Tag})", message.ToEmail, message.Tag);
                return false;
            }
        }

        // Brevo accetta nomi fino a 70 caratteri. Via ritorni a capo, virgolette e parentesi angolari.
        internal static string? CleanName(string? name)
        {
            var cleaned = CleanLine(name);
            cleaned = cleaned.Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            if (cleaned.Length > 70) cleaned = cleaned[..70].Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }

        // Una sola riga, senza caratteri di controllo (difesa extra contro l'iniezione di intestazioni)
        internal static string CleanLine(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var chars = value.Select(c => char.IsControl(c) ? ' ' : c).ToArray();
            return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string? ReadMessageId(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("messageId", out var id) ? id.GetString() : null;
            }
            catch
            {
                return null;
            }
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "…";

        private sealed record BrevoAddress(string Email, string? Name);

        private sealed class BrevoPayload
        {
            public BrevoAddress Sender { get; set; } = null!;
            public BrevoAddress[] To { get; set; } = Array.Empty<BrevoAddress>();
            public BrevoAddress? ReplyTo { get; set; }
            public string Subject { get; set; } = string.Empty;
            public string HtmlContent { get; set; } = string.Empty;
            public string TextContent { get; set; } = string.Empty;
            public string[]? Tags { get; set; }
        }
    }
}
