namespace Rolayther.Models.DTOs.Response
{
    // Esito di una registrazione. EmailSent = false significa: account creato, ma l'email di conferma
    // non è partita (servizio di posta in difficoltà) e l'utente deve richiederne una nuova.
    public sealed record RegistrationResult(bool Success, bool EmailSent)
    {
        public static RegistrationResult Failed => new(false, false);
    }

    // Esito di un'approvazione o di un rifiuto di una richiesta Master.
    // EmailSent = false: decisione registrata, ma l'email al candidato non è partita (l'Admin ne viene avvisato).
    public sealed record MasterDecisionResult(bool Success, bool EmailSent)
    {
        public static MasterDecisionResult Failed => new(false, false);
    }

    public enum ConfirmEmailResult
    {
        Confirmed,
        AlreadyConfirmed,
        Expired,
        Invalid
    }
}
