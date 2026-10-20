using Microsoft.AspNetCore.Http;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    // Implementazione locale su disco: usa wwwroot/uploads per i file Public (serviti da UseStaticFiles)
    // e App_Data/private-uploads per i file Private (mai esposta via file statici).
    //
    // SOLO PER LO SVILUPPO: su Render (piano free) il filesystem del container è effimero,
    // quindi tutto ciò che viene salvato qui sparisce a ogni riavvio/redeploy. Online si usa
    // SupabaseFileStorageService (Storage:Provider = "Supabase"), che rispetta la stessa interfaccia
    // IFileStorageService: nient'altro nel resto del codice cambia. Vedi docs/DEPLOY.md.
    public class LocalDiskFileStorageService : IFileStorageService
    {
        private readonly string _publicRoot;
        private readonly string _privateRoot;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocalDiskFileStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _publicRoot = Path.Combine(env.WebRootPath, "uploads");
            _privateRoot = Path.Combine(env.ContentRootPath, "App_Data", "private-uploads");
            _httpContextAccessor = httpContextAccessor;

            Directory.CreateDirectory(_publicRoot);
            Directory.CreateDirectory(_privateRoot);
        }

        private string RootFor(StorageVisibility visibility) =>
            visibility == StorageVisibility.Public ? _publicRoot : _privateRoot;

        public async Task<string> SaveAsync(IFormFile file, string folder, StorageVisibility visibility)
        {
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var storageKey = $"{folder}/{fileName}";

            var fullPath = Path.Combine(RootFor(visibility), folder.Replace('/', Path.DirectorySeparatorChar), fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return storageKey;
        }

        public Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(string storageKey, StorageVisibility visibility)
        {
            var fullPath = Path.Combine(RootFor(visibility), storageKey.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(fullPath))
                return Task.FromResult<(Stream, string, string)?>(null);

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult<(Stream, string, string)?>((stream, GetContentType(fullPath), Path.GetFileName(fullPath)));
        }

        public void Delete(string storageKey, StorageVisibility visibility)
        {
            var fullPath = Path.Combine(RootFor(visibility), storageKey.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        public string? GetPublicUrl(string storageKey)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return $"/uploads/{storageKey}";

            return $"{request.Scheme}://{request.Host}/uploads/{storageKey}";
        }

        private static string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
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
    }
}
