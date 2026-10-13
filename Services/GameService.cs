using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public class GameService : ServiceBase
    {
        private readonly IFileStorageService _fileStorage;

        public GameService(ApplicationDbContext applicationDbContext, IFileStorageService fileStorage) : base(applicationDbContext)
        {
            _fileStorage = fileStorage;
        }

        // Carica una nuova cover per il gioco, sostituendo quella precedente se era un nostro file
        public async Task<string?> UpdateCoverAsync(Guid gameId, IFormFile file)
        {
            var validationError = FileValidationHelper.ValidateImage(file);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var game = await _context.Games.FirstOrDefaultAsync(g => g.GameId == gameId);
            if (game == null)
                return null;

            var oldKey = ExtractStorageKeyIfLocal(game.CoverImageUrl);

            var storageKey = await _fileStorage.SaveAsync(file, "games", StorageVisibility.Public);
            var publicUrl = _fileStorage.GetPublicUrl(storageKey);

            game.CoverImageUrl = publicUrl;
            _context.Games.Update(game);

            var saved = await SaveAsync();
            if (saved && oldKey != null)
                _fileStorage.Delete(oldKey, StorageVisibility.Public);

            return saved ? publicUrl : null;
        }

        private static string? ExtractStorageKeyIfLocal(string? imgUrl)
        {
            if (string.IsNullOrWhiteSpace(imgUrl))
                return null;

            var marker = "/uploads/";
            var idx = imgUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return idx < 0 ? null : imgUrl[(idx + marker.Length)..];
        }

        // List Games
        public async Task <List<Game>> GetAllGames()
        {
            return await _context.Games
                .AsNoTracking()
                .Include(ge => ge.Genres)
                .Select(g => new Game
                {
                    GameId = g.GameId,
                    Title = g.Title,
                    Description = g.Description,
                    CoverImageUrl = g.CoverImageUrl,
                    Genres = g.Genres
                })
                .ToListAsync();
        }

        // Create Game
        public async Task<bool> CreateGame(GameRequestDto gameRequestDto)
        {
            var newGame = new Game
            {
                GameId = Guid.NewGuid(),
                Title = gameRequestDto.Title,
                Description = gameRequestDto.Description,
                CoverImageUrl = gameRequestDto.CoverImageUrl,
                Genres = new List<Genre>(),
                IsDeleted = false
            };
            _context.Games.Add(newGame);

            return await SaveAsync();

        }

        //Soft delete
        public Task<bool> DeleteGame(Guid gameId)
        {
            return SoftDeleteAsync<Game>(gameId);
        }

    }
}
