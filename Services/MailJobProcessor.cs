using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;
using Rolayther.Services.Mail;
using Serilog;

namespace Rolayther.Services
{
    // Esegue i lavori di posta della coda (vedi MailJobWorker): rilegge dal database i dati che servono
    // (una query leggera per sessione/giocatore, senza caricare le entità complete) e invia l'email.
    //
    // ProcessAsync restituisce true se il lavoro è concluso (anche quando non c'è nulla da fare, ad esempio
    // la sessione nel frattempo è stata cancellata) e false se va ritentato perché l'invio non è riuscito.
    public class MailJobProcessor : ServiceBase, IMailJobHandler
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailNotificationService _emails;

        public MailJobProcessor(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailNotificationService emails)
            : base(context)
        {
            _userManager = userManager;
            _emails = emails;
        }

        public async Task<bool> ProcessAsync(MailJob job, CancellationToken ct)
        {
            switch (job)
            {
                case SessionJoinedPlayerJob j:
                    {
                        var (session, player) = await LoadAsync(j.SessionId, j.PlayerId, ct);
                        if (session == null || player == null) return Skipped("iscrizione");
                        return await _emails.SendSessionJoinedAsync(session, player, ct);
                    }

                case SessionJoinedMasterJob j:
                    {
                        var (session, player) = await LoadAsync(j.SessionId, j.PlayerId, ct);
                        if (session == null || player == null) return Skipped("nuovo giocatore");
                        return await _emails.SendSessionNewPlayerAsync(session, player, ct);
                    }

                case SessionCreatedJob j:
                    {
                        var session = await LoadSessionAsync(j.SessionId, ct);
                        if (session == null) return Skipped("sessione creata");
                        return await _emails.SendSessionCreatedAsync(session, j.CreatedByAdmin, ct);
                    }

                case ConfirmationResendJob j:
                    {
                        var user = await _userManager.FindByEmailAsync(j.Email);

                        // Nessun account, account eliminato o già confermato: niente da fare (e nessuna traccia all'esterno)
                        if (user == null || user.IsDeleted || user.EmailConfirmed) return true;

                        var isMasterRequest = await _context.Masters
                            .AsNoTracking()
                            .AnyAsync(m => m.Email == user.Email && m.Status == ApprovalStatus.Pending, ct);

                        return await _emails.SendEmailConfirmationAsync(user, isMasterRequest, ct);
                    }

                default:
                    return true;
            }
        }

        private static bool Skipped(string what)
        {
            Log.Warning("[MAIL] Email '{What}' non inviata: sessione o giocatore non più disponibili", what);
            return true;
        }

        private async Task<(SessionEmailInfo? Session, PlayerEmailInfo? Player)> LoadAsync(Guid sessionId, Guid playerId, CancellationToken ct)
        {
            var session = await LoadSessionAsync(sessionId, ct);
            var player = await LoadPlayerAsync(playerId, ct);
            return (session, player);
        }

        // Una sola query: i dati della sessione, il suo Master e quanti giocatori ci sono ora
        internal IQueryable<SessionEmailInfo> SessionInfoQuery(Guid sessionId) =>
            _context.Sessions
                .AsNoTracking()
                .Where(s => s.SessionId == sessionId)
                .Select(s => new SessionEmailInfo
                {
                    SessionId = s.SessionId,
                    Title = s.SessionTitle,
                    Description = s.SessionDescription,
                    ScheduledAt = s.ScheduledAt,
                    Duration = s.Duration,
                    MaxPlayers = s.NumbOfPlayer,
                    PlayersCount = s.Players.Count(),
                    State = s.CurrentState,
                    SessionLink = s.SessionLink,
                    GameTitle = s.Game != null ? s.Game.Title : string.Empty,
                    GenreName = s.Genre != null ? s.Genre.Name : string.Empty,
                    MasterEmail = s.Master != null ? s.Master.Email : string.Empty,
                    MasterName = s.Master != null ? s.Master.Name : string.Empty,
                    MasterNickName = s.Master != null ? s.Master.NickName : string.Empty
                });

        private Task<SessionEmailInfo?> LoadSessionAsync(Guid sessionId, CancellationToken ct) =>
            SessionInfoQuery(sessionId).FirstOrDefaultAsync(ct);

        internal IQueryable<PlayerEmailInfo> PlayerInfoQuery(Guid playerId) =>
            _context.Players
                .AsNoTracking()
                .Where(p => p.PlayerId == playerId)
                .Select(p => new PlayerEmailInfo
                {
                    PlayerId = p.PlayerId,
                    Email = p.Email,
                    Name = p.Name,
                    Surname = p.Surname,
                    NickName = p.NickName
                });

        private Task<PlayerEmailInfo?> LoadPlayerAsync(Guid playerId, CancellationToken ct) =>
            PlayerInfoQuery(playerId).FirstOrDefaultAsync(ct);
    }
}
