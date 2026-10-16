namespace Rolayther.Models.DTOs.Response
{
    // Master "in miniatura": è ciò che compare dentro una sessione (es. "Mastered by ...").
    public class MasterSummaryDto
    {
        public Guid MasterId { get; set; }
        public string NickName { get; set; } = string.Empty;
        public string? AvatarImgUrl { get; set; }
    }
}
