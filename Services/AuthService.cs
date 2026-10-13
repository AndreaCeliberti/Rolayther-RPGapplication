using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rolayther.Data;
using Rolayther.Models.DTOs.Request;
using Rolayther.Models.DTOs.Response;
using Rolayther.Models.Entities;
using Rolayther.Models.Enums;
using Serilog;

namespace Rolayther.Services
{
    public class AuthService : ServiceBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly JwtTokenService _jwtTokenService;

        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, JwtTokenService jwtTokenService, IConfiguration configuration) : base(context)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
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
                    ExpiresAt = expiration
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

        //service per la registrazione del player, con transazione per assicurare la coerenza dei dati tra Identity e Player
        public async Task<bool> RegisterPlayerAsync(PlayerRequestDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    Name = dto.Name,
                    Surname = dto.Surname
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    return false;

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
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        //service per la registrazione del master, con transazione per assicurare la coerenza dei dati tra Identity e Master

        public async Task<bool> RegisterMasterAsync(MasterRequestDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    Name = dto.Name,
                    Surname = dto.Surname
                };
                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    return false;
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
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
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
                            ExpiresAt = expiration
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
    }
}
