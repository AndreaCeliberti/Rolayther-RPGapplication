using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rolayther.Exceptions;
using Rolayther.Models.Enums;
using Serilog;
using System.Net;
using System.Net.Http.Headers;

namespace Rolayther.Services
{
    // Archivio dei file di PRODUZIONE: Supabase Storage, tramite la sua API web (nessun pacchetto in più).
    //
    // Perché non il disco: su Render (piano gratuito) il filesystem si svuota a ogni riavvio e a ogni nuova versione,
    // quindi le immagini e i file caricati sparirebbero. Supabase Storage li conserva.
    //
    // Due "bucket" (cartelle di primo livello su Supabase), da creare una volta sola dal pannello (vedi docs/DEPLOY.md):
    //   - "uploads"          PUBBLICO: avatar, copertine, loghi. Chiunque abbia l'indirizzo vede l'immagine (serve per <img src>).
    //   - "private-uploads"  PRIVATO: materiale delle sessioni. Non c'è nessun indirizzo pubblico: il backend lo scarica
    //                        con la chiave segreta e lo gira all'utente solo dopo aver controllato i permessi.
    //
    // Il nome "uploads" non è casuale: i servizi riconoscono i NOSTRI file dalla parte "/uploads/" dell'indirizzo
    // (ExtractStorageKeyIfLocal) per eliminare la vecchia immagine quando ne arriva una nuova. Con questo nome
    // l'indirizzo pubblico di Supabase contiene proprio "/uploads/" seguito dalla chiave del file, come in locale.
    //
    // Si attiva con Storage:Provider = "Supabase" (servono Storage:Supabase:Url e Storage:Supabase:SecretKey).
    public class SupabaseFileStorageService : IFileStorageService
    {
        public const string PublicBucket = "uploads";
        public const string PrivateBucket = "private-uploads";

        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public SupabaseFileStorageService(HttpClient http, IOptions<StorageOptions> options)
        {
            _http = http;
            _baseUrl = options.Value.Supabase.BaseUrl;
        }

        // Intestazioni di autenticazione e tempo massimo, uguali per tutte le chiamate.
        // Le chiavi nuove (sb_secret_...) vanno solo nell'intestazione "apikey": non sono JWT e il gateway di Supabase
        // rifiuterebbe una "Authorization: Bearer sb_secret_...". Le chiavi vecchie (JWT, iniziano con "eyJ") vanno in entrambe.
        public static void ConfigureClient(HttpClient client, SupabaseStorageOptions options)
        {
            client.Timeout = TimeSpan.FromSeconds(60);

            var key = options.SecretKey?.Trim() ?? string.Empty;
            if (key.Length == 0)
                return;

            client.DefaultRequestHeaders.TryAddWithoutValidation("apikey", key);
            if (key.StartsWith("eyJ", StringComparison.Ordinal))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        public async Task<string> SaveAsync(IFormFile file, string folder, StorageVisibility visibility)
        {
            var storageKey = $"{folder.Trim('/')}/{Guid.NewGuid()}{SafeExtension(file.FileName)}";
            var bucket = BucketFor(visibility);

            try
            {
                await using var stream = file.OpenReadStream();
                using var content = new StreamContent(stream);
                // Il tipo si decide dall'estensione (già controllata prima del salvataggio), non da quello dichiarato dal browser
                content.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(storageKey));
                content.Headers.ContentLength = file.Length;

                using var request = new HttpRequestMessage(HttpMethod.Post, ObjectUrl(bucket, storageKey)) { Content = content };
                request.Headers.TryAddWithoutValidation("x-upsert", "false"); // mai sovrascrivere: il nome è un GUID nuovo

                if (visibility == StorageVisibility.Public)
                {
                    // Ogni caricamento ha un nome nuovo e il file non cambia mai: la copia nella cache può durare un anno
                    request.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromDays(365) };
                }

                using var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    throw await FailureAsync("caricamento", bucket, storageKey, response);
            }
            catch (FileStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[STORAGE:Supabase] Caricamento di {Key} nel bucket {Bucket} non riuscito", storageKey, bucket);
                throw new FileStorageException("Impossibile salvare il file nell'archivio (Supabase Storage non raggiungibile).", ex);
            }

            return storageKey;
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(string storageKey, StorageVisibility visibility)
        {
            var bucket = BucketFor(visibility);
            HttpResponseMessage? response = null;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, AuthenticatedUrl(bucket, storageKey));
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    var inner = await response.Content.ReadAsStreamAsync();

                    // Da qui in poi la risposta appartiene allo stream: la chiude lui quando il controller ha finito di inviarlo
                    var owned = response;
                    response = null;
                    return (new ResponseStream(inner, owned), ContentTypeFor(storageKey), LastSegment(storageKey));
                }

                if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                    && IsObjectNotFound(response.StatusCode, await ReadBodyAsync(response)))
                {
                    return null;
                }

                throw await FailureAsync("lettura", bucket, storageKey, response);
            }
            catch (FileStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[STORAGE:Supabase] Lettura di {Key} dal bucket {Bucket} non riuscita", storageKey, bucket);
                throw new FileStorageException("Impossibile leggere il file dall'archivio (Supabase Storage non raggiungibile).", ex);
            }
            finally
            {
                response?.Dispose();
            }
        }

        // L'interfaccia è sincrona (void). Qui si usa la versione sincrona di HttpClient (Send), non un blocco su un Task.
        // È una pulizia "al meglio": se Supabase non risponde si scrive un avviso nel log e si va avanti, come succede
        // con un file già assente. Resta al massimo un file orfano nell'archivio, mai un errore all'utente.
        public void Delete(string storageKey, StorageVisibility visibility)
        {
            var bucket = BucketFor(visibility);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, ObjectUrl(bucket, storageKey));
                using var response = _http.Send(request);

                if (response.IsSuccessStatusCode)
                    return;

                var body = ReadBody(response);
                if (IsObjectNotFound(response.StatusCode, body))
                    return; // era già stato eliminato

                Log.Warning("[STORAGE:Supabase] Eliminazione di {Key} dal bucket {Bucket} non riuscita: HTTP {Status} {Body}",
                    storageKey, bucket, (int)response.StatusCode, Truncate(body, 300));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[STORAGE:Supabase] Eliminazione di {Key} dal bucket {Bucket} non riuscita", storageKey, bucket);
            }
        }

        // Indirizzo pubblico diretto del file (solo bucket "uploads"), pronto per <img src>
        public string? GetPublicUrl(string storageKey) =>
            $"{_baseUrl}/storage/v1/object/public/{PublicBucket}/{EscapePath(storageKey)}";

        // ---- utilità ----

        private static string BucketFor(StorageVisibility visibility) =>
            visibility == StorageVisibility.Public ? PublicBucket : PrivateBucket;

        private string ObjectUrl(string bucket, string storageKey) =>
            $"{_baseUrl}/storage/v1/object/{bucket}/{EscapePath(storageKey)}";

        // Lettura con la chiave segreta (serve anche per il bucket privato)
        private string AuthenticatedUrl(string bucket, string storageKey) =>
            $"{_baseUrl}/storage/v1/object/authenticated/{bucket}/{EscapePath(storageKey)}";

        // "sessions/abc.jpg" -> "sessions/abc.jpg", ma con ogni pezzo codificato (spazi, accenti, ecc.)
        internal static string EscapePath(string storageKey) =>
            string.Join('/', storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

        private static string LastSegment(string storageKey)
        {
            var idx = storageKey.LastIndexOf('/');
            return idx < 0 ? storageKey : storageKey[(idx + 1)..];
        }

        // Estensione in minuscolo, solo lettere e cifre (".jpg"); altrimenti niente. Il nome del file dell'utente
        // non finisce mai nella chiave: la chiave è sempre "cartella/GUID.estensione".
        internal static string SafeExtension(string? fileName)
        {
            var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
            if (ext.Length < 2 || ext.Length > 10)
                return string.Empty;

            return ext.Skip(1).All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9')) ? ext : string.Empty;
        }

        internal static string ContentTypeFor(string storageKey)
        {
            var ext = Path.GetExtension(storageKey).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }

        // Supabase Storage segnala "oggetto non trovato" con HTTP 404, ma in certe versioni con HTTP 400 e
        // {"statusCode":"404","error":"not_found",...} nel corpo: li trattiamo allo stesso modo.
        // "Bucket not found" invece NO: significa che il bucket non è stato creato, è un errore di configurazione
        // e deve restare un errore (altrimenti ogni file sembrerebbe semplicemente "non trovato").
        internal static bool IsObjectNotFound(HttpStatusCode status, string body)
        {
            if (status != HttpStatusCode.NotFound && status != HttpStatusCode.BadRequest)
                return false;

            if (body.Contains("bucket", StringComparison.OrdinalIgnoreCase))
                return false;

            return status == HttpStatusCode.NotFound
                   || body.Contains("not_found", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("not found", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("\"statusCode\":\"404\"", StringComparison.OrdinalIgnoreCase);
        }

        // Registra l'errore (mai la chiave: nel corpo della risposta di Supabase non c'è) e prepara l'eccezione
        private static async Task<FileStorageException> FailureAsync(string action, string bucket, string storageKey, HttpResponseMessage response)
        {
            var body = await ReadBodyAsync(response);

            Log.Error("[STORAGE:Supabase] {Action} di {Key} nel bucket {Bucket} rifiutato: HTTP {Status} {Body}",
                action, storageKey, bucket, (int)response.StatusCode, Truncate(body, 300));

            return new FileStorageException($"Supabase Storage ha risposto HTTP {(int)response.StatusCode} ({action} del file).");
        }

        // Versione sincrona (per Delete): con HttpClient.Send il contenuto è già tutto in memoria
        private static string ReadBody(HttpResponseMessage response)
        {
            try
            {
                using var reader = new StreamReader(response.Content.ReadAsStream());
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static async Task<string> ReadBodyAsync(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "…";

        // Il file scaricato da Supabase arriva "a pezzi" dentro una risposta HTTP: la risposta va chiusa insieme allo stream,
        // altrimenti la connessione resta occupata. Il controller chiude lo stream alla fine dell'invio.
        private sealed class ResponseStream : Stream
        {
            private readonly Stream _inner;
            private readonly HttpResponseMessage _response;

            public ResponseStream(Stream inner, HttpResponseMessage response)
            {
                _inner = inner;
                _response = response;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override int Read(Span<byte> buffer) => _inner.Read(buffer);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                _inner.ReadAsync(buffer, offset, count, cancellationToken);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                _inner.ReadAsync(buffer, cancellationToken);

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _inner.Dispose();
                    _response.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
