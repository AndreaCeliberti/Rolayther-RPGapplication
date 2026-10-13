using Rolayther.Models.Enums;

namespace Rolayther.Models.DTOs.Response
{
    // Sessione "in miniatura": è ciò che compare nel profilo pubblico di un Master.
    public class SessionSummaryDto
    {
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public SessionState CurrentState { get; set; }
    }
}
