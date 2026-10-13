using System.Security.Claims;

namespace Rolayther.Helpers
{
    // "Chi sta guardando la risposta": serve a decidere quali campi mostrare.
    // - Admin: vede tutto.
    // - Master proprietario / giocatore iscritto: vede anche il link della sessione.
    // - Tutti gli altri (anche anonimi): solo i dati pubblici.
    //
    // Gli endpoint [AllowAnonymous] ricevono comunque l'utente se il frontend invia un token valido,
    // quindi lo stesso endpoint può rispondere in modo diverso a chi è loggato e a chi no.
    public sealed class ViewerContext
    {
        public bool IsAdmin { get; }
        public string? Email { get; }

        // true quando il chiamante è il proprietario di TUTTE le sessioni restituite (es. "le mie sessioni")
        public bool OwnsAllSessions { get; }

        public ViewerContext(bool isAdmin, string? email, bool ownsAllSessions = false)
        {
            IsAdmin = isAdmin;
            Email = email;
            OwnsAllSessions = ownsAllSessions;
        }

        public static ViewerContext From(ClaimsPrincipal user, bool ownsAllSessions = false)
        {
            var isAdmin = user.IsInRole(StringConstants.AdminRole);
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            return new ViewerContext(isAdmin, email, ownsAllSessions);
        }

        // true se l'email passata è quella del chiamante (confronto senza distinguere maiuscole/minuscole)
        public bool IsSameEmail(string? other) =>
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(other) &&
            string.Equals(Email, other, StringComparison.OrdinalIgnoreCase);
    }
}
