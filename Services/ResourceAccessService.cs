using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rolayther.Data;
using Rolayther.Helpers;
using Rolayther.Services.Mail;
using System.Security.Claims;

namespace Rolayther.Services
{
    // Controlli di proprietà centralizzati, usati dai controller.
    // Regola unica: l'Admin può sempre; il proprietario della risorsa può gestirla; nessun altro.
    //
    // Il legame tra account e profilo è l'email nel token, la stessa che usano gli endpoint "Me":
    // Master.Email / Player.Email coincidono con l'email dell'account Identity.
    // In caso di dubbio (email assente, risorsa inesistente) il controllo risponde false: si nega, non si concede.
    public class ResourceAccessService : ServiceBase
    {
        private readonly EmailOptions _emailOptions;

        public ResourceAccessService(ApplicationDbContext applicationDbContext, IOptions<EmailOptions> emailOptions) : base(applicationDbContext)
        {
            _emailOptions = emailOptions.Value;
        }

        public bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(StringConstants.AdminRole);

        // Admin, oppure il Master che agisce sul PROPRIO profilo.
        public async Task<bool> CanManageMaster(ClaimsPrincipal user, Guid masterId)
        {
            if (IsAdmin(user)) return true;

            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _context.Masters
                .AsNoTracking()
                .AnyAsync(m => m.MasterId == masterId && m.Email == email);
        }

        // Admin, oppure il Player che agisce sul PROPRIO profilo.
        public async Task<bool> CanManagePlayer(ClaimsPrincipal user, Guid playerId)
        {
            if (IsAdmin(user)) return true;

            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _context.Players
                .AsNoTracking()
                .AnyAsync(p => p.PlayerId == playerId && p.Email == email);
        }

        // L'email dell'utente è stata confermata? Serve per iscriversi alle sessioni e per crearle.
        // L'Admin è escluso, e la regola si può spegnere da configurazione (Email:RequireConfirmedEmail = false).
        // Chi non ha confermato può comunque accedere e navigare: senza conferma non ottiene solo queste due azioni.
        public async Task<bool> HasConfirmedEmail(ClaimsPrincipal user)
        {
            if (!_emailOptions.RequireConfirmedEmail) return true;
            if (IsAdmin(user)) return true;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return false;

            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.EmailConfirmed);
        }

        // Admin, oppure il Master proprietario di QUESTA sessione.
        public async Task<bool> CanManageSession(ClaimsPrincipal user, Guid sessionId)
        {
            if (IsAdmin(user)) return true;

            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email)) return false;

            return await _context.Sessions
                .AsNoTracking()
                .AnyAsync(s => s.SessionId == sessionId && s.Master != null && s.Master.Email == email);
        }
    }
}
