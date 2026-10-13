using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.Entities;
using Rolayther.Services;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class GameController : ControllerBase
    {
       private readonly GameService _gameService;

        public GameController(GameService gameService)
         {
              _gameService = gameService;
        }

        // Get all games

        [Authorize]
        [AllowAnonymous]
        [HttpGet("GetAllGames")]
        public async Task<IActionResult> GetAllGames()
        {
            var games = await _gameService.GetAllGames();
            if (games == null || !games.Any())
            {
                return NotFound("No games found.");
            }
            return Ok(games);
        }

        // Create a new game

        [Authorize(Roles = "Admin")]
        [HttpPost("CreateGame")]
        
        public async Task<IActionResult> CreateGame( [FromBody]GameRequestDto gameRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid game data.");
            }
            var ISCreated = await _gameService.CreateGame(gameRequestDto);
            if (ISCreated)
            {
                return Ok(new { Message = "Game created Succesfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to create game." });
            }
        }

        // Upload cover (file al posto dell'URL)
        [Authorize(Roles = "Admin")]
        [HttpPost("{gameId}/Cover")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadCover(Guid gameId, IFormFile file)
        {
            try
            {
                var url = await _gameService.UpdateCoverAsync(gameId, file);
                if (url == null)
                    return NotFound(new { Message = "Gioco non trovato o salvataggio fallito." });

                return Ok(new { CoverImageUrl = url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Delete a game

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteGame/{gameId}")]
        public async Task<IActionResult> DeleteGame(Guid gameId)
        {
            var isDeleted = await _gameService.DeleteGame(gameId);
            if (isDeleted)
            {
                return Ok(new { Message = "Game deleted Successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to delete game." });
            }
        }
    }
}
