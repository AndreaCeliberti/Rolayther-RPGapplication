    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Rolayther.Models.DTOs.Request;
    using Rolayther.Services;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterGamesController : ControllerBase
    {
        private readonly MasterGamesService _service;
        private readonly ResourceAccessService _access;

        public MasterGamesController(MasterGamesService service, ResourceAccessService access)
        {
            _service = service;
            _access = access;
        }

        // GET /api/MasterGames/{masterId}
        [Authorize(Roles = "Admin, Master")]
        [HttpGet("{masterId}")]
        public async Task<IActionResult> GetMasterGames(Guid masterId)
        {
            var games = await _service.GetMasterGames(masterId);
            return Ok(games);
        }

        // PUT /api/MasterGames/{masterId}
        [Authorize(Roles = "Admin, Master")]
        [HttpPut("{masterId}")]
        public async Task<IActionResult> UpdateMasterGames(Guid masterId, [FromBody] UpdateMasterGamesDto dto)
        {
            if (dto == null || dto.GameIds == null) return BadRequest("Dati mancanti.");

            // Admin sempre; il Master solo sul PROPRIO elenco di giochi
            if (!await _access.CanManageMaster(User, masterId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi modificare solo i tuoi giochi." });

            var ok = await _service.UpdateMasterGames(masterId, dto.GameIds);
            if (!ok) return NotFound("Master non trovato.");

            return Ok(new { Message = "Giochi del master aggiornati correttamente." });
        }
    }
}
