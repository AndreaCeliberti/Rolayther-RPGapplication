# Email di Rolayther

Rolayther scrive agli utenti nei momenti importanti. Le email sono a tema (sfondo scuro, viola, immagini del sito),
si leggono bene anche da telefono e hanno sempre una versione in solo testo per i programmi di posta più vecchi.

## Cosa viene spedito

| Quando | A chi | Oggetto | Note |
|---|---|---|---|
| Registrazione (Player o richiesta Master) | chi si registra | Conferma la tua email per entrare in Rolayther | Pulsante «Conferma la mia email». Per i Master ricorda che serve anche l'approvazione di un amministratore. |
| L'Admin approva una richiesta Master | il candidato | Richiesta approvata: ora sei un Master di Rolayther | Include il messaggio di benvenuto scritto dall'Admin (facoltativo). Se l'email non è ancora confermata, il pulsante porta alla conferma. |
| L'Admin rifiuta una richiesta Master | il candidato | Aggiornamento sulla tua richiesta Master | Include la spiegazione scritta dall'Admin (**obbligatoria**, minimo 10 caratteri). |
| Un Player si iscrive a una sessione | il Player | Iscrizione confermata: *titolo sessione* | Data, durata, Master, posti e (se c'è) il link per entrare. |
| Un Player si iscrive a una sessione | il Master della sessione | Nuova iscrizione: *titolo sessione* | Mostra il nickname del Player e quanti posti restano. |
| Si crea una sessione | il Master della sessione | Sessione creata: *titolo sessione* | Se l'ha creata un Admin per conto del Master, l'email lo dice. |
| «Reinvia email» | chi lo chiede | Conferma la tua email per entrare in Rolayther | Stesso messaggio della registrazione, con un link nuovo. |

**Risposta dell'utente.** Il pulsante della prima email porta alla pagina `/confirm-email` del sito: è la «risposta»
dell'utente, e basta un clic. Se `Email:ReplyToEmail` è impostato, tutte le email hanno quella casella come
«Rispondi a», e quella di conferma e quella di rifiuto invitano esplicitamente a rispondere («Rispondi pure a questa
email»): chi si registra può segnalare un problema, chi viene rifiutato può portare altri elementi.

## Provarle subito in locale

Non serve configurare niente. Con le impostazioni predefinite (`Email:Provider = "Console"`) **nessuna email parte
davvero**: ogni messaggio viene salvato in `App_Data/emails/` come file `.html` (più una copia `.txt`).

1. Avvia backend e frontend (`npm run dev`).
2. Registra un Player dal sito.
3. Apri la cartella `App_Data/emails` del progetto backend e fai doppio clic sul file più recente: si apre nel
   browser, identico a come lo vedrebbe un utente.
4. Premi «Conferma la mia email»: si apre `http://localhost:5173/confirm-email?token=...` e l'indirizzo risulta confermato.

Lo stesso testo, con il link, compare anche nel log (cerca `[EMAIL:Console]`). La cartella è esclusa da Git.
Le immagini delle anteprime vengono dal frontend: si vedono se `npm run dev` è acceso.

## Metterle online (Render + Brevo)

Per spedire email vere serve un servizio di invio. Il progetto usa **Brevo** tramite la sua API web, perché Render
blocca le porte SMTP (25, 465 e 587) sui servizi gratuiti: l'SMTP classico lì non funzionerebbe.
Il piano gratuito di Brevo permette **300 email al giorno**, più che sufficienti per un progetto come questo.

1. Crea un account su [brevo.com](https://www.brevo.com).
2. **Mittente.** In *Mittenti, domini e IP* (Senders, Domains & Dedicated IPs) aggiungi l'indirizzo da cui
   scriveranno le email e verificalo con il codice che ti arriva.
3. **Chiave API.** In *SMTP e API > Chiavi API* (SMTP & API > API keys) genera una chiave. È un segreto: non va
   mai scritta nel repository né in `appsettings.json`.
4. Su Render, nel servizio del backend, aggiungi queste variabili d'ambiente (due trattini bassi al posto dei due punti):

   | Variabile | Valore |
   |---|---|
   | `Email__Provider` | `Brevo` |
   | `Email__Brevo__ApiKey` | la chiave API di Brevo |
   | `Email__FromEmail` | il mittente verificato al punto 2 |
   | `Email__FromName` | `Rolayther` |
   | `Email__ReplyToEmail` | una casella che leggi davvero (facoltativa, consigliata) |
   | `Email__FrontendBaseUrl` | l'indirizzo pubblico del sito, senza `/` finale, es. `https://rolayther.onrender.com` |

5. Controlla che il frontend pubblicato contenga la cartella `public/email/` (le immagini delle email): vengono
   lette da `FrontendBaseUrl`, oppure da `Email__AssetsBaseUrl` se le ospiti altrove.
6. Il backend accetta richieste solo dai siti elencati in `Cors__AllowedOrigins` (predefinito: `http://localhost:5173`):
   imposta su Render l'indirizzo del sito pubblicato, altrimenti la pagina di conferma non riesce a parlare con il server.
7. **Disattiva il blocco degli IP** in Brevo (*Impostazioni > Sicurezza > Indirizzi IP autorizzati*, per le chiavi API):
   gli indirizzi in uscita di Render gratuito cambiano e con il blocco attivo le email verrebbero rifiutate (errore 401).

Tutti i passaggi per mettere online il sito, nell'ordine giusto, sono in [DEPLOY.md](DEPLOY.md).

All'avvio il backend controlla le impostazioni e **non parte** se qualcosa non torna (per esempio `Brevo` senza chiave
API), dicendo cosa manca. Nel log trovi una riga `Email: invio reale con Brevo (mittente ...)` quando è tutto a posto.

> **Mittente con Gmail.** Brevo sconsiglia indirizzi gratuiti (gmail.com, yahoo.it...) come mittente: li sostituisce
> con un indirizzo suo e le email hanno più probabilità di finire nello spam. Funziona, ma per un risultato migliore
> serve un indirizzo su un dominio tuo, autenticato in Brevo (DKIM/DMARC). Una buona soluzione intermedia: mittente
> `no-reply@...` verificato e `Email__ReplyToEmail` con la tua casella Gmail, così le risposte arrivano a te.

## Impostazioni

Sezione `Email` di `appsettings.json` (o variabili d'ambiente `Email__...`).

| Chiave | Predefinito | Significato |
|---|---|---|
| `Provider` | `Console` | `Console` = anteprime su file (sviluppo). `Brevo` = invio reale. |
| `FromEmail` / `FromName` | `no-reply@rolayther.local` / `Rolayther` | Mittente. Con Brevo deve essere verificato. |
| `ReplyToEmail` | vuoto | Dove arrivano le risposte. Vuoto = le email non invitano a rispondere. |
| `FrontendBaseUrl` | `http://localhost:5173` | Indirizzo del sito, usato per costruire i link nelle email. |
| `AssetsBaseUrl` | vuoto | Dove stanno le immagini (`/email/...`). Vuoto = `FrontendBaseUrl`. |
| `ConfirmationTokenDays` | `3` | Giorni di validità del link di conferma (da 1 a 30). |
| `RequireConfirmedEmail` | `true` | Se `false`, nessuno è obbligato a confermare l'email (vedi sotto). |
| `ResendCooldownSeconds` | `60` | Pausa minima tra due «Reinvia email» per lo stesso indirizzo. |
| `ResendMaxPerHour` | `5` | Massimo reinvii all'ora per indirizzo. |
| `PreviewFolder` | `App_Data/emails` | Cartella delle anteprime (solo `Console`). |
| `Brevo:ApiKey` | vuoto | Chiave API di Brevo. **Solo variabile d'ambiente.** |

## Come funziona la conferma

- Il link contiene un codice firmato con la chiave `Jwt:Key`: non viene salvato nulla nel database e scade dopo
  `ConfirmationTokenDays` giorni. È legato all'indirizzo email: se l'indirizzo cambia, il vecchio link smette di valere.
  Se cambi `Jwt:Key`, i link già spediti non valgono più (basta chiederne uno nuovo).
- Il pulsante dell'email apre la pagina `/confirm-email` del sito, che manda il codice al server con una richiesta
  POST. Così le anteprime automatiche di antivirus e programmi di posta, che aprono i link da sole, non confermano
  l'indirizzo al posto dell'utente.
- Un link già usato dice «Email già confermata»; un link scaduto o rovinato propone di chiederne uno nuovo.
- **Cosa si può fare senza conferma:** registrarsi, accedere, navigare, caricare la foto. **Cosa no:** iscriversi alle
  sessioni (Player) e crearle (Master): il server risponde `403` con codice `EmailNotConfirmed` e il sito mostra in
  alto una striscia con «Reinvia email». L'Admin non è mai soggetto a questa regola.
- «Reinvia email» risponde sempre allo stesso modo, che l'indirizzo esista o no, così nessuno può scoprire chi è
  registrato. Ha un limite per indirizzo (`ResendCooldownSeconds`, `ResendMaxPerHour`) e un limite per IP.

### Account già esistenti

Gli utenti registrati **prima** di questa funzione risultano non confermati e dovrebbero confermare l'email
(il sito glielo propone con la striscia in alto). Se preferisci considerarli già a posto, esegui una volta sul database
(su Supabase: *SQL Editor*):

```sql
UPDATE "AspNetUsers" SET "EmailConfirmed" = TRUE WHERE "EmailConfirmed" = FALSE;
```

Per spegnere del tutto l'obbligo: `Email__RequireConfirmedEmail = false` (le email partono comunque).

## Se un'email non parte

- Le email delle sessioni (iscrizione, nuova sessione) e i reinvii partono **in background**, dopo la risposta
  all'utente: un problema di posta non rallenta né blocca mai iscrizioni o creazioni. Se l'invio fallisce si riprova
  dopo 5 secondi, 30 secondi e 2 minuti; poi l'errore resta nel log.
- Registrazione, approvazione e rifiuto dei Master rispondono con `emailSent: true/false`. Con `false` l'operazione è
  comunque riuscita e il sito lo dice (all'Admin: «l'email NON è partita, avvisa il candidato in un altro modo»; a chi
  si registra: «Reinvia email»).
- La coda è in memoria: se il server si riavvia mentre ci sono email in attesa, quelle email si perdono (la conferma
  si può sempre richiedere di nuovo).
- Il log (Serilog) riporta ogni invio e ogni errore con il tag `[EMAIL...]`. La chiave API non viene mai scritta nel log.

## Problemi comuni

| Sintomo | Causa probabile |
|---|---|
| Il backend non parte e parla di `Email:Brevo:ApiKey` | `Provider` è `Brevo` ma manca la variabile `Email__Brevo__ApiKey`. |
| Nel log compare un errore 401 di Brevo | Chiave API sbagliata o disattivata, oppure blocco degli IP attivo (*Impostazioni > Sicurezza > Indirizzi IP autorizzati*: disattivalo, vedi [DEPLOY.md](DEPLOY.md)). |
| Brevo risponde che il mittente non è valido | `Email__FromEmail` non è stato verificato nel pannello Brevo. |
| Le email arrivano in spam | Mittente con indirizzo gratuito o dominio non autenticato (vedi riquadro sopra). |
| Nell'email mancano le immagini | La cartella `public/email/` non è stata pubblicata col frontend, oppure `FrontendBaseUrl`/`AssetsBaseUrl` è sbagliato (in locale non si vedono da un vero programma di posta, solo dall'anteprima). |
| Il pulsante porta a `localhost` | `Email__FrontendBaseUrl` non è stato impostato su Render. |
| «Il link non è valido» | Link spezzato dal programma di posta o `Jwt:Key` cambiata: chiedere un nuovo link. |
| «Il link è scaduto» | Sono passati più di `ConfirmationTokenDays` giorni: chiedere un nuovo link. |
| 429 su «Reinvia email» | Troppe richieste ravvicinate dallo stesso IP o per lo stesso indirizzo: aspettare un minuto. |

## Dove sta il codice

| Cosa | Dove |
|---|---|
| Impostazioni e controlli all'avvio | `Services/Mail/EmailOptions.cs` |
| Modelli delle sei email (HTML + testo) | `Services/Mail/EmailTemplates.cs`, `Services/Mail/EmailLayout.cs` |
| Invio (sviluppo / Brevo) | `Services/ConsoleEmailService.cs`, `Services/BrevoEmailService.cs` |
| Scelta del modello e dei link | `Services/EmailNotificationService.cs` |
| Link firmati di conferma | `Services/Mail/EmailConfirmationTokenService.cs` |
| Coda in background | `Services/Mail/MailJobWorker.cs`, `Services/Mail/MailJobs.cs`, `Services/MailJobProcessor.cs` |
| Frontend | `src/pages/ConfirmEmailPage.jsx`, `src/components/auth/`, `src/components/admin/MasterDecisionModal.jsx`, immagini in `public/email/` |
