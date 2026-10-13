using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public class GenreService : ServiceBase
    {
        private readonly IFileStorageService _fileStorage;

        public GenreService(ApplicationDbContext applicationDbContext, IFileStorageService fileStorage) : base(applicationDbContext)
        {
            _fileStorage = fileStorage;
        }

        public async Task<string?> UpdateImageAsync(Guid genreId, IFormFile file)
        {
            var validationError = FileValidationHelper.ValidateImage(file);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var genre = await _context.Genres.FirstOrDefaultAsync(g => g.GenreId == genreId);
            if (genre == null)
                return null;

            var oldKey = ExtractStorageKeyIfLocal(genre.ImageUrl);

            var storageKey = await _fileStorage.SaveAsync(file, "genres", StorageVisibility.Public);
            var publicUrl = _fileStorage.GetPublicUrl(storageKey);

            genre.ImageUrl = publicUrl;
            _context.Genres.Update(genre);

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

        // List genres
        public async Task <List<Genre>> GetAllGenres()
        {
            return await _context.Genres
                .AsNoTracking()
                .Select(g => new Genre
                {
                    GenreId = g.GenreId,
                    Name = g.Name,
                    Description = g.Description,
                    ImageUrl = g.ImageUrl,
                    
                })
                .ToListAsync();
        }

        // Create genre
        public async Task<bool> CreateGenre(GenreRequestDto genreRequestDto)
        {
            var newGenre = new Genre
            {
                GenreId = Guid.NewGuid(),
                Name = genreRequestDto.Name,
                Description = genreRequestDto.Description,
                ImageUrl = genreRequestDto.ImageUrl,
                
            };
            _context.Genres.Add(newGenre);
            return await SaveAsync();
        }

        //Soft delete
        public Task<bool> DeleteGenre(Guid genreId)
        {
            return SoftDeleteAsync<Genre>(genreId);
        }
    }
}
