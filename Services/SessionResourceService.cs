using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Helpers;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public class SessionResourceService : ServiceBase
    {
        private readonly IFileStorageService _fileStorage;
        private const string FolderPrefix = "session-files";

        public SessionResourceService(ApplicationDbContext applicationDbContext, IFileStorageService fileStorage) : base(applicationDbContext)
        {
            _fileStorage = fileStorage;
        }

        // Determina cosa può fare l'utente (via email/ruolo presi dal token) sui file di questa sessione:
        // il Master proprietario e l'Admin possono gestire (carica/elimina), un Player iscritto può solo vedere/scaricare.
        public async Task<SessionFileAccess> GetAccessLevel(Guid sessionId, string? email, string? role)
        {
            if (string.IsNullOrWhiteSpace(email))
                return SessionFileAccess.None;

            if (role == StringConstants.AdminRole)
                return SessionFileAccess.Manage;

            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Master)
                .Include(s => s.Players)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return SessionFileAccess.None;

            if (session.Master != null && session.Master.Email == email)
                return SessionFileAccess.Manage;

            if (session.Players.Any(p => p.Email == email))
                return SessionFileAccess.View;

            return SessionFileAccess.None;
        }

        public async Task<List<SessionResourceFile>> GetForSession(Guid sessionId)
        {
            return await _context.SessionResourceFiles
                .AsNoTracking()
                .Where(f => f.SessionId == sessionId)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<SessionResourceFile> UploadAsync(Guid sessionId, IFormFile file)
        {
            var validationError = FileValidationHelper.ValidateDocument(file);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var storageKey = await _fileStorage.SaveAsync(file, $"{FolderPrefix}/{sessionId}", StorageVisibility.Private);

            var resource = new SessionResourceFile
            {
                SessionResourceFileId = Guid.NewGuid(),
                SessionId = sessionId,
                FileName = file.FileName,
                StorageKey = storageKey,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.SessionResourceFiles.Add(resource);

            var saved = await SaveAsync();
            if (!saved)
            {
                // rollback del file fisico se il salvataggio su DB fallisce
                _fileStorage.Delete(storageKey, StorageVisibility.Private);
                throw new InvalidOperationException("Salvataggio del file fallito.");
            }

            return resource;
        }

        // Restituisce a quale sessione appartiene un file, per verificare che il fileId nella richiesta
        // corrisponda davvero al sessionId nella rotta prima di aprire lo stream.
        public async Task<Guid?> GetSessionIdForFile(Guid fileId)
        {
            return await _context.SessionResourceFiles
                .AsNoTracking()
                .Where(f => f.SessionResourceFileId == fileId)
                .Select(f => (Guid?)f.SessionId)
                .FirstOrDefaultAsync();
        }

        public async Task<(Stream Stream, string ContentType, string FileName)?> DownloadAsync(Guid fileId)
        {
            var file = await _context.SessionResourceFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.SessionResourceFileId == fileId);

            if (file == null)
                return null;

            var opened = await _fileStorage.OpenReadAsync(file.StorageKey, StorageVisibility.Private);
            if (opened == null)
                return null;

            return (opened.Value.Stream, file.ContentType, file.FileName);
        }

        // Hard delete voluto: a differenza di Player/Master/Game/Session, qui non ha senso tenere
        // in giro indefinitamente un blob fisico "cancellato" solo a livello di flag.
        public async Task<bool> DeleteAsync(Guid fileId)
        {
            var file = await _context.SessionResourceFiles.FirstOrDefaultAsync(f => f.SessionResourceFileId == fileId);
            if (file == null)
                return false;

            _fileStorage.Delete(file.StorageKey, StorageVisibility.Private);
            _context.SessionResourceFiles.Remove(file);

            return await SaveAsync();
        }
    }
}
