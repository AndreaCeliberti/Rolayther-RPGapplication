using Rolayther.Models.Enums;
using System.Globalization;
using System.Text;

namespace Rolayther.Services.Mail
{
    // I sei modelli di email di Rolayther. Qui ci sono SOLO i testi (in italiano) e la scelta dei blocchi:
    // l'aspetto grafico è tutto in EmailLayout, così per cambiare lo stile basta toccare un file solo.
    //
    //   1. ConfirmEmail      -> conferma registrazione (con link da cliccare = la "risposta" dell'utente)
    //   2. MasterApproved    -> richiesta Master approvata (con messaggio facoltativo dell'amministratore)
    //   3. MasterRejected    -> richiesta Master rifiutata, con la spiegazione
    //   4. SessionJoined     -> al giocatore: iscrizione alla sessione confermata
    //   5. SessionNewPlayer  -> al Master: un giocatore si è iscritto
    //   6. SessionCreated    -> al Master: la sessione è stata creata
    public static class EmailTemplates
    {
        // ------------------------------------------------------------------ 1. conferma registrazione
        public static RenderedEmail ConfirmEmail(EmailContext ctx, string firstName, string confirmUrl, bool isMasterRequest)
        {
            var name = EmailText.Clean(firstName, 60);
            var blocks = new List<EmailBlock>
            {
                new ParagraphBlock(new[]
                {
                    new Inline("Il tuo account Rolayther è stato creato. Per iscriverti alle sessioni e usare tutte le funzioni, "),
                    new Inline("conferma che questo indirizzo email è davvero tuo", Bold: true),
                    new Inline(": basta un clic.")
                })
            };

            if (isMasterRequest)
            {
                blocks.Add(new CalloutBlock(
                    "La tua richiesta Master è in attesa",
                    "Hai chiesto di diventare Master: un amministratore la valuterà e ti scriveremo l'esito a questo stesso indirizzo. " +
                    "Per poter creare sessioni, una volta approvato, servirà anche l'email confermata.",
                    EmailTheme.Gold));
            }

            blocks.Add(new ButtonBlock("Conferma la mia email", confirmUrl));
            blocks.Add(new LinkFallbackBlock(confirmUrl));

            blocks.Add(new NoteBlock(new[]
            {
                new Inline($"Il link è valido per {ctx.TokenDays} {(ctx.TokenDays == 1 ? "giorno" : "giorni")}. " +
                           "Se scade, dal sito puoi richiederne uno nuovo in qualsiasi momento.")
            }));

            if (ctx.CanReply)
            {
                blocks.Add(new NoteBlock(new[]
                {
                    new Inline("Hai un dubbio o qualcosa non torna? "),
                    new Inline("Rispondi pure a questa email", Bold: true),
                    new Inline(": ti leggiamo.")
                }));
            }

            blocks.Add(new NoteBlock(new[]
            {
                new Inline("Non hai creato tu questo account? Ignora questa email: senza conferma l'account non potrà iscriversi alle sessioni.")
            }));

            return EmailLayout.Render(new EmailContent(
                Subject: "Conferma la tua email per entrare in Rolayther",
                Preheader: "Un ultimo passo: conferma il tuo indirizzo e la tua avventura può cominciare.",
                IconFile: "ico-confirm.png",
                Accent: EmailTheme.Violet,
                Eyebrow: "Conferma registrazione",
                Title: name.Length > 0 ? $"Eccoti in Rolayther, {name}!" : "Eccoti in Rolayther!",
                Blocks: blocks,
                FooterReason: "Ricevi questa email perché questo indirizzo è stato usato per registrarsi su Rolayther."), ctx);
        }

        // ------------------------------------------------------------------ 2. Master approvato
        // actionUrl: dove porta il pulsante. Se l'email dell'utente NON è ancora confermata (needsEmailConfirmation)
        // il pulsante è il link di conferma, perché senza non potrebbe creare sessioni.
        public static RenderedEmail MasterApproved(EmailContext ctx, string firstName, string? adminMessage, string actionUrl, bool needsEmailConfirmation)
        {
            var name = EmailText.Clean(firstName, 60);
            var message = EmailText.CleanMultiline(adminMessage, 1000);

            var blocks = new List<EmailBlock>
            {
                new ParagraphBlock(new[]
                {
                    new Inline("Un amministratore ha "),
                    new Inline("approvato la tua richiesta", Bold: true),
                    new Inline(". Da questo momento sei un Master di Rolayther: puoi creare sessioni, scegliere i giochi e accogliere i giocatori al tuo tavolo.")
                })
            };

            if (message.Length > 0)
                blocks.Add(new QuoteBlock("Il messaggio dell'amministratore", message, EmailTheme.Emerald));

            var steps = new List<string>();
            if (needsEmailConfirmation)
                steps.Add("Conferma la tua email con il pulsante qui sotto: è necessario per creare sessioni.");
            steps.Add("Accedi a Rolayther (se avevi già fatto l'accesso, esci e rientra per attivare il nuovo ruolo).");
            steps.Add("Completa il tuo profilo Master con foto e presentazione.");
            steps.Add("Crea la tua prima sessione e pubblicala: i giocatori potranno iscriversi.");
            blocks.Add(new StepsBlock("Come iniziare", steps, EmailTheme.Emerald));

            blocks.Add(new ButtonBlock(needsEmailConfirmation ? "Conferma la mia email" : "Accedi a Rolayther", actionUrl));
            if (needsEmailConfirmation)
                blocks.Add(new LinkFallbackBlock(actionUrl));

            return EmailLayout.Render(new EmailContent(
                Subject: "Richiesta approvata: ora sei un Master di Rolayther",
                Preheader: "La tua richiesta è stata approvata: ora puoi creare le tue sessioni.",
                IconFile: "ico-approved.png",
                Accent: EmailTheme.Emerald,
                Eyebrow: "Richiesta approvata",
                Title: name.Length > 0 ? $"Sei dei nostri, {name}!" : "Sei dei nostri!",
                Blocks: blocks,
                FooterReason: "Ricevi questa email perché hai chiesto di diventare Master su Rolayther."), ctx);
        }

        // ------------------------------------------------------------------ 3. Master rifiutato
        public static RenderedEmail MasterRejected(EmailContext ctx, string firstName, string reason)
        {
            var name = EmailText.Clean(firstName, 60);

            var blocks = new List<EmailBlock>
            {
                new ParagraphBlock(new[]
                {
                    new Inline("Grazie per la tua candidatura come Master di Rolayther e per il tempo dedicato alla richiesta. "),
                    new Inline("Dopo averla valutata, per ora non possiamo approvarla.", Bold: true)
                }),
                new QuoteBlock("Il motivo", EmailText.CleanMultiline(reason, 1000), EmailTheme.Rose)
            };

            if (ctx.CanReply)
            {
                blocks.Add(new ParagraphBlock(new[]
                {
                    new Inline("Pensi che ci sia stato un malinteso, o vuoi darci altri elementi? "),
                    new Inline("Rispondi a questa email", Bold: true),
                    new Inline(": riguarderemo la richiesta volentieri.")
                }));
            }
            else
            {
                blocks.Add(new ParagraphBlock(new[]
                {
                    new Inline("Se hai dubbi sulla decisione, puoi contattare gli amministratori di Rolayther.")
                }));
            }

            blocks.Add(new ButtonBlock("Scopri le sessioni di Rolayther", ctx.Url("/sessions")));

            return EmailLayout.Render(new EmailContent(
                Subject: "Aggiornamento sulla tua richiesta Master",
                Preheader: "Abbiamo valutato la tua richiesta: ecco la risposta e il motivo.",
                IconFile: "ico-rejected.png",
                Accent: EmailTheme.Rose,
                Eyebrow: "Richiesta non approvata",
                Title: name.Length > 0 ? $"Aggiornamento sulla tua richiesta, {name}" : "Aggiornamento sulla tua richiesta",
                Blocks: blocks,
                FooterReason: "Ricevi questa email perché hai chiesto di diventare Master su Rolayther."), ctx);
        }

        // ------------------------------------------------------------------ 4. iscrizione alla sessione (al giocatore)
        public static RenderedEmail SessionJoined(EmailContext ctx, string firstName, SessionEmailInfo s)
        {
            var name = EmailText.Clean(firstName, 60);
            var title = EmailText.Clean(s.Title, 100);
            var full = s.MaxPlayers > 0 && s.PlayersCount >= s.MaxPlayers;

            var blocks = new List<EmailBlock>
            {
                new ParagraphBlock(new[]
                {
                    new Inline("La tua iscrizione alla sessione "),
                    new Inline(title, Bold: true),
                    new Inline(" è andata a buon fine: il tuo posto al tavolo è riservato e ora non resta che preparare il personaggio.")
                })
            };

            if (full)
            {
                blocks.Add(new ParagraphBlock(new[]
                {
                    new Inline("Il tavolo è al completo: la tua iscrizione ha occupato l'ultimo posto!", Bold: true)
                }));
            }

            var blurb = EmailText.CleanMultiline(s.Description, 240);
            if (blurb.Length > 0)
                blocks.Add(new QuoteBlock("Di cosa si tratta", blurb, EmailTheme.Cyan));

            blocks.Add(SessionDetails(s, includeMaster: true, includeLink: true, includeState: false));
            blocks.Add(new ButtonBlock("Apri la sessione", ctx.Url("/sessions/" + s.SessionId)));
            blocks.Add(new NoteBlock(new[]
            {
                new Inline("Non puoi più partecipare? Puoi annullare l'iscrizione in qualsiasi momento dalla pagina della sessione, così il posto torna libero per qualcun altro.")
            }));

            return EmailLayout.Render(new EmailContent(
                Subject: "Iscrizione confermata: " + EmailText.Truncate(title, 60),
                Preheader: $"Sei dentro! Ecco quando e dove si gioca: {title}.",
                IconFile: "ico-joined.png",
                Accent: EmailTheme.Cyan,
                Eyebrow: "Iscrizione confermata",
                Title: name.Length > 0 ? $"Il tuo posto è prenotato, {name}!" : "Il tuo posto è prenotato!",
                Blocks: blocks,
                FooterReason: "Ricevi questa email perché hai effettuato l'iscrizione a una sessione su Rolayther."), ctx);
        }

        // ------------------------------------------------------------------ 5. nuovo giocatore (al Master)
        public static RenderedEmail SessionNewPlayer(EmailContext ctx, string masterFirstName, string playerNickName, SessionEmailInfo s)
        {
            var name = EmailText.Clean(masterFirstName, 60);
            var nick = EmailText.Clean(playerNickName, 60);
            var title = EmailText.Clean(s.Title, 100);
            var full = s.MaxPlayers > 0 && s.PlayersCount >= s.MaxPlayers;

            var blocks = new List<EmailBlock>
            {
                new ParagraphBlock(new[]
                {
                    new Inline(nick.Length > 0 ? nick : "Un giocatore", Bold: true),
                    new Inline(" ha appena preso posto nella tua sessione "),
                    new Inline(title, Bold: true),
                    new Inline(full ? ". Il tavolo è ora al completo!" : ".")
                }),
                new DetailsBlock(new List<DetailRow>
                {
                    new("Sessione", title),
                    new("Quando", EmailFormat.When(s.ScheduledAt)),
                    new("Iscritti", EmailFormat.Seats(s.PlayersCount, s.MaxPlayers)),
                    new("Disponibilità", EmailFormat.SeatsLeft(s.PlayersCount, s.MaxPlayers))
                }),
                new ButtonBlock("Gestisci la sessione", ctx.Url("/sessions/" + s.SessionId))
            };

            return EmailLayout.Render(new EmailContent(
                Subject: "Nuova iscrizione: " + EmailText.Truncate(title, 60),
                Preheader: $"{(nick.Length > 0 ? nick : "Un giocatore")} ha preso posto in {title}. {EmailFormat.Seats(s.PlayersCount, s.MaxPlayers)}.",
                IconFile: "ico-player.png",
                Accent: EmailTheme.Blue,
                Eyebrow: "Nuova iscrizione",
                Title: full ? (name.Length > 0 ? $"Tavolo al completo, {name}!" : "Tavolo al completo!") : (name.Length > 0 ? $"Il tuo tavolo si riempie, {name}!" : "Il tuo tavolo si riempie!"),
                Blocks: blocks,
                FooterReason: "Ricevi questa email perché sei il Master di questa sessione su Rolayther."), ctx);
        }

        // ------------------------------------------------------------------ 6. sessione creata (al Master)
        public static RenderedEmail SessionCreated(EmailContext ctx, string masterFirstName, SessionEmailInfo s, bool createdByAdmin)
        {
            var name = EmailText.Clean(masterFirstName, 60);
            var title = EmailText.Clean(s.Title, 100);

            var blocks = new List<EmailBlock>
            {
                new ParagraphBlock(new[]
                {
                    new Inline(createdByAdmin ? "Un amministratore ha creato per te la sessione " : "Hai creato la sessione "),
                    new Inline(title, Bold: true),
                    new Inline(".")
                })
            };

            if (s.State == SessionState.Draft)
            {
                blocks.Add(new CalloutBlock(
                    "Stato: In Arrivo",
                    "La sessione è visibile, ma i giocatori potranno iscriversi solo dopo che l'avrai pubblicata. " +
                    "Quando vuoi aprirla ai giocatori, cambia lo stato in «Pubblicata» dalla tua area Master.",
                    EmailTheme.Gold));
            }
            else if (s.State == SessionState.Published)
            {
                blocks.Add(new ParagraphBlock(new[]
                {
                    new Inline("La sessione è già "),
                    new Inline("pubblicata", Bold: true),
                    new Inline(": i giocatori possono iscriversi.")
                }));
            }

            blocks.Add(SessionDetails(s, includeMaster: false, includeLink: true, includeState: true));

            blocks.Add(new StepsBlock("Prossimi passi", new[]
            {
                "Controlla i dettagli e, se non l'hai già fatto, aggiungi una cover.",
                s.State == SessionState.Draft ? "Pubblica la sessione quando è tutto pronto." : "Condividi il link della sessione con i tuoi giocatori.",
                "Ti scriveremo ogni volta che un giocatore si iscrive."
            }, EmailTheme.Gold));

            blocks.Add(new ButtonBlock("Apri la sessione", ctx.Url("/sessions/" + s.SessionId)));

            return EmailLayout.Render(new EmailContent(
                Subject: "Sessione creata: " + EmailText.Truncate(title, 60),
                Preheader: $"La tua sessione \"{title}\" è stata creata. Ecco il riepilogo.",
                IconFile: "ico-created.png",
                Accent: EmailTheme.Gold,
                Eyebrow: "Sessione creata",
                Title: name.Length > 0 ? $"La tua sessione è pronta, {name}!" : "La tua sessione è pronta!",
                Blocks: blocks,
                FooterReason: "Ricevi questa email perché sei il Master di questa sessione su Rolayther."), ctx);
        }

        // ------------------------------------------------------------------ riquadro dettagli sessione
        private static DetailsBlock SessionDetails(SessionEmailInfo s, bool includeMaster, bool includeLink, bool includeState)
        {
            var rows = new List<DetailRow> { new("Sessione", EmailText.Clean(s.Title, 100)) };

            if (EmailText.Clean(s.GameTitle, 100).Length > 0)
                rows.Add(new("Gioco", EmailText.Clean(s.GameTitle, 100)));
            if (EmailText.Clean(s.GenreName, 100).Length > 0)
                rows.Add(new("Genere", EmailText.Clean(s.GenreName, 100)));
            if (includeMaster && EmailText.Clean(s.MasterNickName, 100).Length > 0)
                rows.Add(new("Master", EmailText.Clean(s.MasterNickName, 100)));

            rows.Add(new("Quando", EmailFormat.When(s.ScheduledAt)));

            if (EmailText.Clean(s.Duration, 100).Length > 0)
                rows.Add(new("Durata", EmailText.Clean(s.Duration, 100)));

            rows.Add(new("Giocatori", EmailFormat.Seats(s.PlayersCount, s.MaxPlayers)));

            if (includeState)
                rows.Add(new("Stato", EmailFormat.State(s.State)));

            var link = includeLink ? EmailLayout.SafeUrl(s.SessionLink) : null;
            if (link != null)
                rows.Add(new("Link di gioco", EmailText.Truncate(link, 80), link));

            return new DetailsBlock(rows);
        }
    }

    // Utilità di pulizia dei testi inseriti dagli utenti (nomi, titoli, messaggi): niente caratteri di controllo
    // né quelli che invertono il senso di scrittura, lunghezza massima. La codifica HTML la fa EmailLayout.
    internal static class EmailText
    {
        public static string Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (IsBidiControl(ch)) continue;
                sb.Append(char.IsControl(ch) ? ' ' : ch);
            }

            var oneLine = string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return Truncate(oneLine, max);
        }

        // Come Clean, ma mantiene gli a capo (messaggi di più righe)
        public static string CleanMultiline(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value.Replace("\r\n", "\n").Replace('\r', '\n'))
            {
                if (IsBidiControl(ch)) continue;
                if (ch == '\n') { sb.Append('\n'); continue; }
                sb.Append(char.IsControl(ch) ? ' ' : ch);
            }

            // niente più di una riga vuota di seguito
            var lines = sb.ToString().Split('\n').Select(l => l.TrimEnd()).ToList();
            var result = new StringBuilder();
            var blank = 0;
            foreach (var line in lines)
            {
                blank = line.Length == 0 ? blank + 1 : 0;
                if (blank > 1) continue;
                result.Append(line).Append('\n');
            }

            return Truncate(result.ToString().Trim(), max);
        }

        public static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..Math.Max(0, max - 1)].TrimEnd() + "…";

        private static bool IsBidiControl(char ch) =>
            ch is >= '‪' and <= '‮' or >= '⁦' and <= '⁩' or '‎' or '‏' or '؜';
    }

    // Formati italiani scritti a mano: non dipendono dalla cultura del server (su Render potrebbe mancare la lingua italiana)
    internal static class EmailFormat
    {
        private static readonly string[] Days = { "domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato" };

        private static readonly string[] Months =
        {
            "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno",
            "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre"
        };

        // Es. "venerdì 20 ottobre 2026, ore 21:00". L'orario è quello salvato con la sessione, senza conversioni di fuso.
        public static string When(DateTime value) =>
            $"{Days[(int)value.DayOfWeek]} {value.Day} {Months[value.Month - 1]} {value.Year}, ore {value.ToString("HH:mm", CultureInfo.InvariantCulture)}";

        // Es. "3 su 5 posti occupati"
        public static string Seats(int taken, int max) =>
            max > 0 ? $"{taken} su {max} posti occupati" : $"{taken} iscritti";

        public static string SeatsLeft(int taken, int max)
        {
            if (max <= 0) return "—";

            var left = max - taken;
            return left <= 0 ? "Al completo" : left == 1 ? "Manca 1 posto" : $"Mancano {left} posti";
        }

        // Stessi nomi che si vedono nell'applicazione (utils/sessionState.js del frontend)
        public static string State(SessionState state) => state switch
        {
            SessionState.Draft => "In Arrivo",
            SessionState.Published => "Pubblicata",
            SessionState.Full => "Piena",
            SessionState.InProgress => "In corso",
            SessionState.Completed => "Completata",
            SessionState.Cancelled => "Annullata",
            _ => state.ToString()
        };
    }
}
