using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;

namespace Rolayther.Services
{
    public class PlayerService : ServiceBase
    {
        public PlayerService(ApplicationDbContext applicationDbContext) : base(applicationDbContext) { }

        // List Players

        public async Task<List<Player>> GetAllPlayers()
        {
            return await _context.Players
                .AsNoTracking()
                .Select(p => new Player
                {
                    PlayerId = p.PlayerId,
                    Name = p.Name,
                    Surname = p.Surname,
                    NickName = p.NickName,
                    DateOfBirth = p.DateOfBirth,
                    AvatarImgUrl = p.AvatarImgUrl,
                    Email = p.Email,
                    BioPlayer = p.BioPlayer,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }

        // Create Player
        public async Task<bool> CreatePlayer(PlayerRequestDto playerRequestDto)
        {
            var newPlayer = new Player
            {
                PlayerId = Guid.NewGuid(),
                Name = playerRequestDto.Name,
                Surname = playerRequestDto.Surname,
                NickName = playerRequestDto.NickName,
                DateOfBirth = playerRequestDto.DateOfBirth,
                AvatarImgUrl = playerRequestDto.AvatarImgUrl,
                Email = playerRequestDto.Email,
                BioPlayer = playerRequestDto.BioPlayer,
                CreatedAt = DateTime.UtcNow
            };
            _context.Players.Add(newPlayer);
            return await SaveAsync();
        }

        // Get player
        public async Task<Player?> GetPlayerById(Guid playerId)
        {
            return await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);
        }

        // Get player by email

        public async Task<Player?> GetPlayerByEmail(string email)
        {
            return await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email == email);
        }

        // Get session by player id
        public async Task<List<Session>?> GetSessionsByPlayerId(Guid playerId)
        {
            var exists = await _context.Players.AnyAsync(p => p.PlayerId == playerId);
            if (!exists) return null;

            return await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Master)
                .Include(s => s.Game)
                .Include(s => s.Genre)
                .Include(s => s.Players)
                .Where(s => s.Players.Any(p => p.PlayerId == playerId))
                .Select(s => new Session
                {
                    SessionId = s.SessionId,
                    SessionTitle = s.SessionTitle,
                    SessionDescription = s.SessionDescription,
                    ScheduledAt = s.ScheduledAt,
                    Duration = s.Duration,
                    NumbOfPlayer = s.NumbOfPlayer,
                    CoverImgUrl = s.CoverImgUrl,
                    Master = s.Master,
                    Game = s.Game,
                    Genre = s.Genre,
                    Players = s.Players
                })
                .ToListAsync();
        }


        // Update player
        public async Task<bool> UpdatePlayer(Guid playerId, PlayerUpdateDto playerUpdateDto)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null)
                return false;

            player.Name = playerUpdateDto.Name;
            player.Surname = playerUpdateDto.Surname;
            player.NickName = playerUpdateDto.NickName;
            player.DateOfBirth = playerUpdateDto.DateOfBirth;
            player.AvatarImgUrl = playerUpdateDto.AvatarImgUrl;
            player.Email = playerUpdateDto.Email;
            player.BioPlayer = playerUpdateDto.BioPlayer;

            _context.Players.Update(player);

            return await SaveAsync();
        }


        //Soft delete
        public Task<bool> DeletePlayer(Guid playerId)
        {
            return SoftDeleteAsync<Player>(playerId);
        }
    }
}
