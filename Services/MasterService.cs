using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Models.Entities.Bridges;
using Rolayther.Models.Enums;

namespace Rolayther.Services
{
    public class MasterService : ServiceBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IFileStorageService _fileStorage;

        public MasterService(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, IEmailService emailService, IFileStorageService fileStorage) : base(applicationDbContext)
        {
            _userManager = userManager;
            _emailService = emailService;
            _fileStorage = fileStorage;
        }

        // Carica un nuovo avatar per il Master, sostituendo quello precedente (se caricato tramite file, non URL esterno)
        public async Task<string?> UpdateAvatarAsync(Guid masterId, IFormFile file)
        {
            var validationError = FileValidationHelper.ValidateImage(file);
            if (validationError != null)
                throw new InvalidOperationException(validationError);

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.MasterId == masterId);
            if (master == null)
                return null;

            var oldKey = ExtractStorageKeyIfLocal(master.AvatarImgUrl);

            var storageKey = await _fileStorage.SaveAsync(file, "avatars", StorageVisibility.Public);
            var publicUrl = _fileStorage.GetPublicUrl(storageKey);

            master.AvatarImgUrl = publicUrl;
            _context.Masters.Update(master);

            var saved = await SaveAsync();
            if (saved && oldKey != null)
                _fileStorage.Delete(oldKey, StorageVisibility.Public);

            return saved ? publicUrl : null;
        }

        // Se l'URL salvato punta al nostro storage locale (/uploads/...), ne ricava la storage key per poterlo eliminare.
        // Se invece è un URL esterno inserito a mano, torna null: non è un file nostro da cancellare.
        private static string? ExtractStorageKeyIfLocal(string? imgUrl)
        {
            if (string.IsNullOrWhiteSpace(imgUrl))
                return null;

            var marker = "/uploads/";
            var idx = imgUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return idx < 0 ? null : imgUrl[(idx + marker.Length)..];
        }

        // List Masters (solo quelli approvati: è la lista pubblica per la ricerca)
        public async Task<List<Master>> GetAllMasters()
        {
            return await _context.Masters
                .AsNoTracking()
                .Where(m => m.Status == ApprovalStatus.Approved)
                .Include(m => m.Sessions)
                .Select(m => new Master
                {
                    MasterId = m.MasterId,
                    Name = m.Name,
                    Surname = m.Surname,
                    NickName = m.NickName,
                    AvatarImgUrl = m.AvatarImgUrl,
                    Email = m.Email,
                    BioMaster = m.BioMaster,
                    CreatedAt = m.CreatedAt,
                    Status = m.Status,
                    Sessions = m.Sessions,
                })
                .ToListAsync();
        }

        // List Masters in attesa di approvazione (solo Admin)
        public async Task<List<Master>> GetPendingMasters()
        {
            return await _context.Masters
                .AsNoTracking()
                .Where(m => m.Status == ApprovalStatus.Pending)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // Approva una richiesta: assegna il ruolo Identity "Master" e notifica via email
        public async Task<bool> ApproveMaster(Guid masterId)
        {
            var master = await _context.Masters.FirstOrDefaultAsync(m => m.MasterId == masterId);
            if (master == null || master.Status != ApprovalStatus.Pending)
                return false;

            var user = await _userManager.FindByEmailAsync(master.Email);
            if (user == null)
                return false; // nessun account Identity collegato: non c'è nessuno a cui assegnare il ruolo

            if (!await _userManager.IsInRoleAsync(user, StringConstants.MasterRole))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, StringConstants.MasterRole);
                if (!roleResult.Succeeded)
                    return false;
            }

            master.Status = ApprovalStatus.Approved;
            _context.Masters.Update(master);
            var saved = await SaveAsync();

            if (saved)
                await _emailService.SendMasterApprovedEmailAsync(master.Email, master.Name);

            return saved;
        }

        // Rifiuta una richiesta (la riga resta in storico con Status = Rejected)
        public async Task<bool> RejectMaster(Guid masterId, string? reason)
        {
            var master = await _context.Masters.FirstOrDefaultAsync(m => m.MasterId == masterId);
            if (master == null || master.Status != ApprovalStatus.Pending)
                return false;

            master.Status = ApprovalStatus.Rejected;
            _context.Masters.Update(master);
            var saved = await SaveAsync();

            if (saved)
                await _emailService.SendMasterRejectedEmailAsync(master.Email, master.Name, reason);

            return saved;
        }

        // Create Master
        
        public async Task<Guid?> CreateMaster(MasterRequestDto masterRequestDto)
        {
            var newMaster = new Master
            {
                MasterId = Guid.NewGuid(),
                Name = masterRequestDto.Name,
                Surname = masterRequestDto.Surname,
                NickName = masterRequestDto.NickName,
                DateOfBirth = masterRequestDto.DateOfBirth,
                AvatarImgUrl = masterRequestDto.AvatarImgUrl,
                Email = masterRequestDto.Email,
                BioMaster = masterRequestDto.BioMaster,
                CreatedAt = DateTime.UtcNow,
                Status = ApprovalStatus.Pending,
                MasterGames = new List<MasterGame>(),
                Sessions = new List<Session>(),
                //Genres = new List<Genre>(),
                MasterPlatforms = new List<MasterPlatform>()
            };
            _context.Masters.Add(newMaster);
            return await SaveAsync() ? newMaster.MasterId : null;
        }

        // Get Master by Id
        public async Task<Master?> GetMasterById(Guid masterId)
        {
            return await _context.Masters
                .AsNoTracking()
                
                .FirstOrDefaultAsync(m => m.MasterId == masterId);
        }

        // Profilo pubblico: un Master approvato con le sue sessioni (serve alla pagina di dettaglio).
        // Un Master in attesa o rifiutato non esiste per il pubblico: torna null.
        public async Task<Master?> GetApprovedMasterWithSessions(Guid masterId)
        {
            return await _context.Masters
                .AsNoTracking()
                .Include(m => m.Sessions)
                .FirstOrDefaultAsync(m => m.MasterId == masterId && m.Status == ApprovalStatus.Approved);
        }

        // Get master by email

        public async Task<Master?> GetMasterByEmail(string email)
        {
            return await _context.Masters
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Email == email);
        }

        // Update Master
        public async Task<bool> UpdateMaster(Guid masterId, MasterUpdateDto masterUpdatetDto)
        {
            var master = await _context.Masters
                .Include(m => m.Sessions)
                .FirstOrDefaultAsync(m => m.MasterId == masterId);

            if (master == null)
                return false;

            master.Name = masterUpdatetDto.Name;
            master.Surname = masterUpdatetDto.Surname;
            master.NickName = masterUpdatetDto.NickName;
            master.DateOfBirth = masterUpdatetDto.DateOfBirth;
            master.AvatarImgUrl = masterUpdatetDto.AvatarImgUrl;
            master.Email = masterUpdatetDto.Email;
            master.BioMaster = masterUpdatetDto.BioMaster;

            _context.Masters.Update(master);

            return await SaveAsync();
        }



        //Soft delete
        public Task<bool> DeleteMaster(Guid masterId)
        {
            return SoftDeleteAsync<Master>(masterId);
        }
    }
}
