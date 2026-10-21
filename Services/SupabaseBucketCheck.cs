using Microsoft.Extensions.Options;
using Serilog;
using System.Net;
using System.Text.Json;

namespace Rolayther.Services
{
    // Controllo "diagnostico" all'avvio, solo con Storage:Provider = "Supabase": guarda se i due bucket esistono e se
    // hanno la visibilità giusta, e scrive nel log cosa fare se qualcosa non torna. Non crea e non modifica nulla,
    // e non blocca mai l'avvio: il suo unico scopo è evitare di scoprire a mano, dalle immagini rotte, un bucket
    // dimenticato o creato con la visibilità sbagliata.
    public sealed class SupabaseBucketCheck : BackgroundService
    {
        public const string HttpClientName = "SupabaseStorageCheck";

        private readonly IHttpClientFactory _httpFactory;
        private readonly string _baseUrl;

        public SupabaseBucketCheck(IHttpClientFactory httpFactory, IOptions<StorageOptions> options)
        {
            _httpFactory = httpFactory;
            _baseUrl = options.Value.Supabase.BaseUrl;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Mai far cadere l'applicazione per un controllo informativo
            try
            {
                var http = _httpFactory.CreateClient(HttpClientName);

                await CheckAsync(http, SupabaseFileStorageService.PublicBucket, expectPublic: true, stoppingToken);
                await CheckAsync(http, SupabaseFileStorageService.PrivateBucket, expectPublic: false, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // arresto dell'applicazione: niente da segnalare
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[STORAGE:Supabase] Controllo dei bucket non riuscito (non è un problema se l'archivio funziona)");
            }
        }

        private async Task CheckAsync(HttpClient http, string bucket, bool expectPublic, CancellationToken ct)
        {
            var expected = expectPublic ? "PUBBLICO (Public bucket attivo)" : "PRIVATO (Public bucket spento)";

            using var response = await http.GetAsync($"{_baseUrl}/storage/v1/bucket/{Uri.EscapeDataString(bucket)}", ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var isPublic = ReadPublicFlag(body);

                if (isPublic is null)
                    Log.Information("[STORAGE:Supabase] Bucket {Bucket} trovato", bucket);
                else if (isPublic.Value != expectPublic)
                    Log.Warning("[STORAGE:Supabase] Il bucket {Bucket} deve essere {Expected} ma su Supabase è {Actual}: correggilo in Storage > {Bucket} > Edit bucket",
                        bucket, expected, isPublic.Value ? "pubblico" : "privato", bucket);
                else
                    Log.Information("[STORAGE:Supabase] Bucket {Bucket} ok ({Visibility})", bucket, expectPublic ? "pubblico" : "privato");

                return;
            }

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                && body.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("[STORAGE:Supabase] Il bucket {Bucket} non esiste: crealo in Supabase > Storage > New bucket, con visibilità {Expected}",
                    bucket, expected);
                return;
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                Log.Warning("[STORAGE:Supabase] Supabase non accetta la chiave segreta (HTTP {Status}): controlla Storage__Supabase__SecretKey e Storage__Supabase__Url",
                    (int)response.StatusCode);
                return;
            }

            Log.Warning("[STORAGE:Supabase] Controllo del bucket {Bucket}: risposta inattesa HTTP {Status} {Body}",
                bucket, (int)response.StatusCode, body.Length <= 200 ? body : body[..200] + "…");
        }

        // La risposta di Supabase è un oggetto JSON con { "id": "uploads", "public": true, ... }
        internal static bool? ReadPublicFlag(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("public", out var flag)
                    && flag.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return flag.GetBoolean();
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }
}
