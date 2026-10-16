using Serilog;

namespace Rolayther.Services.Mail
{
    // Servizio in background che svuota la coda delle email. Per ogni lavoro apre un "ambito" nuovo dell'iniezione
    // delle dipendenze (con il suo DbContext), perché il lavoro gira DOPO la fine della richiesta HTTP che lo ha creato.
    public sealed class MailJobWorker : BackgroundService
    {
        // Pause prima di ciascun nuovo tentativo (dopo il primo errore, il secondo, il terzo); poi si rinuncia
        private static readonly TimeSpan[] DefaultRetryDelays =
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2)
        };

        private readonly MailJobQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly TimeSpan[] _retryDelays;
        private CancellationToken _stopping;

        public MailJobWorker(MailJobQueue queue, IServiceScopeFactory scopes)
            : this(queue, scopes, DefaultRetryDelays)
        {
        }

        // Usato anche dai test, per non aspettare davvero secondi e minuti
        internal MailJobWorker(MailJobQueue queue, IServiceScopeFactory scopes, TimeSpan[] retryDelays)
        {
            _queue = queue;
            _scopes = scopes;
            _retryDelays = retryDelays;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _stopping = stoppingToken;

            try
            {
                await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
                    await ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // arresto del server: si passa allo svuotamento qui sotto
            }

            // Arresto ordinato: si provano a spedire le email rimaste in coda, ma al massimo per 10 secondi
            using var drain = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (_queue.Reader.TryRead(out var pending))
            {
                if (drain.IsCancellationRequested)
                {
                    Log.Warning("[MAIL] Arresto del server: alcune email in coda non sono state inviate");
                    break;
                }

                await ProcessAsync(pending, drain.Token, allowRetry: false);
            }
        }

        private async Task ProcessAsync(MailJob job, CancellationToken ct, bool allowRetry = true)
        {
            bool done;
            try
            {
                using var scope = _scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IMailJobHandler>();
                done = await processor.ProcessAsync(job, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[MAIL] Errore nell'elaborazione del lavoro {Job}", job.GetType().Name);
                done = false;
            }

            if (done)
                return;

            if (allowRetry && job.Attempt < _retryDelays.Length)
            {
                var next = job with { Attempt = job.Attempt + 1 };
                var delay = _retryDelays[job.Attempt];
                Log.Warning("[MAIL] Lavoro {Job} non riuscito: nuovo tentativo ({Attempt}/{Max}) tra {Delay}", job.GetType().Name, next.Attempt, _retryDelays.Length, delay);

                _ = Task.Delay(delay, _stopping).ContinueWith(
                    t => { if (!t.IsCanceled) _queue.TryEnqueue(next); },
                    TaskScheduler.Default);
            }
            else
            {
                Log.Error("[MAIL] Lavoro {Job} non riuscito: nessun altro tentativo", job.GetType().Name);
            }
        }
    }
}
