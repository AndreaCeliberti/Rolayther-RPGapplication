namespace Rolayther.Models.Enums
{
    // Public: immagini di profilo/copertina, servite direttamente via file statici (chiunque abbia l'URL le vede)
    // Private: file allegati a una sessione, scaricabili solo tramite endpoint autenticato con controllo permessi
    public enum StorageVisibility
    {
        Public,
        Private
    }
}
