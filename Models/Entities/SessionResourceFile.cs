using System.ComponentModel.DataAnnotations;

namespace Rolayther.Models.Entities
{
    public class SessionResourceFile : BaseEntity
    {
        [Key]
        public Guid SessionResourceFileId { get; set; }

        public Guid SessionId { get; set; }
        public Session? Session { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        // Percorso relativo restituito da IFileStorageService: non è un URL pubblico,
        // serve solo internamente per ritrovare/eliminare il file.
        [Required]
        public string StorageKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
