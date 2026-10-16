using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Services;
using System.Security.Claims;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly PlayerService _playerService;
        private readonly ResourceAccessService _access;

        public PlayerController(PlayerService playerService, ResourceAccessService access)
        {
            _playerService = playerService;
            _access = access;
        }

        // Get all players
        // Solo Admin: la lista contiene nome, cognome, email e data di nascita di TUTTI i giocatori,
        // e l'unico che la usa è la dashboard Admin.

        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllPlayers")]
        public async Task<IActionResult> GetAllPlayers()
        {
            var players = await _playerService.GetAllPlayers();
            if (players == null || !players.Any())
            {
                return NotFound("No players found.");
            }
            return Ok(players);
        }

        // Create a new player (solo Admin: la registrazione pubblica passa da /Auth/register-player,
        // questo endpoint crea una riga Player senza account Identity e non deve essere aperto a tutti)

        [Authorize(Roles = "Admin")]
        [HttpPost("CreatePlayer")]
        public async Task<IActionResult> CreatePlayer([FromBody]PlayerRequestDto playerRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var newPlayerId = await _playerService.CreatePlayer(playerRequestDto);

                return newPlayerId != null
                    ? Ok(new { Message = "Player created Successfully.", PlayerId = newPlayerId })
                    : BadRequest(new { Message = "Failed to create Player." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreatePlayer] ERROR: {ex}");

                return BadRequest(new { message = "Registrazione master fallita", detail = ex.Message });

            }
        }

        // Get player by id

        [Authorize (Roles = "Admin, Player, Master")]
        [HttpGet("GetPlayer/{playerId}")]
        public async Task<IActionResult> GetPlayerById(Guid playerId)
        {
            var player = await _playerService.GetPlayerById(playerId);

            if (player == null)
                return NotFound(new { Message = "Player not found." });

            // Admin e il Player stesso: record completo (serve ai form di modifica).
            // Chiunque altro (altri Player, Master): solo nickname e avatar.
            if (await _access.CanManagePlayer(User, playerId))
                return Ok(player);

            return Ok(player.ToPublic());
        }

        // Get current player's profile

        [Authorize(Roles = "Player,Admin,Master")]
        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var player = await _playerService.GetPlayerByEmail(email);
            if (player == null)
                return NotFound(new { Message = "Player profile not found for this user." });

            return Ok(new
            {
                playerId = player.PlayerId,
                nickName = player.NickName,
                email = player.Email,
                avatarImgUrl = player.AvatarImgUrl,
                bioPlayer = player.BioPlayer
            });
        }

        // Get session by player

        [Authorize(Roles = "Admin, Player, Master")]
        [HttpGet("{playerId}/Sessions")]
        public async Task<IActionResult> GetPlayerSessions(Guid playerId)
        {
            // Lo storico delle sessioni a cui partecipa un giocatore è privato: solo Admin e il Player stesso
            if (!await _access.CanManagePlayer(User, playerId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi vedere solo le tue sessioni." });

            var sessions = await _playerService.GetSessionsByPlayerId(playerId);

            if (sessions == null)
                return NotFound(new { Message = "Player not found." });

            // Il Player è iscritto a tutte queste sessioni, quindi vede il link; degli altri partecipanti solo nickname e avatar
            var viewer = ViewerContext.From(User);
            return Ok(sessions.Select(s => s.ToPublic(viewer)).ToList());
        }


        // Update player

        [Authorize(Roles = "Admin, Player")]
        [HttpPut("UpdatePlayer/{playerId}")]
        public async Task<IActionResult> UpdatePlayer(Guid playerId, [FromBody] PlayerUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid player data.");

            // Admin sempre; il Player solo sul PROPRIO profilo
            if (!await _access.CanManagePlayer(User, playerId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi modificare solo il tuo profilo." });

            var isUpdated = await _playerService.UpdatePlayer(playerId, dto);

            if (isUpdated)
                return Ok(new { Message = "Player updated successfully." });

            return NotFound(new { Message = "Player not found or update failed." });
        }

        // Upload avatar (file al posto dell'URL)
        [Authorize(Roles = "Admin, Player")]
        [HttpPost("{playerId}/Avatar")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadAvatar(Guid playerId, IFormFile file)
        {
            // Admin sempre; il Player solo sul PROPRIO profilo
            if (!await _access.CanManagePlayer(User, playerId))
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Puoi cambiare solo il tuo avatar." });

            try
            {
                var url = await _playerService.UpdateAvatarAsync(playerId, file);
                if (url == null)
                    return NotFound(new { Message = "Player non trovato o salvataggio fallito." });

                return Ok(new { AvatarImgUrl = url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        // Delete a player

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeletePlayer/{playerId}")]
        public async Task<IActionResult> DeletePlayer(Guid playerId)
        {
            var isDeleted = await _playerService.DeletePlayer(playerId);
            if (isDeleted)
            {
                return Ok(new { Message = "Player deleted Successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to delete player." });
            }
        }
    }
}
