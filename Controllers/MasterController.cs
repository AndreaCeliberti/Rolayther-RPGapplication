using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Models.DTOs.Request;
using Rolayther.Services;
using System.Security.Claims;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterController : ControllerBase
    {
        private readonly MasterService _masterService;
        private readonly SessionService _sessionService;
        private readonly ResourceAccessService _access;

        public MasterController(MasterService masterService, SessionService sessionService, ResourceAccessService access)
        {
            _masterService = masterService;
            _sessionService = sessionService;
            _access = access;
        }

        //Get all master


        [AllowAnonymous]
        [HttpGet("GetAllMasters")]
        public async Task<IActionResult> GetAllMasters()
        {
            var masters = await _masterService.GetAllMasters();
            if (masters == null || !masters.Any())
            {
                return NotFound("No masters found.");
            }
            return Ok(masters);
        }

        // Create a new master (solo Admin: la registrazione pubblica passa da /Auth/register-master,
        // questo endpoint crea una riga Master senza account Identity e non deve essere aperto a tutti)

        [Authorize(Roles = "Admin")]
        [HttpPost("CreateMaster")]
        public async Task<IActionResult> CreateMaster([FromBody]MasterRequestDto masterRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var isCreated = await _masterService.CreateMaster(masterRequestDto);

                return isCreated
                    ? Ok(new { Message = "Master created Successfully." })
                    : BadRequest(new { Message = "Failed to create master." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateMaster] ERROR: {ex}");

                return BadRequest(new { message = "Registrazione master fallita", detail = ex.Message });
            }
        }

        // Get current masters's profile

        
        [AllowAnonymous]
        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var master = await _masterService.GetMasterByEmail(email);
            if (master == null)
                return NotFound(new { Message = "Master profile not found for this user." });

            return Ok(new
            {
                master.MasterId,
                master.Name,
                master.Surname,
                master.NickName,
                master.Email,
                master.AvatarImgUrl,
                master.BioMaster
            });
        }

        // Get master by id

        [Authorize]
        [HttpGet("GetMaster/{masterId}")]
        public async Task<IActionResult> GetMasterById(Guid masterId)
        {
            var master = await _masterService.GetMasterById(masterId);

            if (master == null)
                return NotFound(new { Message = "Master not found." });

            return Ok(master);
        }

        [AllowAnonymous]
        [HttpGet("{masterId}/Sessions")]
        public async Task<IActionResult> GetSessionsByMaster(Guid masterId)
        {
            // (opzionale ma consigliato) verifica che il master esista
            var master = await _masterService.GetMasterById(masterId);
            if (master == null)
                return NotFound(new { Message = "Master not found." });

            var sessions = await _sessionService.GetSessionsByMasterId(masterId);
            return Ok(sessions);
        }

        [Authorize(Roles = "Admin, Master")]
        [HttpGet("Me/Sessions")]
        public async Task<IActionResult> GetMySessions()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var master = await _masterService.GetMasterByEmail(email);
            if (master == null)
                return NotFound(new { Message = "Master profile not found for this user." });

            var sessions = await _sessionService.GetSessionsByMasterId(master.MasterId);
            return Ok(sessions);
        }

        // Update master

        [Authorize(Roles = "Admin, Master")]
        [HttpPut("UpdateMaster/{masterId}")]
        public async Task<IActionResult> UpdateMaster(Guid masterId, MasterUpdateDto masterUpdatetDto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid master data.");

            // Admin sempre; il Master solo sul PROPRIO profilo
            if (!await _access.CanManageMaster(User, masterId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi modificare solo il tuo profilo." });

            var isUpdated = await _masterService.UpdateMaster(masterId, masterUpdatetDto);

            if (isUpdated)
                return Ok(new { Message = "Master updated successfully." });

            return NotFound(new { Message = "Master not found or update failed." });
        }

        // Upload avatar (file al posto dell'URL)
        [Authorize(Roles = "Admin, Master")]
        [HttpPost("{masterId}/Avatar")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadAvatar(Guid masterId, IFormFile file)
        {
            // Admin sempre; il Master solo sul PROPRIO profilo
            if (!await _access.CanManageMaster(User, masterId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi cambiare solo il tuo avatar." });

            try
            {
                var url = await _masterService.UpdateAvatarAsync(masterId, file);
                if (url == null)
                    return NotFound(new { Message = "Master non trovato o salvataggio fallito." });

                return Ok(new { AvatarImgUrl = url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        // ================== ADMIN: RICHIESTE MASTER IN ATTESA ==================

        [Authorize(Roles = "Admin")]
        [HttpGet("PendingRequests")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var pending = await _masterService.GetPendingMasters();
            return Ok(pending);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{masterId}/Approve")]
        public async Task<IActionResult> ApproveMaster(Guid masterId)
        {
            var approved = await _masterService.ApproveMaster(masterId);
            return approved
                ? Ok(new { Message = "Richiesta approvata: il ruolo Master è stato assegnato." })
                : BadRequest(new { Message = "Richiesta non trovata, già gestita, o nessun account collegato." });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{masterId}/Reject")]
        public async Task<IActionResult> RejectMaster(Guid masterId, [FromBody] MasterApprovalActionDto? dto)
        {
            var rejected = await _masterService.RejectMaster(masterId, dto?.Reason);
            return rejected
                ? Ok(new { Message = "Richiesta rifiutata." })
                : BadRequest(new { Message = "Richiesta non trovata o già gestita." });
        }

        // Delete a master
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteMaster/{masterId}")]
        public async Task<IActionResult> DeleteMaster(Guid masterId)
        {
            var isDeleted = await _masterService.DeleteMaster(masterId);
            if (isDeleted)
            {
                return Ok(new { Message = "Master deleted successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to delete master." });
            }
        }
    }
}
