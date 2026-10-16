using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Enums;
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

            // Admin: lista completa (la dashboard mostra email e nomi).
            // Tutti gli altri, anche anonimi: solo il profilo pubblico (nickname, avatar, bio, sessioni).
            if (_access.IsAdmin(User))
                return Ok(masters);

            return Ok(masters.Select(m => m.ToPublic()).ToList());
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
                var newMasterId = await _masterService.CreateMaster(masterRequestDto);

                return newMasterId != null
                    ? Ok(new { Message = "Master created Successfully.", MasterId = newMasterId })
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

        // Pubblico come la lista dei Master (la pagina /masters/:id è raggiungibile senza login).
        // Admin e il Master stesso ricevono il record completo (serve ai form di modifica);
        // tutti gli altri ricevono il solo profilo pubblico, e solo se il Master è approvato.
        [AllowAnonymous]
        [HttpGet("GetMaster/{masterId}")]
        public async Task<IActionResult> GetMasterById(Guid masterId)
        {
            if (await _access.CanManageMaster(User, masterId))
            {
                var full = await _masterService.GetMasterById(masterId);
                if (full == null)
                    return NotFound(new { Message = "Master not found." });

                return Ok(full);
            }

            var publicMaster = await _masterService.GetApprovedMasterWithSessions(masterId);
            if (publicMaster == null)
                return NotFound(new { Message = "Master not found." });

            return Ok(publicMaster.ToPublic());
        }

        [AllowAnonymous]
        [HttpGet("{masterId}/Sessions")]
        public async Task<IActionResult> GetSessionsByMaster(Guid masterId)
        {
            // verifica che il master esista
            var master = await _masterService.GetMasterById(masterId);
            if (master == null)
                return NotFound(new { Message = "Master not found." });

            // Admin e il Master stesso gestiscono queste sessioni; per il pubblico un Master non approvato non esiste
            var canManage = await _access.CanManageMaster(User, masterId);
            if (!canManage && master.Status != ApprovalStatus.Approved)
                return NotFound(new { Message = "Master not found." });

            var sessions = await _sessionService.GetSessionsByMasterId(masterId);

            // Niente email né dati personali dei player; il link della sessione solo a chi la gestisce o è iscritto
            var viewer = ViewerContext.From(User, ownsAllSessions: canManage);
            return Ok(sessions.Select(s => s.ToPublic(viewer)).ToList());
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

            // Sono le sessioni del chiamante: link visibile, ma niente dati personali dei player
            var viewer = ViewerContext.From(User, ownsAllSessions: true);
            return Ok(sessions.Select(s => s.ToPublic(viewer)).ToList());
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

        // Upload avatar (file al posto dell'URL).
        // Basta essere autenticati: chi può davvero cambiare l'avatar lo decide CanManageMaster (Admin o il Master stesso).
        // Il ruolo "Master" non serve: chi si è appena registrato è ancora in attesa di approvazione e non lo ha,
        // ma deve poter caricare la propria foto.
        [Authorize]
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

        // Approvazione. Corpo facoltativo: { message } = messaggio di benvenuto per il nuovo Master (finisce nell'email).
        // La risposta include emailSent: false significa "approvato, ma l'email non è partita".
        [Authorize(Roles = "Admin")]
        [HttpPut("{masterId}/Approve")]
        public async Task<IActionResult> ApproveMaster(Guid masterId, [FromBody] MasterApprovalActionDto? dto)
        {
            var result = await _masterService.ApproveMaster(masterId, dto?.Message);
            if (!result.Success)
                return BadRequest(new { Message = "Richiesta non trovata, già gestita, o nessun account collegato." });

            return Ok(new
            {
                Message = result.EmailSent
                    ? "Richiesta approvata: il ruolo Master è stato assegnato e l'email di benvenuto è partita."
                    : "Richiesta approvata: il ruolo Master è stato assegnato, ma l'email di benvenuto NON è partita.",
                EmailSent = result.EmailSent
            });
        }

        // Rifiuto. Il motivo è OBBLIGATORIO: viene spiegato al candidato nell'email.
        [Authorize(Roles = "Admin")]
        [HttpPut("{masterId}/Reject")]
        public async Task<IActionResult> RejectMaster(Guid masterId, [FromBody] MasterApprovalActionDto? dto)
        {
            var reason = dto?.Reason?.Trim();
            if (string.IsNullOrEmpty(reason))
                return BadRequest(new { Message = "Indica il motivo del rifiuto: verrà spiegato al candidato nell'email." });

            var result = await _masterService.RejectMaster(masterId, reason);
            if (!result.Success)
                return BadRequest(new { Message = "Richiesta non trovata o già gestita." });

            return Ok(new
            {
                Message = result.EmailSent
                    ? "Richiesta rifiutata: il candidato è stato avvisato per email."
                    : "Richiesta rifiutata, ma l'email al candidato NON è partita.",
                EmailSent = result.EmailSent
            });
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
