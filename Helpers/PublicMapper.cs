using Rolayther.Models.DTOs.Response;
using Rolayther.Models.Entities;

namespace Rolayther.Helpers
{
    // Converte le entità nelle viste pubbliche. Qui, e solo qui, si decide cosa esce verso chi non è Admin:
    // se un campo non è copiato nel DTO, non può finire nella risposta.
    public static class PublicMapper
    {
        public static PlayerPublicDto ToPublic(this Player player) => new PlayerPublicDto
        {
            PlayerId = player.PlayerId,
            NickName = player.NickName,
            AvatarImgUrl = player.AvatarImgUrl
        };

        public static MasterSummaryDto ToSummary(this Master master) => new MasterSummaryDto
        {
            MasterId = master.MasterId,
            NickName = master.NickName,
            AvatarImgUrl = master.AvatarImgUrl
        };

        public static SessionSummaryDto ToSummary(this Session session) => new SessionSummaryDto
        {
            SessionId = session.SessionId,
            SessionTitle = session.SessionTitle,
            ScheduledAt = session.ScheduledAt,
            CurrentState = session.CurrentState
        };

        public static MasterPublicDto ToPublic(this Master master) => new MasterPublicDto
        {
            MasterId = master.MasterId,
            NickName = master.NickName,
            AvatarImgUrl = master.AvatarImgUrl,
            BioMaster = master.BioMaster,
            CreatedAt = master.CreatedAt,
            Sessions = (master.Sessions ?? new List<Session>())
                .Select(s => s.ToSummary())
                .ToList()
        };

        public static SessionPublicDto ToPublic(this Session session, ViewerContext viewer)
        {
            var players = session.Players ?? new List<Player>();

            // Il link della sessione è riservato a chi partecipa o gestisce la sessione
            var canSeeLink =
                viewer.IsAdmin ||
                viewer.OwnsAllSessions ||
                viewer.IsSameEmail(session.Master?.Email) ||
                players.Any(p => viewer.IsSameEmail(p.Email));

            return new SessionPublicDto
            {
                SessionId = session.SessionId,
                SessionTitle = session.SessionTitle,
                SessionDescription = session.SessionDescription,
                ScheduledAt = session.ScheduledAt,
                Duration = session.Duration,
                NumbOfPlayer = session.NumbOfPlayer,
                SessionLink = canSeeLink ? session.SessionLink : null,
                CoverImgUrl = session.CoverImgUrl,

                // Alcune query (es. GetAllSessions) non valorizzano gli Id scalari ma caricano le entità collegate:
                // in quel caso l'Id si ricava dall'entità.
                MasterId = session.Master?.MasterId ?? session.MasterId,
                GameId = session.Game?.GameId ?? session.GameId,
                GenreId = session.Genre?.GenreId ?? session.GenreId,

                Master = session.Master?.ToSummary(),
                Game = session.Game,
                Genre = session.Genre,
                Players = players.Select(p => p.ToPublic()).ToList(),
                CurrentState = session.CurrentState
            };
        }
    }
}
