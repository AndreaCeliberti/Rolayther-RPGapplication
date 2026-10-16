using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Services;
using System.Data;
using System.Security.Claims;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly SessionService _sessionService;
        private readonly ResourceAccessService _access;

        public SessionController(SessionService sessionService, ResourceAccessService access)
        {
            _sessionService = sessionService;
            _access = access;
        }

        // Get all sessions 

        [Authorize]
        [AllowAnonymous]
        [HttpGet("GetAllSessions")]
        public async Task<IActionResult> GetAllSessions()
        {
            var sessions = await _sessionService.GetAllSessions();
            if (sessions == null || !sessions.Any())
            {
                return NotFound("No sessions found.");
            }

            // Admin: risposta completa (la dashboard usa email e nomi dei player).
            // Tutti gli altri, anche anonimi: solo dati pubblici (niente email, date di nascita, nomi reali;
            // il link della sessione solo a chi è iscritto o la gestisce).
            if (_access.IsAdmin(User))
                return Ok(sessions);

            var viewer = ViewerContext.From(User);
            return Ok(sessions.Select(s => s.ToPublic(viewer)).ToList());
        }

        // Get session by id
        [AllowAnonymous]
        [HttpGet("GetSessionById/{sessionId}")]
        public async Task<IActionResult> GetSessionById(Guid sessionId)
        {
            var session = await _sessionService.GetSessionById(sessionId);
            if (session == null)
            {
                return NotFound("Session not found.");
            }

            if (_access.IsAdmin(User))
                return Ok(session);

            return Ok(session.ToPublic(ViewerContext.From(User)));
        }


        // Create a new session

        [Authorize(Roles = "Admin, Master")]
        [HttpPost("CreateSession")]
        public async Task<IActionResult> CreateSession(SessionRequestDto sessionRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid session data.");
            }
            // L'Admin può creare per qualsiasi Master; un Master solo a proprio nome
            if (!await _access.CanManageMaster(User, sessionRequestDto.MasterId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi creare sessioni solo a tuo nome." });

            var newSessionId = await _sessionService.CreateSession(sessionRequestDto);
            if (newSessionId != null)
            {
                // SessionId serve al frontend per caricare subito la cover (POST {sessionId}/Cover)
                return Ok(new { Message = "Session created Successfully.", SessionId = newSessionId });
            }
            else
            {
                return BadRequest(new { Message = "Failed to create session." });
            }
        }

        // Update a session

        [Authorize(Roles = "Admin, Master")]
        [HttpPut("UpdateSession/{sessionId}")]
        public async Task<IActionResult> UpdateSession(Guid sessionId, [FromBody] SessionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid session data.");

            // Admin sempre; il Master solo sulla PROPRIA sessione
            if (!await _access.CanManageSession(User, sessionId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi modificare solo le tue sessioni." });

            // Solo l'Admin può assegnare la sessione a un altro Master
            if (!_access.IsAdmin(User))
            {
                var currentMasterId = await _sessionService.GetSessionMasterId(sessionId);
                if (currentMasterId != dto.MasterId)
                    return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Solo l'Admin può assegnare la sessione a un altro Master." });
            }

            var ok = await _sessionService.UpdateSession(sessionId, dto);
            if (!ok) return NotFound(new { Message = "Session not found or update failed." });

            return Ok(new { Message = "Session updated successfully." });
        }

        // Upload cover (file al posto dell'URL)
        [Authorize(Roles = "Admin, Master")]
        [HttpPost("{sessionId}/Cover")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadCover(Guid sessionId, IFormFile file)
        {
            // Admin sempre; il Master solo sulla PROPRIA sessione
            if (!await _access.CanManageSession(User, sessionId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi cambiare la cover solo delle tue sessioni." });

            try
            {
                var url = await _sessionService.UpdateCoverAsync(sessionId, file);
                if (url == null)
                    return NotFound(new { Message = "Sessione non trovata o salvataggio fallito." });

                return Ok(new { CoverImgUrl = url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        // Change session state

        [Authorize(Roles = "Admin, Master")]
        [HttpPatch("{sessionId}/ChangeState")]
        public async Task<IActionResult> ChangeState(Guid sessionId, [FromBody] ChangeSessionStateDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Unauthorized();

            // Admin sempre; il Master solo sulla PROPRIA sessione
            if (!await _access.CanManageSession(User, sessionId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi cambiare lo stato solo delle tue sessioni." });

            await _sessionService.ChangeSessionState(sessionId, dto.NewState, userId, role, dto.Reason);

            return Ok("Stato aggiornato correttamente");

        }



        // Join session

        [Authorize]
        [HttpPost("{sessionId}/join/{playerId}")]
        public async Task<IActionResult> Join(Guid sessionId, Guid playerId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                    return Unauthorized();

                // Admin sempre; un Player può iscrivere solo SE STESSO
                if (!await _access.CanManagePlayer(User, playerId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Puoi iscrivere solo te stesso." });

                var ok = await _sessionService.AddPlayerToSession(sessionId, playerId, userId, role);
                if (!ok) return NotFound(new { message = "Session o player non trovati" });

                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
           // catch (DbUpdateConcurrencyException)
            //{
              //  return Conflict(new { message = "Qualcuno ha appena occupato l’ultimo posto. Aggiorna e riprova." });
            //}
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return Conflict(new { message = inner });
            }
        }



        // Leave session

       

    [Authorize]
    [HttpPost("{sessionId}/Leave/{playerId}")]
    public async Task<IActionResult> LeaveSession(Guid sessionId, Guid playerId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            return Unauthorized();

        // Admin sempre; un Player può far uscire dalla sessione solo SE STESSO
        if (!await _access.CanManagePlayer(User, playerId))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Puoi uscire solo tu dalla sessione." });

        try
        {
            var result = await _sessionService.RemovePlayerFromSession(sessionId, playerId, userId, role);

            if (!result)
                return BadRequest("Unable to leave session");

            return Ok(new { Message = "Player removed from session" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Conflitto durante l’uscita dalla sessione. Riprova." });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Conflitto durante l’uscita dalla sessione. Riprova." });
        }
    }


    // Delete a session

    [Authorize(Roles = "Admin, Master")]
        [HttpDelete("DeleteSession/{sessionId}")]
        public async Task<IActionResult> DeleteSession(Guid sessionId)
        {
            // Admin sempre; il Master solo sulla PROPRIA sessione
            if (!await _access.CanManageSession(User, sessionId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi eliminare solo le tue sessioni." });

            var isDeleted = await _sessionService.DeleteSession(sessionId);
            if (isDeleted)
            {
                return Ok(new { Message = "Session deleted Successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to delete session." });
            }
        }
    }
}
