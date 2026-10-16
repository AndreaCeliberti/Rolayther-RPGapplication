using Rolayther.Models.Enums;

namespace Rolayther.Services.Mail
{
    // Dati di una sessione così come servono alle email: una sola query leggera (vedi MailJobProcessor),
    // senza caricare le entità complete con tutti i loro collegamenti.
    public sealed class SessionEmailInfo
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string Duration { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public int PlayersCount { get; set; }
        public SessionState State { get; set; }
        public string? SessionLink { get; set; }

        public string GameTitle { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;

        public string MasterEmail { get; set; } = string.Empty;
        public string MasterName { get; set; } = string.Empty;
        public string MasterNickName { get; set; } = string.Empty;
    }

    public sealed class PlayerEmailInfo
    {
        public Guid PlayerId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
    }
}
