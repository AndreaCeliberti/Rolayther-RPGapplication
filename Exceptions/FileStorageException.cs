namespace Rolayther.Exceptions
{
    // Errore dell'archivio dei file (per esempio Supabase Storage che non risponde o rifiuta il caricamento).
    // Non è un InvalidOperationException di proposito: quelle i controller le trasformano in "400 richiesta errata",
    // mentre qui il problema non è dell'utente ma del server, quindi deve finire nel log e rispondere 500.
    public class FileStorageException : Exception
    {
        public FileStorageException(string message) : base(message)
        {
        }

        public FileStorageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
