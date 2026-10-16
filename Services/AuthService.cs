using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.DTOs.Response;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;
using Rolayther.Services.Mail;
using Serilog;

namespace Rolayther.Services
{
    public class AuthService : ServiceBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly JwtTokenService _jwtTokenService;

        private readonly IConfiguration _configuration;

        private readonly EmailNotificationService _emailNotifications;

        private readonly EmailConfirmationTokenService _emailTokens;

        private readonly ResendThrottle _resendThrottle;

        private readonly IMailJobQueue _mailQueue;

        public AuthService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            JwtTokenService jwtTokenService,
            IConfiguration configuration,
            EmailNotificationService emailNotifications,
            EmailConfirmationTokenService emailTokens,
            ResendThrottle resendThrottle,
            IMailJobQueue mailQueue) : base(context)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _emailNotifications = emailNotifications;
            _emailTokens = emailTokens;
            _resendThrottle = resendThrottle;
            _mailQueue = mailQueue;
        }

        private async Task<RefreshToken> CreateAndSaveRefreshToken(ApplicationUser user)
        {
            var newToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };
            await _context.RefreshTokens.AddAsync(newToken);
            try
            {
                await SaveAsync();
                Console.WriteLine(newToken.Id);
                return newToken;
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Errore salvataggio RefreshToken");
                throw new Exception("Errore interno durante la generazione del token. Riprova più tardi.");
            }

        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var existingToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                // controllo se il refresh token esiste, se non è stato revocato, se la data di scadenza non supera la data odierna e se l'user è attivo
                if (existingToken == null
                    || existingToken.IsRevoked
                    || existingToken.ExpiresAt <= DateTime.UtcNow
                    || existingToken.User.IsDeleted)
                {
                    throw new UnauthorizedAccessException("Invalid refresh token");
                }

                // revoco il vecchio token
                existingToken.IsRevoked = true;
                _context.RefreshTokens.Update(existingToken);

                //genero un nuovo Jwt
                var roles = await _userManager.GetRolesAsync(existingToken.User);
                string newJwt = _jwtTokenService.GenerateAccessToken(existingToken.User, roles);

                //genero un nuovo refresh token
                var newRefreshToken = await CreateAndSaveRefreshToken(existingToken.User);

                //imposto la scadenza del token 
                var expiration = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!));



                return new AuthResponseDto
                {
                    AccessToken = newJwt,
                    RefreshToken = newRefreshToken.Token,
                    Role = roles.FirstOrDefault() ?? "",
                    ExpiresAt = expiration,
                    EmailConfirmed = existingToken.User.EmailConfirmed
                };

            }
            catch (UnauthorizedAccessException)
            {
                // token invalido → 401
                throw;
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Errore salvataggio refresh token");
                throw new Exception("Errore interno durante la generazione del token. Riprova più tardi.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore interno AuthService.RefreshTokenAsync");
                throw new Exception("Errore interno durante la generazione del token. Riprova più tardi.");
            }
        }

        public async Task<bool> RegisterUser(RegisterUserDto userDto)
        {
            try
            {

                ApplicationUser newUser = new ApplicationUser
                {
                    UserName = userDto.Email,
                    Email = userDto.Email,
                    Name = userDto.Name,
                    Surname = userDto.Surname,
                    CreatedAt = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString(),
                    IsDeleted = false,
                    EmailConfirmed = true,
                    LockoutEnabled = false,
                };

                IdentityResult result = await _userManager.CreateAsync(newUser, userDto.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(newUser, userDto.Role);
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Registration failed");
                return false;
            }

        }

        //service per la registrazione del player, con transazione per assicurare la coerenza dei dati tra Identity e Player.
        //Dopo il commit parte l'email di conferma: se l'invio fallisce la registrazione resta valida (EmailSent = false)
        //e l'utente può chiedere un nuovo invio.
        public async Task<RegistrationResult> RegisterPlayerAsync(PlayerRequestDto dto)
        {
            ApplicationUser? user = null;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    user = new ApplicationUser
                    {
                        UserName = dto.Email,
                        Email = dto.Email,
                        Name = dto.Name,
                        Surname = dto.Surname
                    };

                    var result = await _userManager.CreateAsync(user, dto.Password);
                    if (!result.Succeeded)
                        return RegistrationResult.Failed;

                    await _userManager.AddToRoleAsync(user, "Player");

                    var player = new Player
                    {
                        PlayerId = Guid.NewGuid(),
                        Name = dto.Name,
                        Surname = dto.Surname,
                        NickName = dto.NickName,
                        DateOfBirth = dto.DateOfBirth,
                        AvatarImgUrl = dto.AvatarImgUrl,
                        Email = dto.Email,
                        BioPlayer = dto.BioPlayer,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Players.Add(player);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return RegistrationResult.Failed;
                }
            }

            var emailSent = await _emailNotifications.SendEmailConfirmationAsync(user, isMasterRequest: false);
            return new RegistrationResult(true, emailSent);
        }

        //service per la registrazione del master, con transazione per assicurare la coerenza dei dati tra Identity e Master

        public async Task<RegistrationResult> RegisterMasterAsync(MasterRequestDto dto)
        {
            ApplicationUser? user = null;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    user = new ApplicationUser
                    {
                        UserName = dto.Email,
                        Email = dto.Email,
                        Name = dto.Name,
                        Surname = dto.Surname
                    };
                    var result = await _userManager.CreateAsync(user, dto.Password);
                    if (!result.Succeeded)
                        return RegistrationResult.Failed;
                    // Il ruolo "Master" NON viene più assegnato qui: la richiesta resta Pending
                    // finché un Admin non la approva (vedi MasterService.ApproveMaster).
                    var master = new Master
                    {
                        MasterId = Guid.NewGuid(),
                        Name = dto.Name,
                        Surname = dto.Surname,
                        NickName = dto.NickName,
                        DateOfBirth = dto.DateOfBirth,
                        AvatarImgUrl = dto.AvatarImgUrl,
                        Email = dto.Email,
                        BioMaster = dto.BioMaster,
                        CreatedAt = DateTime.UtcNow,
                        Status = ApprovalStatus.Pending
                    };
                    _context.Masters.Add(master);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return RegistrationResult.Failed;
                }
            }

            var emailSent = await _emailNotifications.SendEmailConfirmationAsync(user, isMasterRequest: true);
            return new RegistrationResult(true, emailSent);
        }

        //service per il login dell'utente
        public async Task<AuthResponseDto> Login(LoginUserDto loginUser)
        {
            try
            {

                ApplicationUser user = await _userManager.FindByEmailAsync(loginUser.Email);

                if (user is not null)
                {
                    bool passwordValid = await _userManager.CheckPasswordAsync(user, loginUser.Password);

                    if (!passwordValid)
                    {
                        throw new UnauthorizedAccessException("Password not valid");
                    }

                    var roles = await _userManager.GetRolesAsync(user);
                    string newJwt = _jwtTokenService.GenerateAccessToken(user, roles);

                    //genero un nuovo refresh token
                    var newRefreshToken = await CreateAndSaveRefreshToken(user);

                    //imposto la scadenza del token 
                    var expiration = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!));

                    if (newRefreshToken is not null)
                    {
                        return new AuthResponseDto
                        {
                            AccessToken = newJwt,
                            RefreshToken = newRefreshToken.Token,
                            Role = roles.FirstOrDefault() ?? "User",
                            ExpiresAt = expiration,
                            EmailConfirmed = user.EmailConfirmed
                        };
                    }
                    else
                    {
                        return new AuthResponseDto();
                    }

                }
                else
                {
                    throw new UnauthorizedAccessException("User not valid");
                }


            }
            catch (Exception ex)
            {
                Log.Error(ex, "Loggin failed");
                return new AuthResponseDto();
            }
        }

        //service per il logout dell'utente, impostanto il refresh token come revocato
        public async Task<bool> Logout(string refreshToken)
        {
            try
            {
                var existingToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);
                if (existingToken == null || existingToken.IsRevoked)
                {
                    return false;
                }
                existingToken.IsRevoked = true;
                _context.RefreshTokens.Update(existingToken);
                return await SaveAsync();
            }
            catch (DbUpdateException ex)
            {
                Log.Error(ex, "Errore salvataggio refresh token");
                throw new Exception("Errore interno durante la generazione del token. Riprova più tardi.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Logout failed");
                return false;
            }
        }

        // ================== CONFERMA EMAIL ==================

        // Conferma l'indirizzo con il token ricevuto per email (link /confirm-email?token=... del frontend).
        // Il token è firmato e legato all'indirizzo email: se è stato manomesso, è scaduto o l'utente nel
        // frattempo ha cambiato email, la conferma non avviene. Rifare la conferma è innocuo (AlreadyConfirmed).
        public async Task<ConfirmEmailResult> ConfirmEmailAsync(string? token)
        {
            try
            {
                var check = _emailTokens.Validate(token);

                if (check.Status == EmailTokenStatus.Invalid)
                    return ConfirmEmailResult.Invalid;

                var user = await _userManager.FindByIdAsync(check.UserId!);
                if (user == null || user.IsDeleted)
                    return ConfirmEmailResult.Invalid;

                // Il token vale solo per l'indirizzo per cui è stato creato
                if (!string.Equals(check.EmailFingerprint, EmailConfirmationTokenService.Fingerprint(user.Email ?? string.Empty), StringComparison.Ordinal))
                    return ConfirmEmailResult.Invalid;

                if (user.EmailConfirmed)
                    return ConfirmEmailResult.AlreadyConfirmed;

                if (check.Status == EmailTokenStatus.Expired)
                    return ConfirmEmailResult.Expired;

                user.EmailConfirmed = true;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    Log.Error("Conferma email non salvata per l'utente {UserId}: {Errors}", user.Id, string.Join("; ", result.Errors.Select(e => e.Description)));
                    return ConfirmEmailResult.Invalid;
                }

                return ConfirmEmailResult.Confirmed;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Conferma email fallita");
                return ConfirmEmailResult.Invalid;
            }
        }

        // Richiesta di un nuovo link di conferma. L'endpoint è pubblico, quindi:
        //  - la risposta all'esterno è SEMPRE la stessa, sia che l'indirizzo esista o no (non si può scoprire chi è registrato);
        //  - l'email vera parte in background, così anche i tempi di risposta non rivelano nulla;
        //  - ogni indirizzo ha una pausa minima e un tetto orario (vedi ResendThrottle).
        public void RequestConfirmationResend(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            if (!_resendThrottle.TryAcquire(email))
                return;

            _mailQueue.TryEnqueue(new ConfirmationResendJob(email.Trim()));
        }

        public int ResendCooldownSeconds => _resendThrottle.CooldownSeconds;

        // L'email dell'utente (già autenticato) risulta confermata?
        public async Task<bool> IsEmailConfirmedAsync(string userId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.EmailConfirmed);
        }
    }
}
