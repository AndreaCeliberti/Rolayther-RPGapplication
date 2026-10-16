using Microsoft.AspNetCore.Http;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public interface IFileStorageService
    {
        // Salva il file nella sotto-cartella indicata e restituisce una chiave univoca per recuperarlo/eliminarlo in seguito.
        Task<string> SaveAsync(IFormFile file, string folder, StorageVisibility visibility);

        // Apre uno stream in lettura sul file. Torna null se non esiste.
        Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(string storageKey, StorageVisibility visibility);

        // Elimina il file se esiste (nessuna eccezione se già assente).
        void Delete(string storageKey, StorageVisibility visibility);

        // URL pubblico diretto per un file Public (per <img src>). Torna null per i file Private:
        // quelli vanno scaricati solo tramite un endpoint autenticato che controlla i permessi.
        string? GetPublicUrl(string storageKey);
    }
}
