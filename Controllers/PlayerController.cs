using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public PlayerController(PlayerService playerService)
        {
            _playerService = playerService;
        }

        // Get all players

        
        [Authorize(Roles = "Admin,Master")]
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

        // Create a new player

        [AllowAnonymous]
        [HttpPost("CreatePlayer")]
        public async Task<IActionResult> CreatePlayer([FromBody]PlayerRequestDto playerRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var isCreated = await _playerService.CreatePlayer(playerRequestDto);

                return isCreated
                    ? Ok(new { Message = "Player created Successfully." })
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

            return Ok(player);
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
                email = player.Email
            });
        }

        // Get session by player

        [Authorize(Roles = "Admin, Player, Master")]
        [HttpGet("{playerId}/Sessions")]
        public async Task<IActionResult> GetPlayerSessions(Guid playerId)
        {
            var sessions = await _playerService.GetSessionsByPlayerId(playerId);

            if (sessions == null)
                return NotFound(new { Message = "Player not found." });

            return Ok(sessions);
        }


        // Update player

        [Authorize(Roles = "Admin, Player")]
        [HttpPut("UpdatePlayer/{playerId}")]
        public async Task<IActionResult> UpdatePlayer(Guid playerId, [FromBody] PlayerUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid player data.");

            var isUpdated = await _playerService.UpdatePlayer(playerId, dto);

            if (isUpdated)
                return Ok(new { Message = "Player updated successfully." });

            return NotFound(new { Message = "Player not found or update failed." });
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
