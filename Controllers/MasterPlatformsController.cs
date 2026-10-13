using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Models.DTOs.Request;
using Rolayther.Services;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterPlatformsController : ControllerBase
    {
        private readonly MasterPlatformsService _service;
        private readonly ResourceAccessService _access;

        public MasterPlatformsController(MasterPlatformsService service, ResourceAccessService access)
        {
            _service = service;
            _access = access;
        }

        [Authorize(Roles = "Admin, Master")]
        [HttpGet("{masterId}")]
        public async Task<IActionResult> GetMasterPlatforms(Guid masterId)
        {
            var platforms = await _service.GetMasterPlatforms(masterId);
            return Ok(platforms);
        }

        [Authorize(Roles = "Admin, Master")]
        [HttpPut("{masterId}")]
        public async Task<IActionResult> UpdateMasterPlatforms(Guid masterId, [FromBody] UpdateMasterPlatformsDto dto)
        {
            if (dto == null || dto.PlatformIds == null) return BadRequest("Dati mancanti.");

            // Admin sempre; il Master solo sul PROPRIO elenco di piattaforme
            if (!await _access.CanManageMaster(User, masterId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi modificare solo le tue piattaforme." });

            var ok = await _service.UpdateMasterPlatforms(masterId, dto.PlatformIds);
            if (!ok) return NotFound("Master non trovato.");
            return Ok(new { Message = "Piattaforme del master aggiornate correttamente." });
        }
    }
}
