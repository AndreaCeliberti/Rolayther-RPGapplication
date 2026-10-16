using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public class PlatformService : ServiceBase
    {
        private readonly IFileStorageService _fileStorage;

        public PlatformService(ApplicationDbContext applicationDbContext, IFileStorageService fileStorage) : base(applicationDbContext)
        {
            _fileStorage = fileStorage;
        }

        public async Task<string?> UpdateLogoAsync(Guid platformId, IFormFile file)
        {
            var validationError = FileValidationHelper.ValidateImage(file);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var platform = await _context.Platforms.FirstOrDefaultAsync(p => p.PlatformId == platformId);
            if (platform == null)
                return null;

            var oldKey = ExtractStorageKeyIfLocal(platform.LogoUrl);

            var storageKey = await _fileStorage.SaveAsync(file, "platforms", StorageVisibility.Public);
            var publicUrl = _fileStorage.GetPublicUrl(storageKey);

            platform.LogoUrl = publicUrl;
            _context.Platforms.Update(platform);

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

        // List Platforms   
        public async Task<List<Platform>> GetAllPlatforms()
        {
            return await _context.Platforms
                .AsNoTracking()
                .Select(p => new Platform
                {
                    PlatformId = p.PlatformId,
                    Name = p.Name,
                    Description = p.Description,
                    LogoUrl = p.LogoUrl
                })
                .ToListAsync();
        }

        // Create Platform
        public async Task<Guid?> CreatePlatform(PlatformRequestDto platformRequestDto)
        {
            var newPlatform = new Platform
            {
                PlatformId = Guid.NewGuid(),
                Name =  platformRequestDto.Name,
                Description = platformRequestDto.Description,
                LogoUrl = platformRequestDto.LogoUrl,
                //MasterId = platformRequestDto.MasterId
            };
            _context.Platforms.Add(newPlatform);

            // Restituisce l'id della piattaforma creata (serve al frontend per caricare subito il logo)
            return await SaveAsync() ? newPlatform.PlatformId : null;
        }

        //Soft delete

        public Task<bool> DeletePlatform(Guid platformId)
        {
            return SoftDeleteAsync<Platform>(platformId);
        }
    }
}
