using Serilog;
using System.Threading.Channels;

namespace Rolayther.Services.Mail
{
    // Un "lavoro di posta" da eseguire in background. Le email delle sessioni e il reinvio della conferma NON si
    // spediscono dentro la richiesta HTTP: la risposta all'utente parte subito, le email seguono pochi istanti dopo.
    // Così un servizio di posta lento o fuori uso non rallenta né fa fallire un'iscrizione o una creazione.
    //
    // Nei lavori ci sono solo identificativi: i dati veri si rileggono dal database al momento dell'invio.
    public abstract record MailJob
    {
        // Quante volte è già stato ritentato
        public int Attempt { get; init; }
    }

    public sealed record SessionJoinedPlayerJob(Guid SessionId, Guid PlayerId) : MailJob;

    public sealed record SessionJoinedMasterJob(Guid SessionId, Guid PlayerId) : MailJob;

    public sealed record SessionCreatedJob(Guid SessionId, bool CreatedByAdmin) : MailJob;

    public sealed record ConfirmationResendJob(string Email) : MailJob;

    // Chi sa eseguire un lavoro di posta (l'implementazione vera è MailJobProcessor)
    public interface IMailJobHandler
    {
        // true = lavoro concluso (anche se non c'era nulla da inviare); false = da ritentare
        Task<bool> ProcessAsync(MailJob job, CancellationToken ct);
    }

    public interface IMailJobQueue
    {
        // false se la coda è piena: in quel caso l'email viene scartata (e scritto un avviso nel log)
        bool TryEnqueue(MailJob job);
    }

    // Coda in memoria. Va persa se il server si riavvia (su Render gratuito succede quando va "a riposo"):
    // per email di cortesia è accettabile, e i lavori falliti vengono ritentati con pause crescenti.
    public sealed class MailJobQueue : IMailJobQueue
    {
        private readonly Channel<MailJob> _channel = Channel.CreateBounded<MailJob>(new BoundedChannelOptions(500)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        public ChannelReader<MailJob> Reader => _channel.Reader;

        public bool TryEnqueue(MailJob job)
        {
            if (_channel.Writer.TryWrite(job))
                return true;

            Log.Warning("[MAIL] Coda piena: lavoro {Job} scartato", job.GetType().Name);
            return false;
        }
    }
}
