using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public SessionController(SessionService sessionService)
        {
            _sessionService = sessionService;
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
            return Ok(sessions);
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
            return Ok(session);
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
            var isCreated = await _sessionService.CreateSession(sessionRequestDto);
            if (isCreated)
            {
                return Ok(new { Message = "Session created Successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to create session." });
            }
        }

        // Update a session

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateSession/{sessionId}")]
        public async Task<IActionResult> UpdateSession(Guid sessionId, [FromBody] SessionRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid session data.");

            var ok = await _sessionService.UpdateSession(sessionId, dto);
            if (!ok) return NotFound(new { Message = "Session not found or update failed." });

            return Ok(new { Message = "Session updated successfully." });
        }


        // Change session state

        [HttpPatch("{sessionId}/ChangeState")]
        public async Task<IActionResult> ChangeState(Guid sessionId, [FromBody] ChangeSessionStateDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Unauthorized();

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
