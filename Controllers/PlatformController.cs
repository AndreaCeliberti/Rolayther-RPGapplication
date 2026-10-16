using Rolayther.Models.DTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Services;

namespace Rolayther.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlatformController : ControllerBase
    {
        private readonly PlatformService _platformService;
        public PlatformController(PlatformService platformService)
        {
            _platformService = platformService;
        }

        // Get all platforms 

        
        [AllowAnonymous]
        [HttpGet("GetAllPlatforms")]
        public async Task<IActionResult> GetAllPlatforms()
        {
            var platforms = await _platformService.GetAllPlatforms();
            if (platforms == null || !platforms.Any())
            {
                return NotFound("No platforms found.");
            }
            return Ok(platforms);
        }

        // Create a new platform

        [Authorize(Roles = "Admin")]
        [HttpPost("CreatePlatform")]
        public async Task<IActionResult> CreatePlatform(PlatformRequestDto platformRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid platform data.");
            }
            var newPlatformId = await _platformService.CreatePlatform(platformRequestDto);
            if (newPlatformId != null)
            {
                // PlatformId serve al frontend per caricare subito il logo (POST {platformId}/Logo)
                return Ok(new { Message = "Platform created Successfully.", PlatformId = newPlatformId });
            }
            else
            {
                return BadRequest(new { Message = "Failed to create platform." });
            }
        }

        // Upload logo (file al posto dell'URL)
        [Authorize(Roles = "Admin")]
        [HttpPost("{platformId}/Logo")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadLogo(Guid platformId, IFormFile file)
        {
            try
            {
                var url = await _platformService.UpdateLogoAsync(platformId, file);
                if (url == null)
                    return NotFound(new { Message = "Piattaforma non trovata o salvataggio fallito." });

                return Ok(new { LogoUrl = url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Delete a platform

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeletePlatform/{platformId}")]
        public async Task<IActionResult> DeletePlatform(Guid platformId)
        {
            var isDeleted = await _platformService.DeletePlatform(platformId);
            if (isDeleted)
            {
                return Ok(new { Message = "Platform deleted Successfully." });
            }
            else
            {
                return BadRequest(new { Message = "Failed to delete platform." });
            }
        }

    }
}
