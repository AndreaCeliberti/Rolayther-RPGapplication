using System.Data.Common;

namespace Rolayther.Helpers
{
    // Aiuti per la stringa di connessione a PostgreSQL (Supabase).
    public static class ConnectionStringHelper
    {
        // Supabase mostra l'indirizzo del database in forma di URI:
        //     postgresql://postgres.abcdefghijkl:LA-TUA-PASSWORD@aws-0-eu-central-1.pooler.supabase.com:5432/postgres
        // ma il driver Npgsql vuole la forma "chiave=valore;chiave=valore". Qui si accettano entrambe:
        // la seconda passa com'è, la prima viene convertita (con SSL obbligatorio e un massimo di connessioni
        // contenuto, perché il piano gratuito di Supabase ne concede poche).
        public static string Normalize(string connectionString)
        {
            var text = (connectionString ?? string.Empty).Trim();

            // Errore tipico: copiare l'indirizzo dal pannello di Supabase e dimenticare di sostituire il segnaposto della password
            if (text.Contains("YOUR-PASSWORD", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException(
                    "Nella stringa di connessione è rimasto il segnaposto [YOUR-PASSWORD]: sostituiscilo (parentesi quadre comprese) " +
                    "con la password del database di Supabase (Project Settings > Database > Reset database password).");
            }

            if (!text.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !text.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }

            // Messaggi d'errore senza mai il testo della stringa: dentro c'è la password
            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
            {
                throw new FormatException(
                    "La stringa di connessione è in formato postgresql://... ma non è valida. Controlla di aver sostituito [YOUR-PASSWORD] " +
                    "con la password vera e che la password non contenga caratteri speciali come @ : / # ? (meglio solo lettere e cifre), " +
                    "oppure usa la forma Host=...;Port=...;Database=...;Username=...;Password=...");
            }

            var userInfo = uri.UserInfo;
            var colon = userInfo.IndexOf(':');
            var user = Uri.UnescapeDataString(colon < 0 ? userInfo : userInfo[..colon]);
            var password = colon < 0 ? string.Empty : Uri.UnescapeDataString(userInfo[(colon + 1)..]);
            var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));

            var builder = new DbConnectionStringBuilder
            {
                ["Host"] = uri.Host,
                ["Port"] = uri.Port > 0 ? uri.Port : 5432,
                ["Database"] = database.Length > 0 ? database : "postgres",
                ["Username"] = user,
                ["Password"] = password,
                ["SSL Mode"] = "Require",
                ["Trust Server Certificate"] = true,
                ["Maximum Pool Size"] = 10
            };

            return builder.ConnectionString;
        }

        // Descrizione sicura per il log, "host:porta/database", MAI utente e password
        public static string Describe(string connectionString)
        {
            try
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

                string Get(params string[] keys)
                {
                    foreach (var key in keys)
                    {
                        if (builder.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                            return value!.ToString()!;
                    }

                    return string.Empty;
                }

                var host = Get("Host", "Server");
                var port = Get("Port");
                var database = Get("Database", "Initial Catalog");

                return host.Length == 0
                    ? "(indirizzo non riconosciuto)"
                    : $"{host}{(port.Length > 0 ? ":" + port : string.Empty)}/{database}";
            }
            catch
            {
                return "(indirizzo non riconosciuto)";
            }
        }
    }
}
