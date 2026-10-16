namespace Rolayther.Models.Enums
{
    public enum SessionFileAccess
    {
        None,    // nessun accesso
        View,    // player iscritto: può vedere/scaricare
        Manage   // master proprietario o Admin: può anche caricare/eliminare
    }
}
