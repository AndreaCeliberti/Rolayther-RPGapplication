# Mettere Rolayther online (gratis)

Quattro servizi gratuiti, ognuno con un compito:

| Servizio | Cosa fa | Da dove prende il codice | Limiti del piano gratuito che contano |
|---|---|---|---|
| **Netlify** | ospita il sito (frontend React) | branch `main` | abbondanti per un sito così |
| **Render** | esegue il backend ASP.NET (con Docker) | branch `master` | si **addormenta dopo 15 minuti senza visite** (la prima visita dopo la pausa aspetta circa un minuto); 750 ore al mese, quindi un solo servizio acceso 24 ore su 24 ci sta; il disco si svuota a ogni riavvio |
| **Supabase** | database PostgreSQL + archivio dei file | — | 500 MB di database, 1 GB di file (max 50 MB l'uno); il progetto va **in pausa dopo 7 giorni senza attività**; nessun backup automatico |
| **Brevo** | spedisce le email | — | 300 email al giorno |

Ordine consigliato, perché ogni passo ha bisogno di un indirizzo creato dal precedente:

1. Supabase: progetto, database, bucket dei file (sezioni 1 e 2)
2. Il tuo PC: generare le tabelle e provare tutto in locale (sezione 3)
3. Brevo: mittente e chiave (sezione 4)
4. Netlify: creare il sito, solo per conoscerne l'indirizzo (sezione 5)
5. Render: il backend (sezione 6)
6. Netlify: dire al sito dov'è il backend (sezione 7)
7. Un ping ogni 5 minuti e la prova finale (sezioni 8 e 9)

> I nomi delle voci nei pannelli dei vari servizi cambiano ogni tanto. Se una voce non c'è, cerca quella più simile.

---

## 1. Supabase: progetto e database

1. [supabase.com](https://supabase.com) → **New project**. Scegli la regione **Europa** (per esempio *Frankfurt*) e usa la stessa regione per Render: le richieste tra i due sono più veloci.
2. **Database password**: scegline una fatta **solo di lettere e cifre** (niente `@ : / # ? ; = "`): finisce dentro un indirizzo e i simboli lo rompono. Conservala: Supabase non la mostra più (si può solo azzerare, in *Project Settings > Database*).
3. Premi **Connect** (in alto) e scegli la modalità **Session pooler**, non *Direct connection*. Il motivo: Render si collega solo con IPv4, e la connessione diretta di Supabase è solo IPv6, quindi da Render non funzionerebbe. L'indirizzo ha questa forma:

   ```
   postgresql://postgres.abcdefghijkl:[YOUR-PASSWORD]@aws-0-eu-central-1.pooler.supabase.com:5432/postgres
   ```

   Sostituisci `[YOUR-PASSWORD]` (parentesi quadre comprese) con la tua password. L'utente è `postgres.abcdefghijkl`, non solo `postgres`. Il backend accetta questo indirizzo così com'è: aggiunge da solo SSL obbligatorio e un massimo di 10 connessioni (il piano gratuito ne concede poche).

   Questo è il valore di `ConnectionStrings__DefaultConnection`. È un **segreto**: contiene la password.

## 2. Supabase: i due bucket e la chiave dei file

I file caricati (avatar, copertine, materiale delle sessioni) stanno in **Storage**, non sul disco di Render.

1. **Storage > New bucket**, due volte:

   | Nome (esatto) | Public bucket | Cosa contiene | Limiti consigliati |
   |---|---|---|---|
   | `uploads` | **acceso** | avatar, copertine, loghi (chiunque abbia l'indirizzo vede l'immagine, serve per `<img>`) | 5 MB; tipi `image/jpeg, image/png, image/webp, image/gif` |
   | `private-uploads` | **spento** | materiale delle sessioni: lo vedono solo Master, Player iscritti e Admin, passando dal backend | 20 MB |

   I nomi vanno scritti proprio così: il codice li usa tali e quali (`uploads` serve anche a riconoscere le immagini nostre da quelle esterne).
2. **Project Settings > API Keys**: copia la chiave **segreta** (*Secret key*, inizia con `sb_secret_`). Nei progetti più vecchi si chiama `service_role`. Non la chiave pubblica (`sb_publishable_` / `anon`): il backend la rifiuta all'avvio. È un **segreto**: può fare tutto sui file, mai nel sito, mai nel repository. Vale come `Storage__Supabase__SecretKey`.
3. L'indirizzo del progetto (*Project URL*, in *Project Settings > Data API* e nella finestra Connect) è nella forma `https://abcdefghijkl.supabase.co`. Se non lo trovi: è `https://` + *Reference ID* (in *Project Settings > General*) + `.supabase.co`. Vale come `Storage__Supabase__Url`.

Al primo avvio il backend controlla i due bucket e scrive nel log se uno manca o ha la visibilità sbagliata (cerca `[STORAGE:Supabase]`).

## 3. Il tuo PC: tabelle e prova in locale

Le vecchie migrazioni erano scritte per SQL Server e non funzionano con PostgreSQL: si rigenerano una volta sola. Il database è **vuoto** all'inizio: i dati di prova che avevi in SQL Server locale non si spostano da soli.

1. **User Secrets** del progetto (Visual Studio: tasto destro sul progetto > *Manage User Secrets*). Aggiungi la stringa di connessione della sezione 1 accanto a quello che c'è già (`Jwt:Key`, `Admin:Email`, `Admin:Password`):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "postgresql://postgres.abcdefghijkl:LA-TUA-PASSWORD@aws-0-eu-central-1.pooler.supabase.com:5432/postgres"
     }
   }
   ```

   In locale i file restano su disco (`Storage:Provider` vale `Local`) e le email sono salvate come anteprime (`Email:Provider` vale `Console`): per provare non serve altro.
2. **Elimina la cartella `Migrations`** (Esplora soluzioni > tasto destro > *Elimina*).
3. Compila il progetto, poi apri *Strumenti > Gestione pacchetti NuGet > Console di Gestione pacchetti* e scrivi:

   ```powershell
   Add-Migration InitialCreate
   ```

   Nasce una nuova cartella `Migrations` per PostgreSQL. Questa va **nel repository**: l'applicazione online ne ha bisogno per creare le tabelle.
4. Avvia il backend. All'avvio, in sviluppo, applica la migrazione, crea i ruoli e l'utente Admin (con `Admin:Email` e `Admin:Password`). Nella console deve comparire `Database PostgreSQL: aws-0-...pooler.supabase.com:5432/postgres`. In Supabase, *Table Editor*, ora ci sono `AspNetUsers`, `Sessions`, ecc.
5. Avvia anche il frontend e prova: registrazione, accesso, creazione di una sessione, iscrizione.

## 4. Brevo: email vere

Passaggi completi in [EMAIL.md](EMAIL.md#metterle-online-render--brevo). In breve: verifica un **mittente**, genera una **chiave API** (segreta), poi

- **disattiva il blocco degli IP**: *Impostazioni > Sicurezza > Indirizzi IP autorizzati* (*Authorized IPs*) e, per le chiavi API, scegli di disattivare il blocco. Con il blocco attivo Brevo accetta le chiamate solo dagli IP che conosce, ma gli indirizzi in uscita di Render gratuito cambiano: le email verrebbero rifiutate con un errore 401. (Brevo può attivarlo da solo, dopo circa 30 giorni senza nuovi IP.)

## 5. Netlify: creare il sito

1. **Add new site > Import an existing project**, scegli GitHub e il repository `Rolayther-RPGapplication`, **branch `main`**. Le impostazioni di compilazione le legge dal file `netlify.toml` del repository.
2. Dai al sito un nome che ti piace (*Site configuration > Change site name*): l'indirizzo sarà `https://nome.netlify.app`. Annotalo, **senza** `/` finale: serve a Render.
3. Il primo deploy userà ancora l'indirizzo del backend di prova: è normale, lo sistemi alla sezione 7.

## 6. Render: il backend

1. **New > Web Service** > *Build and deploy from a Git repository* > il repository `Rolayther-RPGapplication`.
2. Impostazioni:

   | Campo | Valore |
   |---|---|
   | Branch | `master` |
   | Language | **Docker** (il `Dockerfile` è nella cartella principale) |
   | Region | la stessa di Supabase (Frankfurt) |
   | Instance Type | **Free** |
   | Health Check Path (in *Advanced*) | `/health` |

3. **Environment Variables** (due trattini bassi al posto dei due punti). Quelle con 🔒 sono segreti:

   | Variabile | Valore |
   |---|---|
   | `ConnectionStrings__DefaultConnection` 🔒 | la stringa Session pooler della sezione 1 |
   | `Database__InitializeOnStartup` | `true` (a ogni avvio applica le migrazioni mancanti e crea ruoli e Admin) |
   | `Admin__Email` | l'email con cui entrerai come Admin |
   | `Admin__Password` 🔒 | la password dell'Admin (almeno 8 caratteri, maiuscole e minuscole) |
   | `Jwt__Key` 🔒 | una chiave casuale di almeno 32 caratteri (vedi sotto) |
   | `Jwt__Issuer` | l'indirizzo del backend, per esempio `https://rolayther-api.onrender.com` |
   | `Jwt__Audience` | l'indirizzo del sito, per esempio `https://nome.netlify.app` |
   | `Cors__AllowedOrigins` | l'indirizzo del sito, `https://nome.netlify.app`, senza `/` finale. Più siti: separati da virgola |
   | `Storage__Provider` | `Supabase` |
   | `Storage__Supabase__Url` | `https://abcdefghijkl.supabase.co` |
   | `Storage__Supabase__SecretKey` 🔒 | la chiave segreta della sezione 2 |
   | `Email__Provider` | `Brevo` |
   | `Email__Brevo__ApiKey` 🔒 | la chiave API di Brevo |
   | `Email__FromEmail` | il mittente verificato in Brevo |
   | `Email__FromName` | `Rolayther` |
   | `Email__ReplyToEmail` | una casella che leggi davvero (facoltativa, consigliata) |
   | `Email__FrontendBaseUrl` | l'indirizzo del sito, `https://nome.netlify.app` |

   Per generare `Jwt__Key` (PowerShell):

   ```powershell
   $b = New-Object byte[] 48; (New-Object Security.Cryptography.RNGCryptoServiceProvider).GetBytes($b); [Convert]::ToBase64String($b)
   ```

   Se cambi `Jwt__Key`, chi è collegato deve rifare l'accesso e i link di conferma email già spediti non valgono più.
   `ASPNETCORE_ENVIRONMENT` (= `Production`) e `Hosting__ClientIpHeader` (= `CF-Connecting-IP`) li imposta già il `Dockerfile`.
4. **Create Web Service**. La prima compilazione richiede alcuni minuti. Nei **Logs** di Render, a servizio avviato, devono comparire righe come:

   ```
   CORS: il sito può chiamare l'API da https://nome.netlify.app
   Ambiente: Production. Database PostgreSQL: aws-0-eu-central-1.pooler.supabase.com:5432/postgres
   Archivio file: Supabase Storage (https://abcdefghijkl.supabase.co)
   Email: invio reale con Brevo (mittente ...)
   [STORAGE:Supabase] Bucket uploads ok (pubblico)
   [STORAGE:Supabase] Bucket private-uploads ok (privato)
   ```

   Se qualcosa manca, il servizio **non parte** e dice cosa (vedi *Problemi comuni*). `https://rolayther-api.onrender.com/health` deve rispondere `{"status":"ok"}`.

## 7. Netlify: collegare il sito al backend

*Site configuration > Environment variables* > aggiungi:

| Variabile | Valore |
|---|---|
| `VITE_API_URL` | l'indirizzo del backend **con `/api` in fondo**, per esempio `https://rolayther-api.onrender.com/api` |

Poi *Deploys > Trigger deploy > Deploy site*: le variabili `VITE_` entrano nel sito al momento della compilazione, quindi serve rifarla. Il valore del pannello Netlify ha la precedenza sul file `.env` del repository.

## 8. Un ping ogni 5 minuti (facoltativo ma consigliato)

Senza visite per 15 minuti Render addormenta il backend (primo accesso lento) e dopo 7 giorni senza attività Supabase mette il progetto in pausa. Un servizio di controllo gratuito (per esempio [UptimeRobot](https://uptimerobot.com), monitor di tipo *HTTP(s)*, intervallo 5 minuti) risolve entrambe le cose se chiama

```
https://rolayther-api.onrender.com/health/db
```

Quell'indirizzo interroga anche il database (risponde `503` se non risponde). Tenere il servizio sempre sveglio consuma quasi tutte le 750 ore gratuite del mese: va bene con un solo servizio. Senza ping tutto funziona lo stesso, solo con l'attesa della prima visita.

## 9. Prova finale

1. Apri il sito, registra un Player: deve arrivare l'email di conferma e il pulsante deve portare al sito pubblicato.
2. Entra con l'Admin (`Admin__Email` / `Admin__Password`).
3. Carica un avatar: si vede subito. Su Render premi *Manual Deploy > Restart service*: l'avatar **deve esserci ancora** (è su Supabase, non sul disco).
4. Da Master (o Admin) carica un file nei materiali di una sessione, scaricalo da un Player iscritto, poi eliminalo.
5. Crea una sessione e iscrivi due Player.

## Aggiornare il sito

- **Frontend**: un push su `main` ricompila Netlify da solo.
- **Backend**: un push su `master` ricompila Render da solo. Se il codice porta una nuova migrazione (`Add-Migration ...` sul tuo PC, cartella `Migrations` nel repository), all'avvio il backend la applica da solo (`Database__InitializeOnStartup`).
- Le variabili d'ambiente di Render si cambiano dal pannello (*Environment*); il servizio si riavvia da solo.

## Problemi comuni

| Sintomo | Causa probabile e rimedio |
|---|---|
| Nel log: *Network is unreachable* o *Name or service not known* verso il database | Hai usato *Direct connection* (solo IPv6). Usa **Session pooler** (host `...pooler.supabase.com`, porta `5432`). |
| *password authentication failed* | Password sbagliata, oppure utente scritto `postgres` invece di `postgres.abcdefghijkl`. Azzera la password in Supabase se non la ricordi. |
| *MaxClientsInSessionMode* / *too many connections* | Troppe connessioni aperte. Con l'indirizzo `postgresql://` il limite è già 10; se usi la forma `Host=...;` aggiungi `Maximum Pool Size=10`. |
| Il servizio non parte e il log parla di `Storage:Supabase:...` | Variabile mancante o sbagliata (indirizzo senza `https://`, chiave pubblica al posto di quella segreta). Il messaggio dice quale. |
| Il servizio non parte e il log parla di `Jwt:...` | Mancano `Jwt__Key` (almeno 16 caratteri), `Jwt__Issuer` o `Jwt__Audience`. |
| Nel log: *segnaposto [YOUR-PASSWORD]* | Nella stringa di connessione è rimasto il segnaposto di Supabase: sostituiscilo con la password. |
| Nel browser: errore CORS, le richieste al backend falliscono | `Cors__AllowedOrigins` non coincide con l'indirizzo del sito (attenzione a `https` / `http`, a `www`, e a spazi). Controlla il log: la riga `CORS:` mostra cosa è stato letto. |
| Le immagini non si vedono (404 / 403) | Il bucket `uploads` non è **pubblico** o ha un altro nome. Il log all'avvio lo dice (`[STORAGE:Supabase]`). |
| Il caricamento file dà errore 500 | Nel log cerca `[STORAGE:Supabase]`: di solito chiave segreta sbagliata (403) o bucket mancante. |
| Il primo accesso dopo una pausa è lentissimo | È l'addormentamento di Render (circa un minuto). Vedi sezione 8. |
| Il login risponde 429 | Il limite è di 10 tentativi al minuto per visitatore (IP). Se capita a più utenti insieme, il backend non sta distinguendo i visitatori: `Hosting__ClientIpHeader` deve valere `CF-Connecting-IP` (lo imposta il `Dockerfile`; controlla di non averlo sovrascritto su Render). |
| Le email non arrivano o il log dice 401 di Brevo | Chiave sbagliata, mittente non verificato o blocco degli IP attivo (sezione 4). Altri casi in [EMAIL.md](EMAIL.md#problemi-comuni). |
| Supabase dice *project paused* | Era inattivo da più di 7 giorni. Dal pannello si riattiva (*Restore project*). Il ping della sezione 8 lo evita. |

## Cose da sapere

- **Prove in locale.** Con l'indirizzo di Supabase nei User Secrets il tuo PC usa lo stesso database del sito online: va bene prima di aprirlo al pubblico, ma dopo ogni prova (utenti e sessioni di collaudo) finisce tra i dati veri. Per le prove usa un secondo progetto Supabase solo per questo, oppure PostgreSQL installato sul PC: `appsettings.Development.json` punta già a `localhost`, basta inserire la password lì e togliere la stringa dai User Secrets.
- **Backup.** Il piano gratuito di Supabase non fa backup automatici. Ogni tanto esporta i dati (in locale: `pg_dump` con l'indirizzo *Session pooler*) o almeno conserva le migrazioni nel repository.
- **Dati personali.** Le email e i dati degli utenti passano da Render, Supabase, Brevo e Netlify: nell'informativa privacy del sito (`src/config/privacy.js`, elenco `providers`) vanno indicati questi quattro fornitori.
- **Segreti.** Chiave del database, chiave segreta di Supabase, `Jwt__Key`, chiave Brevo e password Admin stanno solo nelle variabili d'ambiente e nei User Secrets, mai nel repository. Se una chiave finisce in un posto sbagliato, rigenerala dal pannello del servizio e aggiornala su Render.
- **.NET 8.** Il supporto di .NET 8 termina il 10 novembre 2026: l'aggiornamento a una versione più recente va pianificato (si cambia versione nel `Dockerfile` e nel progetto).
- **Swagger** è disattivato online (resta solo in sviluppo).
