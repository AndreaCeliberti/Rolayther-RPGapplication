using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Rolayther.Helpers;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.DTOs.Response;
using Rolayther.Services;
using System.Security.Claims;

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
        public async Task<IActionResult> RegisterPlayer([FromBody] RegisterPlayerRequestDto playerRequestDto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var consentError = CheckPrivacyConsent(playerRequestDto);
            if (consentError is not null)
                return consentError;

            try
            {
                var result = await _authService.RegisterPlayerAsync(playerRequestDto);

                if (!result.Success)
                    return BadRequest(new { Message = "Registrazione player fallita" });

                // EmailSent = false: account creato ma email di conferma non partita (l'utente può chiederne un'altra)
                return Ok(new { Message = "Player registrato con successo", EmailSent = result.EmailSent });
            }
            catch (Exception ex)
            {
                // Il payload NON si scrive nel log: contiene la password in chiaro
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
        public async Task<IActionResult> RegisterMaster([FromBody] RegisterMasterRequestDto masterRequestDto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var consentError = CheckPrivacyConsent(masterRequestDto);
            if (consentError is not null)
                return consentError;

            try
            {
                var result = await _authService.RegisterMasterAsync(masterRequestDto);

                if (!result.Success)
                    return BadRequest(new { Message = "Registrazione master fallita" });

                return Ok(new { Message = "Master registrato con successo", EmailSent = result.EmailSent });
            }
            catch (Exception ex)
            {
                // Il payload NON si scrive nel log: contiene la password in chiaro
                Console.WriteLine($"[RegisterMaster] ERROR: {ex}");

                return BadRequest(new
                {
                    Message = "Registrazione master fallita",
                    Detail = ex.Message
                });
            }
        }

        // Consenso al trattamento dei dati personali (informativa privacy):
        // - chi si registra da solo deve darlo: senza, la registrazione non parte, anche se qualcuno salta la casella del sito;
        // - un Admin che crea l'account per un'altra persona non può dare il consenso al posto suo: in quel caso
        //   non si chiede e non si registra, e l'account resta senza consenso.
        // Gli endpoint di registrazione sono [AllowAnonymous], ma se la richiesta porta un token valido
        // l'utente viene riconosciuto lo stesso: è così che si distingue l'Admin.
        // Ritorna la risposta da dare se la registrazione va fermata, altrimenti null.
        private IActionResult? CheckPrivacyConsent(IPrivacyConsentRequest dto)
        {
            if (User.IsInRole(StringConstants.AdminRole))
            {
                dto.PrivacyConsent = false;
                dto.PrivacyPolicyVersion = null;
                return null;
            }

            if (dto.PrivacyConsent)
                return null;

            // Un Admin con il token scaduto sembra un visitatore: 401 (e non "manca il consenso"), così il sito
            // rinnova il token da solo e ripete la richiesta. Un visitatore vero non manda alcun token.
            if (Request.Headers.ContainsKey("Authorization") && User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            return BadRequest(new
            {
                Message = "Per registrarti devi acconsentire al trattamento dei dati personali.",
                Code = "PrivacyConsentRequired"
            });
        }



        // ================== CONFERMA EMAIL ==================

        // Chiamato dalla pagina /confirm-email del frontend con il token del link ricevuto per email.
        // È un POST (e non un GET sul link dell'email) perché alcuni antivirus/anteprime aprono in automatico
        // i link delle email: così una semplice anteprima non conferma nulla, serve una pagina vera.
        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _authService.ConfirmEmailAsync(dto.Token);

            return result switch
            {
                ConfirmEmailResult.Confirmed => Ok(new { Message = "Email confermata: ora puoi usare tutte le funzioni di Rolayther.", Status = "confirmed" }),
                ConfirmEmailResult.AlreadyConfirmed => Ok(new { Message = "Questo indirizzo era già stato confermato.", Status = "already-confirmed" }),
                ConfirmEmailResult.Expired => BadRequest(new { Message = "Il link è scaduto. Richiedine uno nuovo e riprova.", Code = "TokenExpired" }),
                _ => BadRequest(new { Message = "Il link non è valido. Controlla di averlo copiato per intero oppure richiedine uno nuovo.", Code = "TokenInvalid" })
            };
        }

        // Nuovo link di conferma. Pubblico (anche chi non ha ancora fatto login deve poterlo chiedere), quindi
        // la risposta è sempre la stessa e ha un limite di richieste tutto suo.
        [AllowAnonymous]
        [EnableRateLimiting("ResendPolicy")]
        [HttpPost("resend-confirmation")]
        public IActionResult ResendConfirmation([FromBody] ResendConfirmationRequestDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            _authService.RequestConfirmationResend(dto.Email);

            return Ok(new
            {
                Message = "Se l'indirizzo è registrato e non ancora confermato, a breve riceverai una nuova email.",
                CooldownSeconds = _authService.ResendCooldownSeconds
            });
        }

        // Stato attuale della conferma per l'utente loggato (il frontend lo usa per mostrare o nascondere l'avviso)
        [Authorize]
        [DisableRateLimiting]
        [HttpGet("email-status")]
        public async Task<IActionResult> EmailStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            return Ok(new { EmailConfirmed = await _authService.IsEmailConfirmedAsync(userId) });
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

