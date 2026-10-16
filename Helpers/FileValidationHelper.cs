using Microsoft.AspNetCore.Http;

namespace Rolayther.Helpers
{
    public static class FileValidationHelper
    {
        public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        public static readonly string[] AllowedDocumentExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".txt", ".zip", ".docx" };

        public const long MaxImageSizeBytes = 5L * 1024 * 1024;     // 5 MB
        public const long MaxDocumentSizeBytes = 20L * 1024 * 1024; // 20 MB

        // Torna null se il file va bene, altrimenti un messaggio d'errore pronto per l'utente.
        public static string? ValidateImage(IFormFile? file)
        {
            var error = Validate(file, AllowedImageExtensions, MaxImageSizeBytes, "immagine");
            if (error != null)
                return error;

            // L'estensione si falsifica in un attimo: controlliamo anche i primi byte del contenuto.
            if (!HasImageSignature(file!))
                return "Il file non è un'immagine valida (ammessi JPG, PNG, WebP e GIF).";

            return null;
        }

        public static string? ValidateDocument(IFormFile? file) =>
            Validate(file, AllowedDocumentExtensions, MaxDocumentSizeBytes, "file");

        // Firme (magic bytes) dei formati immagine ammessi: JPEG, PNG, GIF e WebP.
        private static bool HasImageSignature(IFormFile file)
        {
            Span<byte> h = stackalloc byte[12];
            int read;
            using (var stream = file.OpenReadStream())
            {
                read = stream.ReadAtLeast(h, 12, throwOnEndOfStream: false);
            }

            if (read < 12)
                return false;

            var jpeg = h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
            var png = h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47
                      && h[4] == 0x0D && h[5] == 0x0A && h[6] == 0x1A && h[7] == 0x0A;
            var gif = h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38;
            var webp = h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                       && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;

            return jpeg || png || gif || webp;
        }

        private static string? Validate(IFormFile? file, string[] allowedExtensions, long maxSizeBytes, string label)
        {
            if (file == null || file.Length == 0)
                return $"Nessun {label} caricato.";

            if (file.Length > maxSizeBytes)
                return $"Il {label} supera la dimensione massima di {maxSizeBytes / 1024 / 1024} MB.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return $"Formato non consentito ({ext}). Formati ammessi: {string.Join(", ", allowedExtensions)}.";

            return null;
        }
    }
}
