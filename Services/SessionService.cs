using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.DTOs.Response;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;
using System.Data;
using System.Security.Claims;

namespace Rolayther.Services
{
    public class SessionService : ServiceBase
    {
        public SessionService(ApplicationDbContext applicationDbContext) : base(applicationDbContext) { }

        // List Sessions
        public async Task<List<Session>> GetAllSessions()
        {
            return await _context.Sessions
         .AsNoTracking()
         .Include(s => s.Master)
         .Include(s => s.Game)
         .Include(s => s.Genre)
         .Include(s => s.Players)
         .Select(s => new Session
         {
             SessionId = s.SessionId,
             SessionTitle = s.SessionTitle,
             SessionDescription = s.SessionDescription,
             ScheduledAt = s.ScheduledAt,
             Duration = s.Duration,
             NumbOfPlayer = s.NumbOfPlayer,
             SessionLink = s.SessionLink,
             CoverImgUrl = s.CoverImgUrl,
             Master = s.Master,
             Game = s.Game,
             Genre = s.Genre,
             Players = s.Players,

             CurrentState = s.CurrentState
         })
         .ToListAsync();
        }

        // Get Session by Id
        public async Task<Session?> GetSessionById(Guid sessionId)
        {
            return await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Master)
                .Include(s => s.Game)
                .Include(s => s.Genre)
                .Include(s => s.Players)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        // Create Session

        public async Task<bool> CreateSession(SessionRequestDto sessionRequestDto)
        {
            var newSession = new Session
            {
                SessionId = Guid.NewGuid(),
                SessionTitle = sessionRequestDto.SessionTitle,
                SessionDescription = sessionRequestDto.SessionDescription,
                ScheduledAt = sessionRequestDto.ScheduledAt,
                Duration = sessionRequestDto.Duration,
                NumbOfPlayer = sessionRequestDto.NumbOfPlayer,
                SessionLink = sessionRequestDto.SessionLink,
                CoverImgUrl = sessionRequestDto.CoverImgUrl,
                MasterId = sessionRequestDto.MasterId,
                GameId = sessionRequestDto.GameId,
                GenreId = sessionRequestDto.GenreId,
                Players = new List<Player>()
            };
            _context.Sessions.Add(newSession);
            return await SaveAsync();
        }

        public async Task<List<Session>> GetSessionsByMasterId(Guid masterId)
        {
            return await _context.Sessions
                .AsNoTracking()
                .Where(s => s.MasterId == masterId)
                .Include(s => s.Game)
                .Include(s => s.Genre)
                .Include(s => s.Players)
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync();
        }

        // Update Session

        public async Task<bool> UpdateSession(Guid sessionId, SessionRequestDto dto)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null) return false;

            session.SessionTitle = dto.SessionTitle;
            session.SessionDescription = dto.SessionDescription;
            session.ScheduledAt = dto.ScheduledAt;
            session.Duration = dto.Duration;
            session.NumbOfPlayer = dto.NumbOfPlayer;
            session.SessionLink = dto.SessionLink;
            session.CoverImgUrl = dto.CoverImgUrl;
            session.MasterId = dto.MasterId;
            session.GameId = dto.GameId;
            session.GenreId = dto.GenreId;

            _context.Sessions.Update(session);
            return await SaveAsync();
        }




        // Change Session State

        public async Task ChangeSessionState(Guid sessionId, SessionState newState, string userId, string role, string? reason)
        {
            // 1) Leggi lo stato attuale (e verifica esistenza) in una sola query
            var currentState = await _context.Sessions
                .Where(s => s.SessionId == sessionId)
                .Select(s => (SessionState?)s.CurrentState)
                .SingleOrDefaultAsync();

            if (currentState is null)
                throw new KeyNotFoundException("Session not found");

            // idempotenza
            if (currentState.Value == newState)
                return;

            if (!SessionStateService.CanTransition(currentState.Value, newState))
                throw new InvalidOperationException($"Invalid transition from {currentState.Value} to {newState}");

            // 2) Update atomico: cambia stato SOLO se è ancora quello che hai letto
            var rows = await _context.Sessions
                .Where(s => s.SessionId == sessionId && s.CurrentState == currentState.Value)
                .ExecuteUpdateAsync(u => u.SetProperty(s => s.CurrentState, newState));

            if (rows == 0)
                throw new InvalidOperationException("Conflitto: stato già cambiato da un'altra richiesta. Riprova.");

            // 3) Inserisci history
            _context.SessionStateHistories.Add(new SessionStateHistory
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                FromState = currentState.Value,
                ToState = newState,
                ChangedByUserId = userId,
                ChangedByRole = role,
                Reason = reason,
                ChangedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }




        // Add player to session
        public async Task<bool> AddPlayerToSession(Guid sessionId, Guid playerId, string userId, string role)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // 1) Lock della riga Session per evitare race condition sull’ultimo posto
            var sessionData = await _context.Sessions
                .FromSqlInterpolated($@"
            SELECT * FROM Sessions WITH (UPDLOCK, ROWLOCK)
            WHERE SessionId = {sessionId}
        ")
                .Select(s => new { s.SessionId, s.NumbOfPlayer, s.CurrentState })
                .SingleOrDefaultAsync();

            if (sessionData is null) return false;

            // Stato valido
            if (sessionData.CurrentState != SessionState.Published)
                throw new InvalidOperationException("Cannot join a session that is not published.");

            // 2) Player esiste
            var playerExists = await _context.Players.AnyAsync(p => p.PlayerId == playerId);
            if (!playerExists) return false;

            // 3) Già iscritto?
            var alreadyIn = await _context.Database.ExecuteSqlInterpolatedAsync($@"
        SELECT 1
        FROM SessionPlayer
        WHERE SessionId = {sessionId} AND PlayerId = {playerId}
    ");

            // Nota: ExecuteSql* ritorna -1 su SELECT, quindi facciamo Any via query vera:
            var alreadyJoined = await _context.Set<Dictionary<string, object>>("SessionPlayer")
                .AnyAsync(x => EF.Property<Guid>(x, "SessionId") == sessionId &&
                               EF.Property<Guid>(x, "PlayerId") == playerId);

            if (alreadyJoined)
                throw new InvalidOperationException("Player already joined.");

            // 4) Quanti player ci sono ORA? (con lock già preso sulla sessione)
            var count = await _context.Set<Dictionary<string, object>>("SessionPlayer")
                .CountAsync(x => EF.Property<Guid>(x, "SessionId") == sessionId);

            if (count >= sessionData.NumbOfPlayer)
                throw new InvalidOperationException("Session is already full.");

            // 5) Inserisci nella join table (atomicamente dentro tx)
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO SessionPlayer (SessionId, PlayerId)
            VALUES ({sessionId}, {playerId});
        ");
            }
            catch (DbUpdateException)
            {
                // Dup key -> qualcun altro ha inserito prima
                throw new InvalidOperationException("Join conflict. Retry.");
            }

            // 6) Se ora è piena -> set Full + history
            var newCount = count + 1;
            if (newCount == sessionData.NumbOfPlayer)
            {
                await _context.Sessions
                    .Where(s => s.SessionId == sessionId && s.CurrentState == SessionState.Published)
                    .ExecuteUpdateAsync(u => u.SetProperty(s => s.CurrentState, SessionState.Full));

                _context.SessionStateHistories.Add(new SessionStateHistory
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    FromState = SessionState.Published,
                    ToState = SessionState.Full,
                    ChangedByUserId = userId,
                    ChangedByRole = role,
                    Reason = "Session reached max number of players",
                    ChangedAtUtc = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return true;
        }




        // Remove player from session
        public async Task<bool> RemovePlayerFromSession(Guid sessionId, Guid playerId, string userId, string role)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // 1) Lock della sessione (evita race tra leave/join e cambio stato)
            var sessionData = await _context.Sessions
                .FromSqlInterpolated($@"
            SELECT * FROM Sessions WITH (UPDLOCK, ROWLOCK)
            WHERE SessionId = {sessionId}
        ")
                .Select(s => new { s.SessionId, s.NumbOfPlayer, s.CurrentState })
                .SingleOrDefaultAsync();

            if (sessionData is null) return false;

            // 2) Se non è iscritto, niente da fare
            var isJoined = await _context.Set<Dictionary<string, object>>("SessionPlayer")
                .AnyAsync(x => EF.Property<Guid>(x, "SessionId") == sessionId &&
                               EF.Property<Guid>(x, "PlayerId") == playerId);

            if (!isJoined) return false;

            // 3) Rimuovi dalla join table
            var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($@"
        DELETE FROM SessionPlayer
        WHERE SessionId = {sessionId} AND PlayerId = {playerId};
    ");

            if (deleted == 0) return false; // qualcuno l'ha già rimosso in mezzo

            // 4) Se era FULL, ora torna PUBLISHED (per la tua regola)
            if (sessionData.CurrentState == SessionState.Full)
            {
                // torna Published solo se è ancora Full (coerenza)
                var rows = await _context.Sessions
                    .Where(s => s.SessionId == sessionId && s.CurrentState == SessionState.Full)
                    .ExecuteUpdateAsync(u => u.SetProperty(s => s.CurrentState, SessionState.Published));

                if (rows > 0)
                {
                    _context.SessionStateHistories.Add(new SessionStateHistory
                    {
                        Id = Guid.NewGuid(),
                        SessionId = sessionId,
                        FromState = SessionState.Full,
                        ToState = SessionState.Published,
                        ChangedByUserId = userId,
                        ChangedByRole = role,
                        Reason = "Player left session, slots available",
                        ChangedAtUtc = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return true;
        }

        //Soft delete

        public Task<bool> DeleteSession(Guid sessionId)
        {
            return SoftDeleteAsync<Session>(sessionId);
        }
    }
}
