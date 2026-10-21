using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Rolayther.Services
{
    // Impostazioni dell'archivio dei file (immagini di profilo, copertine, materiale delle sessioni).
    // Si leggono dalla sezione "Storage" di appsettings oppure dalle variabili d'ambiente
    // (su Render: Storage__Provider, Storage__Supabase__Url, Storage__Supabase__SecretKey).
    //
    // Predefinito: "Local" = file su disco, comodo in sviluppo. Online serve "Supabase": il disco di Render gratuito
    // si svuota a ogni riavvio, quindi con "Local" le immagini caricate sparirebbero.
    public class StorageOptions
    {
        public const string SectionName = "Storage";

        // "Local" (sviluppo) oppure "Supabase" (online)
        public string Provider { get; set; } = "Local";

        public SupabaseStorageOptions Supabase { get; set; } = new();

        public bool IsSupabase => string.Equals(Provider?.Trim(), "Supabase", StringComparison.OrdinalIgnoreCase);
        public bool IsLocal => string.Equals(Provider?.Trim(), "Local", StringComparison.OrdinalIgnoreCase);
    }

    public class SupabaseStorageOptions
    {
        // Indirizzo del progetto Supabase, es. https://abcdefghijkl.supabase.co (senza percorso).
        public string Url { get; set; } = string.Empty;

        // Chiave SEGRETA del progetto (Supabase > Project Settings > API Keys): quella che inizia con "sb_secret_"
        // oppure, nei progetti più vecchi, la "service_role". Permette di fare tutto sui file: SEGRETA, mai nel
        // repository e mai nel frontend, solo variabile d'ambiente (Storage__Supabase__SecretKey) o User Secrets.
        public string SecretKey { get; set; } = string.Empty;

        // Indirizzo senza spazi e senza "/" finale, pronto per essere concatenato
        public string BaseUrl => (Url ?? string.Empty).Trim().TrimEnd('/');
    }

    // Controlla le impostazioni all'avvio: meglio un errore chiaro subito che immagini che non si caricano.
    public class StorageOptionsValidator : IValidateOptions<StorageOptions>
    {
        public ValidateOptionsResult Validate(string? name, StorageOptions options)
        {
            var errors = new List<string>();

            if (!options.IsLocal && !options.IsSupabase)
                errors.Add($"Storage:Provider \"{options.Provider}\" non valido: usa \"Local\" oppure \"Supabase\".");

            if (options.IsSupabase)
            {
                var supabase = options.Supabase ?? new SupabaseStorageOptions();

                if (!Uri.TryCreate(supabase.BaseUrl, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                {
                    errors.Add("Storage:Supabase:Url mancante o non valido: serve l'indirizzo del progetto, per esempio https://abcdefghijkl.supabase.co (variabile d'ambiente Storage__Supabase__Url).");
                }
                else if (uri.AbsolutePath.Length > 1)
                {
                    errors.Add("Storage:Supabase:Url deve essere solo l'indirizzo del progetto (https://abcdefghijkl.supabase.co), senza percorso dopo il dominio.");
                }

                var key = supabase.SecretKey?.Trim() ?? string.Empty;
                if (key.Length == 0)
                {
                    errors.Add("Storage:Supabase:SecretKey mancante: serve la chiave segreta del progetto Supabase (variabile d'ambiente Storage__Supabase__SecretKey).");
                }
                else if (key.StartsWith("sb_publishable_", StringComparison.Ordinal) || IsAnonJwt(key))
                {
                    // Errore tipico: incollare la chiave pubblica (quella adatta al browser). Con quella i caricamenti vengono rifiutati.
                    errors.Add("Storage:Supabase:SecretKey è la chiave PUBBLICA: serve quella segreta (inizia con sb_secret_, oppure è la service_role nei progetti più vecchi).");
                }
            }

            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }

        // Le vecchie chiavi di Supabase sono JWT: il ruolo ("anon" o "service_role") sta nella parte centrale.
        // Se non riusciamo a leggerlo, nel dubbio non blocchiamo nulla.
        internal static bool IsAnonJwt(string key)
        {
            try
            {
                var parts = key.Split('.');
                if (parts.Length != 3)
                    return false;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                return doc.RootElement.TryGetProperty("role", out var role)
                       && string.Equals(role.GetString(), "anon", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
