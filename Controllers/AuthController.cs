using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.DTOs.Response;
using Rolayther.Services;

namespace Rolayther.Controllers
{
   
        [Route("api/[controller]")]
        [ApiController]
        [EnableRateLimiting("AuthPolicy")]
        public class AuthController : ControllerBase
        {
            private readonly AuthService _authService;

            public AuthController(AuthService authService)
            {
                _authService = authService;
            }

        // ================== REGISTER ==================
        [AllowAnonymous]
        [HttpPost("register-player")]
        public async Task<IActionResult> RegisterPlayer([FromBody] PlayerRequestDto playerRequestDto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var result = await _authService.RegisterPlayerAsync(playerRequestDto);

                if (!result)
                    return BadRequest(new { Message = "Registrazione player fallita" });

                return Ok(new { Message = "Player registrato con successo" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegisterPlayer] Payload: {System.Text.Json.JsonSerializer.Serialize(playerRequestDto)}");
                Console.WriteLine($"[RegisterPlayer] ERROR: {ex}");

                return BadRequest(new
                {
                    Message = "Registrazione player fallita",
                    Detail = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("register-master")]
        public async Task<IActionResult> RegisterMaster([FromBody] MasterRequestDto masterRequestDto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var result = await _authService.RegisterMasterAsync(masterRequestDto);

                if (!result)
                    return BadRequest(new { Message = "Registrazione master fallita" });

                return Ok(new { Message = "Master registrato con successo" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegisterMaster] Payload: {System.Text.Json.JsonSerializer.Serialize(masterRequestDto)}");
                Console.WriteLine($"[RegisterMaster] ERROR: {ex}");

                return BadRequest(new
                {
                    Message = "Registrazione master fallita",
                    Detail = ex.Message
                });
            }
        }



        // ================== LOGIN ==================
        [HttpPost("login")]
            public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                try
                {
                    AuthResponseDto authResponse = await _authService.Login(loginDto);

                    if (string.IsNullOrEmpty(authResponse.AccessToken))
                        return Unauthorized(new { Message = "Email o password errate" });

                    return Ok(authResponse);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // ================== REFRESH TOKEN ==================
            [HttpPost("refresh")]
            public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto refreshDto)
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                try
                {
                    AuthResponseDto authResponse = await _authService.RefreshTokenAsync(refreshDto.RefreshToken);
                    return Ok(authResponse);
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized(new { Message = "Refresh token non valido o scaduto" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // ================== LOGOUT ==================
            //[Authorize]
            [HttpPost("logout")]
            public async Task<IActionResult> Logout(LogoutDto logout)
            {
                try
                {

                    await _authService.Logout(logout.RefreshToken);

                    return Ok(new { Message = "Logout effettuato con successo" });
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized(new { Message = "Refresh token non valido o scaduto" });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }
        }
    }

