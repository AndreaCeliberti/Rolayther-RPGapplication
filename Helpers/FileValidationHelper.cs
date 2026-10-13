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
        public static string? ValidateImage(IFormFile? file) =>
            Validate(file, AllowedImageExtensions, MaxImageSizeBytes, "immagine");

        public static string? ValidateDocument(IFormFile? file) =>
            Validate(file, AllowedDocumentExtensions, MaxDocumentSizeBytes, "file");

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
