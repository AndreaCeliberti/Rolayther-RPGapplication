using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Models.Entities.Bridges;

namespace Rolayther.Services
{
    public class MasterService : ServiceBase
    {
        public MasterService(ApplicationDbContext applicationDbContext) : base(applicationDbContext) { }

        // List Masters
        public async Task<List<Master>> GetAllMasters()
        {
            return await _context.Masters
                .AsNoTracking()
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
                    Sessions = m.Sessions,
                })
                .ToListAsync();
        }

        // Create Master
        
        public async Task<bool> CreateMaster(MasterRequestDto masterRequestDto)
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
                MasterGames = new List<MasterGame>(),
                Sessions = new List<Session>(),
                //Genres = new List<Genre>(),
                MasterPlatforms = new List<MasterPlatform>()
            };
            _context.Masters.Add(newMaster);
            return await SaveAsync();
        }

        // Get Master by Id
        public async Task<Master?> GetMasterById(Guid masterId)
        {
            return await _context.Masters
                .AsNoTracking()
                
                .FirstOrDefaultAsync(m => m.MasterId == masterId);
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
