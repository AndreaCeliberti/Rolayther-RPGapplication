using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Rolayther.Services.Mail
{
    public enum EmailTokenStatus
    {
        Valid,
        Invalid,
        Expired
    }

    public readonly record struct EmailTokenResult(EmailTokenStatus Status, string? UserId, string? EmailFingerprint)
    {
        public static EmailTokenResult Invalid => new(EmailTokenStatus.Invalid, null, null);
    }

    // Token del link di conferma email. È "senza stato": non si salva nulla nel database, il token contiene
    // tutto ciò che serve ed è firmato (HMAC-SHA256), quindi non si può falsificare né modificare.
    //
    //   token = base64url( "idUtente|scadenzaUnix|impronta(email)" ) + "." + base64url( firma )
    //
    // Perché non i token di Identity (GenerateEmailConfirmationTokenAsync): usano le chiavi di DataProtection,
    // che sul piano gratuito di Render si perdono a ogni riavvio del container, e i link già spediti
    // smetterebbero di funzionare. Qui la chiave deriva da Jwt:Key, che è già configurata e resta stabile.
    //
    // L'impronta dell'email lega il token all'indirizzo: se l'utente cambia email, i vecchi link non valgono più.
    public sealed class EmailConfirmationTokenService
    {
        private const int MaxTokenLength = 512;

        private readonly byte[] _signingKey;
        private readonly TimeSpan _lifetime;
        private readonly Func<DateTimeOffset> _clock;

        public EmailConfirmationTokenService(IConfiguration configuration, IOptions<EmailOptions> options)
            : this(configuration["Jwt:Key"], options.Value.ConfirmationTokenDays, () => DateTimeOffset.UtcNow)
        {
        }

        // Usato anche dai test, per controllare l'orologio
        internal EmailConfirmationTokenService(string? secret, int lifetimeDays, Func<DateTimeOffset> clock)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Jwt:Key mancante: serve per firmare i link di conferma email.");

            // Sottochiave dedicata: la chiave dei JWT non viene mai usata direttamente per altri scopi
            _signingKey = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes("rolayther/email-confirmation/v1"));

            _lifetime = TimeSpan.FromDays(Math.Max(1, lifetimeDays));
            _clock = clock;
        }

        public string CreateToken(string userId, string email)
        {
            var expires = _clock().Add(_lifetime).ToUnixTimeSeconds();
            var payload = Encoding.UTF8.GetBytes(string.Join('|', userId, expires.ToString(CultureInfo.InvariantCulture), Fingerprint(email)));
            var signature = HMACSHA256.HashData(_signingKey, payload);
            return WebEncoders.Base64UrlEncode(payload) + "." + WebEncoders.Base64UrlEncode(signature);
        }

        public EmailTokenResult Validate(string? token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
                return EmailTokenResult.Invalid;

            var parts = token.Trim().Split('.');
            if (parts.Length != 2)
                return EmailTokenResult.Invalid;

            byte[] payload, signature;
            try
            {
                payload = WebEncoders.Base64UrlDecode(parts[0]);
                signature = WebEncoders.Base64UrlDecode(parts[1]);
            }
            catch (FormatException)
            {
                return EmailTokenResult.Invalid;
            }

            // 1) La firma si controlla PRIMA di guardare il contenuto, con confronto a tempo costante
            var expected = HMACSHA256.HashData(_signingKey, payload);
            if (!CryptographicOperations.FixedTimeEquals(expected, signature))
                return EmailTokenResult.Invalid;

            // 2) Contenuto
            string[] fields;
            try
            {
                fields = Encoding.UTF8.GetString(payload).Split('|');
            }
            catch (Exception)
            {
                return EmailTokenResult.Invalid;
            }

            if (fields.Length != 3
                || fields[0].Length == 0
                || !long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresUnix))
                return EmailTokenResult.Invalid;

            // 3) Scadenza (si dichiara "scaduto" solo un token autentico: chi non ha la chiave non impara nulla)
            if (_clock().ToUnixTimeSeconds() > expiresUnix)
                return new EmailTokenResult(EmailTokenStatus.Expired, fields[0], fields[2]);

            return new EmailTokenResult(EmailTokenStatus.Valid, fields[0], fields[2]);
        }

        // Impronta breve e stabile dell'indirizzo (maiuscole/minuscole e spazi non contano)
        public static string Fingerprint(string email)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes((email ?? string.Empty).Trim().ToLowerInvariant()));
            return Convert.ToHexString(hash, 0, 8);
        }
    }
}
