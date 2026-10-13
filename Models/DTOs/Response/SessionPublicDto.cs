using Rolayther.Models.Entities;
using Rolayther.Models.Enums;

namespace Rolayther.Models.DTOs.Response
{
    // Vista PUBBLICA di una sessione. Stessi nomi di campo dell'entità, così il frontend non cambia,
    // ma Master e Players sono ridotti ai soli dati pubblici (niente email, date di nascita, nomi reali).
    public class SessionPublicDto
    {
        public Guid SessionId { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
        public string? SessionDescription { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string Duration { get; set; } = string.Empty;
        public int NumbOfPlayer { get; set; }

        // Il link per entrare nella sessione (Discord, Meet, Roll20...) è valorizzato SOLO per
        // l'Admin, il Master proprietario e i giocatori iscritti a questa sessione. Per tutti gli altri è null.
        public string? SessionLink { get; set; }

        public string? CoverImgUrl { get; set; }

        public Guid MasterId { get; set; }
        public Guid GameId { get; set; }
        public Guid GenreId { get; set; }

        public MasterSummaryDto? Master { get; set; }
        public Game? Game { get; set; }
        public Genre? Genre { get; set; }
        public List<PlayerPublicDto> Players { get; set; } = new();

        public SessionState CurrentState { get; set; }
    }
}
