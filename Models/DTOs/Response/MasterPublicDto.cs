namespace Rolayther.Models.DTOs.Response
{
    // Vista PUBBLICA di un Master: ciò che vede chiunque (anche senza login).
    // Niente nome/cognome, email, data di nascita, password, stato di approvazione:
    // quelli li vedono solo l'Admin e il Master stesso.
    public class MasterPublicDto
    {
        public Guid MasterId { get; set; }
        public string NickName { get; set; } = string.Empty;
        public string? AvatarImgUrl { get; set; }
        public string? BioMaster { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SessionSummaryDto> Sessions { get; set; } = new();
    }
}
