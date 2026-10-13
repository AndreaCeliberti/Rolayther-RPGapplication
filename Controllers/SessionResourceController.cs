using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rolayther.Models.Enums;
using Rolayther.Services;
using System.Security.Claims;

namespace Rolayther.Controllers
{
    // File privati legati a una sessione (es. materiale di campagna): visibili solo al Master
    // proprietario, ai Player iscritti a QUELLA sessione, e all'Admin.
    [Route("api/Session/{sessionId}/Resources")]
    [ApiController]
    [Authorize]
    public class SessionResourceController : ControllerBase
    {
        private readonly SessionResourceService _resourceService;

        public SessionResourceController(SessionResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        [HttpGet]
        public async Task<IActionResult> List(Guid sessionId)
        {
            var (email, role) = GetIdentity();
            var access = await _resourceService.GetAccessLevel(sessionId, email, role);
            if (access == SessionFileAccess.None)
                return Forbid();

            var files = await _resourceService.GetForSession(sessionId);
            var result = files.Select(f => new
            {
                f.SessionResourceFileId,
                f.FileName,
                f.ContentType,
                f.FileSizeBytes,
                f.UploadedAt
            });

            return Ok(result);
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Upload(Guid sessionId, IFormFile file)
        {
            var (email, role) = GetIdentity();
            var access = await _resourceService.GetAccessLevel(sessionId, email, role);
            if (access != SessionFileAccess.Manage)
                return Forbid();

            try
            {
                var resource = await _resourceService.UploadAsync(sessionId, file);
                return Ok(new
                {
                    resource.SessionResourceFileId,
                    resource.FileName,
                    resource.ContentType,
                    resource.FileSizeBytes,
                    resource.UploadedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{fileId}/Download")]
        public async Task<IActionResult> Download(Guid sessionId, Guid fileId)
        {
            var (email, role) = GetIdentity();
            var access = await _resourceService.GetAccessLevel(sessionId, email, role);
            if (access == SessionFileAccess.None)
                return Forbid();

            var fileSessionId = await _resourceService.GetSessionIdForFile(fileId);
            if (fileSessionId != sessionId)
                return NotFound();

            var download = await _resourceService.DownloadAsync(fileId);
            if (download == null)
                return NotFound();

            return File(download.Value.Stream, download.Value.ContentType, download.Value.FileName);
        }

        [HttpDelete("{fileId}")]
        public async Task<IActionResult> Delete(Guid sessionId, Guid fileId)
        {
            var (email, role) = GetIdentity();
            var access = await _resourceService.GetAccessLevel(sessionId, email, role);
            if (access != SessionFileAccess.Manage)
                return Forbid();

            var fileSessionId = await _resourceService.GetSessionIdForFile(fileId);
            if (fileSessionId != sessionId)
                return NotFound();

            var deleted = await _resourceService.DeleteAsync(fileId);
            return deleted ? Ok(new { Message = "File eliminato." }) : NotFound();
        }

        private (string? Email, string? Role) GetIdentity()
        {
            return (User.FindFirstValue(ClaimTypes.Email), User.FindFirstValue(ClaimTypes.Role));
        }
    }
}
